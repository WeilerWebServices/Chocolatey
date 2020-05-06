// Copyright 2011 - Present RealDimensions Software, LLC, the original
// authors/contributors from ChocolateyGallery
// at https://github.com/chocolatey/chocolatey.org,
// and the authors/contributors of NuGetGallery
// at https://github.com/NuGet/NuGetGallery
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//   http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data.Entity;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Elmah;
using Lucene.Net.Index;
using Lucene.Net.Store;
using WebBackgrounder;
using Directory = Lucene.Net.Store.Directory;

namespace NuGetGallery
{
    public class LuceneIndexingService : IIndexingService
    {
        private const int LUCENE_CACHE_REBUILD_HOURS = 24;
        private const int LUCENE_UPDATE_JOB_FREQUENCY_MINUTES = 10;
        private const int LUCENE_UPDATE_JOB_TIMEOUT_MINUTES = 8;
        private readonly string _rejectedStatus = PackageStatusType.Rejected.GetDescriptionOrValue();
        private readonly Func<EntitiesContext> _contextThunk;
        private static readonly object IndexWriterLock = new object();
        private static readonly TimeSpan IndexRecreateInterval = TimeSpan.FromHours(LUCENE_CACHE_REBUILD_HOURS);
        private static readonly ConcurrentDictionary<Directory, IndexWriter> WriterCache = new ConcurrentDictionary<Directory, IndexWriter>();
        private readonly Directory _directory;
        private IndexWriter _indexWriter;
        private readonly bool _indexContainsAllVersions;
        private readonly Func<bool> _getShouldAutoUpdate;

        public string IndexPath { get { return LuceneCommon.IndexDirectory; } }

        public bool IsLocal { get { return true; } }

        private IndexWriter Index
        {
            get
            {
                // Lock so that we can block in the getter if we're in error recovery.
                // The reference should resolve when the recovery code completes.
                lock (IndexWriterLock)
                {
                    return _indexWriter;
                }
            }
            set
            {
                _indexWriter = value;
            }
        }

        public LuceneIndexingService(Func<EntitiesContext> contextThunk, bool indexContainsAllVersions)
        {
            if (contextThunk == null)
            {
                throw new ArgumentNullException("contextThunk");
            }
            _contextThunk = contextThunk;

            _indexContainsAllVersions = indexContainsAllVersions;
            _directory = new LuceneFileSystem(LuceneCommon.IndexDirectory);
            _getShouldAutoUpdate = () => true;
        }

        public void UpdateIndex()
        {
            if (_getShouldAutoUpdate())
            {
                UpdateIndex(forceRefresh: false);
            }
        }

        public void UpdateIndex(bool forceRefresh)
        {
            // TODO could we rebuild the lucene cache somewhere and then remove the current and replace it with the updated one
            // Always do it if we're asked to "force" a refresh (i.e. manually triggered)
            // Otherwise, no-op unless we're supporting background search indexing.
            if (forceRefresh || _getShouldAutoUpdate())
            {
                DateTime? lastWriteTime = GetLastWriteTime();

                if ((lastWriteTime == null) || IndexRequiresRefresh() || forceRefresh)
                {
                    DoAndRetryOnOutOfMemory(
                        () =>
                        {
                            EnsureIndexWriter(creatingIndex: true);
                            Debug.Assert(Index != null);

                            Trace.WriteLine("Lucene Index: Deleting index");
                            Index.DeleteAll();
                            Trace.WriteLine("Lucene Index: Index delete completed");

                            Index.Commit();
                            Trace.WriteLine("Lucene Index: Index delete committed");
                        }, () => DisposeAndEnsureIndexWriter(creatingIndex: true));

                    // Reset the lastWriteTime to null. This will allow us to get a fresh copy of all the packages
                    lastWriteTime = null;

                    // Set the index create time to now. This would tell us when we last rebuilt the index.
                    UpdateIndexRefreshTime();
                }

                var packages = GetPackages(lastWriteTime);
                if (packages.Count > 0)
                {
                    EnsureIndexWriter(creatingIndex: lastWriteTime == null);
                    AddPackagesCore(packages, creatingIndex: lastWriteTime == null);
                }

                UpdateLastWriteTime();
            }
        }

        public void UpdatePackage(Package package)
        {
            if (_indexContainsAllVersions)
            {
                //just update everything since the last write time
                UpdateIndex(forceRefresh: false);
                return;
            }

            // when we only store the latest, we can run the rest of this
            if (_getShouldAutoUpdate())
            {
                var packageRegistrationKey = package.PackageRegistrationKey;
                var updateTerm = new Term("PackageRegistrationKey", packageRegistrationKey.ToString(CultureInfo.InvariantCulture));

                if (!package.IsLatest || !package.IsLatestStable)
                {
                    // Someone passed us in a version which was e.g. just unlisted? Or just not the latest version which is what we want to index. Doesn't really matter. We'll find one to index.
                    using (var context = _contextThunk())
                    {
                        var packageRepo = new EntityRepository<Package>(context);
                        package = packageRepo.GetAll()
                            .Where(p => (p.IsLatest || p.IsLatestStable) && p.PackageRegistrationKey == packageRegistrationKey)
                            .Include(p => p.PackageRegistration)
                            .Include(p => p.PackageRegistration.Owners)
                            .Include(p => p.SupportedFrameworks)
                            .FirstOrDefault();
                    }
                }

                EnsureIndexWriter(creatingIndex: false);

                // Just update the provided package
                if (package != null)
                {
                    var document = new PackageIndexEntity(package).ToDocument();

                    DoAndRetryOnOutOfMemory(
                        () => Index.UpdateDocument(updateTerm, document),
                        () => DisposeAndEnsureIndexWriter(creatingIndex: false),
                        waitMilliseconds: 50);
                }
                else
                {
                    DoAndRetryOnOutOfMemory(() => Index.DeleteDocuments(new Term[] { updateTerm }), () => DisposeAndEnsureIndexWriter(creatingIndex: false), waitMilliseconds: 50);
                }

                DoAndRetryOnOutOfMemory(() => Index.Commit(), () => DisposeAndEnsureIndexWriter(creatingIndex: false));
            }
        }

        private List<PackageIndexEntity> GetPackages(DateTime? lastIndexTime)
        {
            using (var context = _contextThunk())
            {
                var packageRepo = new EntityRepository<Package>(context);

                IQueryable<Package> set = packageRepo.GetAll();

                if (lastIndexTime.HasValue)
                {
                    // Retrieve the Latest and LatestStable version of packages if any package for that registration changed since we last updated the index.
                    // We need to do this because some attributes that we index such as DownloadCount are values in the PackageRegistration table that may
                    // update independent of the package.
                    set = set.Where(
                        p => (_indexContainsAllVersions || p.IsLatest || p.IsLatestStable) &&
                             p.PackageRegistration.Packages.Any(p2 => p2.LastUpdated > lastIndexTime)
                    );
                    Trace.WriteLine("Lucene Indexer: Getting changed results from the database since {0}".format_with(lastIndexTime));
                }
                else if (!_indexContainsAllVersions)
                {
                    set = set.Where(p => p.IsLatest || p.IsLatestStable); // which implies that p.IsListed by the way!
                    Trace.WriteLine("Lucene Indexer: Getting latest/latest stable results from the database");
                }
                else
                {
                    // get everything including unlisted
                    set = set.Where(p => p.StatusForDatabase != _rejectedStatus || p.StatusForDatabase == null);
                    Trace.WriteLine("Lucene Indexer: Getting all results from the database");
                }

                var list = MethodExtensionWrappers.WrapExecutionTracingTime(() =>
                {
                    return set
                            .Include(p => p.PackageRegistration)
                            .Include(p => p.PackageRegistration.Owners)
                            .Include(p => p.SupportedFrameworks)
                            .ToList();
                }, "Lucene Indexer: Select from database completed");

                var packagesForIndexing = MethodExtensionWrappers.WrapExecutionTracingTime(
                    () =>
                    {
                        return list.Select(
                            p => new PackageIndexEntity
                            {
                                Package = p
                            });
                    },
                    "Lucene Indexer: Converted results from what was pulled");

                return packagesForIndexing.ToList();
            }
        }

        public void AddPackages(IList<PackageIndexEntity> packages, bool creatingIndex)
        {
            if (_getShouldAutoUpdate())
            {
                AddPackagesCore(packages, creatingIndex);
            }
        }

        private void AddPackagesCore(IList<PackageIndexEntity> packages, bool creatingIndex)
        {
            if (!creatingIndex)
            {
                // If this is not the first time we're creating the index, clear any package registrations for packages we are going to updating.
                var packagesToDelete = from packageRegistrationKey in packages.Select(p => p.Package.PackageRegistrationKey).Distinct()
                                       select new Term("PackageRegistrationKey", packageRegistrationKey.ToString(CultureInfo.InvariantCulture));

                DoAndRetryOnOutOfMemory(() => Index.DeleteDocuments(packagesToDelete.ToArray()), () => DisposeAndEnsureIndexWriter(creatingIndex));
            }

            EnsureIndexWriter(creatingIndex);

            // As per http://stackoverflow.com/a/3894582. The IndexWriter is CPU bound, so we can try and write multiple packages in parallel.
            // The IndexWriter is thread safe and is primarily CPU-bound.
            Parallel.ForEach(packages, AddPackage);

            Debug.Assert(Index != null);
            DoAndRetryOnOutOfMemory(() => Index.Commit(), () => DisposeAndEnsureIndexWriter(creatingIndex));
        }

        /// <summary>
        /// Execute a function which may need to be retried upon encountering an OutOfMemoryException.
        /// </summary>
        /// <param name="action">Action to try/retry.</param>
        /// <param name="handleException">Action to take to handle the OutOfMemoryException.</param>
        /// <param name="numberOfAttempts">Number of retry attempts.</param>
        /// <param name="waitMilliseconds">Milliseconds to wait between retry attempts.</param>
        /// <param name="increaseWaitOnRetryMilliseconds">Additional wait time between successive retry attempts if there is a repeated failure.
        /// <remarks>
        /// Per https://lucenenetdocs.azurewebsites.net/api/Lucene.Net/Lucene.Net.Index.IndexWriter.html we should expect and handle OutOfMemoryException.
        /// </remarks>
        private static void DoAndRetryOnOutOfMemory(Action action, Action handleException, int numberOfAttempts = 3, int waitMilliseconds = 100, int increaseWaitOnRetryMilliseconds = 0)
        {
            if (action == null)
            {
                return;
            }

            DoAndRetryOnOutOfMemory(
                () =>
                    {
                        action.Invoke();
                        return true;
                    },
                handleException,
                numberOfAttempts,
                waitMilliseconds,
                increaseWaitOnRetryMilliseconds);
        }

        /// <summary>
        /// Execute a function which may need to be retried upon encountering an OutOfMemoryException.
        /// </summary>
        /// <typeparam name="T">Return type from the function.</typeparam>
        /// <param name="function">Action to try/retry.</param>
        /// <param name="handleException">Action to take to handle the OutOfMemoryException.</param>
        /// <param name="numberOfAttempts">Number of retry attempts.</param>
        /// <param name="waitMilliseconds">Milliseconds to wait between retry attempts.</param>
        /// <param name="increaseWaitOnRetryMilliseconds">Additional wait time between successive retry attempts if there is a repeated failure.</param>
        /// <returns>The result from the executed function upon successful execution.</returns>
        /// <remarks>
        /// Per https://lucenenetdocs.azurewebsites.net/api/Lucene.Net/Lucene.Net.Index.IndexWriter.html we should expect and handle OutOfMemoryException.
        /// </remarks>
        private static T DoAndRetryOnOutOfMemory<T>(Func<T> function, Action handleException, int numberOfAttempts = 3, int waitMilliseconds = 100, int increaseWaitOnRetryMilliseconds = 0)
        {
            if (numberOfAttempts <= 0)
            {
                throw new ArgumentOutOfRangeException(string.Format("The parameter '{0}' must be greater than zero.", numberOfAttempts.GetType().Name));
            }

            var result = default(T);
            if (function == null)
            {
                return result;
            }

            for (int attempts = 1; attempts <= numberOfAttempts; attempts++)
            {
                try
                {
                    result = function.Invoke();
                    break;
                }
                catch (OutOfMemoryException ex)
                {
                    if (attempts >= numberOfAttempts)
                    {
                        throw ex;
                    }
                    else
                    {
                        Trace.WriteLine("Out of Memory exception detected. Executing failure handling.");
                        handleException.Invoke();
                    }

                    Thread.Sleep(waitMilliseconds + (attempts * increaseWaitOnRetryMilliseconds));
                    Trace.WriteLine("Retrying action {0}/{1}.".format_with(attempts, numberOfAttempts));
                }
            }

            return result;
        }

        public virtual DateTime? GetLastWriteTime()
        {
            var metadataPath = LuceneCommon.IndexMetadataPath;
            if (!File.Exists(metadataPath))
            {
                return null;
            }
            return File.GetLastWriteTimeUtc(metadataPath);
        }

        private void AddPackage(PackageIndexEntity packageInfo)
        {
            EnsureIndexWriter(creatingIndex: false);

            DoAndRetryOnOutOfMemory(
                () => Index.AddDocument(packageInfo.ToDocument()),
                handleException: () => DisposeAndEnsureIndexWriter(creatingIndex: false));
        }

        protected void EnsureIndexWriter(bool creatingIndex)
        {
            if (Index == null)
            {
                lock (IndexWriterLock)
                {
                    IndexWriter indexWriter = null;
                    if (WriterCache.TryGetValue(_directory, out indexWriter))
                    {
                        Debug.Assert(indexWriter != null);
                        Index = indexWriter;
                        return;
                    }

                    EnsureIndexWriterCore(creatingIndex);
                }
            }
        }

        protected void DisposeAndEnsureIndexWriter(bool creatingIndex)
        {
            lock (IndexWriterLock)
            {
                // Remove the writer from the cache so the subsequent check forces it to be re-created.
                IndexWriter writer = null;
                bool removed = WriterCache.TryRemove(_directory, out writer);
                Debug.Assert(removed && writer == Index);
                Index.Dispose();
                Index = null;

                EnsureIndexWriter(creatingIndex);
            }
        }

        private void EnsureIndexWriterCore(bool creatingIndex)
        {
            var analyzer = new PerFieldAnalyzer();
            try
            {
                Index = new IndexWriter(_directory, analyzer, create: creatingIndex, mfl: IndexWriter.MaxFieldLength.UNLIMITED);
            }
            catch (LockObtainFailedException ex)
            {
                DirectoryInfo luceneIndexDirectory = new DirectoryInfo(LuceneCommon.IndexDirectory);
                FSDirectory luceneFSDirectory = FSDirectory.Open(luceneIndexDirectory, new Lucene.Net.Store.SimpleFSLockFactory(luceneIndexDirectory));
                IndexWriter.Unlock(luceneFSDirectory);
                Index = new IndexWriter(_directory, analyzer, create: creatingIndex, mfl: IndexWriter.MaxFieldLength.UNLIMITED);

                // Log but swallow the exception
                ErrorSignal.FromCurrentContext().Raise(ex);
            }

            // Should always be add, due to locking
            var got = WriterCache.GetOrAdd(_directory, Index);
            Debug.Assert(got == Index);
        }

        protected internal static bool IndexRequiresRefresh()
        {
            var metadataPath = LuceneCommon.IndexMetadataPath;
            if (File.Exists(metadataPath))
            {
                var creationTime = File.GetCreationTimeUtc(metadataPath);
                return (DateTime.UtcNow - creationTime) > IndexRecreateInterval;
            }

            // If we've never created the index, it needs to be refreshed.
            return true;
        }

        protected internal virtual void UpdateLastWriteTime()
        {
            var metadataPath = LuceneCommon.IndexMetadataPath;
            if (!File.Exists(metadataPath))
            {
                // Create the index and add a timestamp to it that specifies the time at which it was created.
                File.WriteAllBytes(metadataPath, new byte[0]);
            }
            else
            {
                File.SetLastWriteTimeUtc(metadataPath, DateTime.UtcNow);
            }
        }

        protected static void UpdateIndexRefreshTime()
        {
            var metadataPath = LuceneCommon.IndexMetadataPath;
            if (File.Exists(metadataPath))
            {
                File.SetCreationTimeUtc(metadataPath, DateTime.UtcNow);
            }
        }

        public int GetDocumentCount()
        {
            using (IndexReader reader = IndexReader.Open(_directory, readOnly: true))
            {
                return reader.NumDocs();
            }
        }

        public long GetIndexSizeInBytes()
        {
            var path = IndexPath;
            return CalculateSize(new DirectoryInfo(path));
        }

        private long CalculateSize(DirectoryInfo dir)
        {
            if (!dir.Exists)
            {
                return 0;
            }

            return
                dir.EnumerateFiles().Sum(f => f.Length) +
                dir.EnumerateDirectories().Select(d => CalculateSize(d)).Sum();
        }

        public void RegisterBackgroundJobs(IList<IJob> jobs)
        {
            if (_getShouldAutoUpdate())
            {
                jobs.Add(
                    new LuceneIndexingJob(
                        frequence: TimeSpan.FromMinutes(LUCENE_UPDATE_JOB_FREQUENCY_MINUTES),
                        timeout: TimeSpan.FromMinutes(LUCENE_UPDATE_JOB_TIMEOUT_MINUTES),
                        indexingService: this));
            }
        }
    }
}
