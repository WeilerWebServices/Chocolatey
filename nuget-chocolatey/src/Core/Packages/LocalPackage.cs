﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Versioning;

namespace NuGet
{
    public abstract class LocalPackage : IPackage
    {
        private const string ResourceAssemblyExtension = ".resources.dll";
        private IList<IPackageAssemblyReference> _assemblyReferences;

        protected LocalPackage()
        {
            // local packages are typically listed; exception is with those served by NuGet.Server when delist feature is turned on
            Listed = true;
        }

        public string Id
        {
            get;
            set;
        }

        public SemanticVersion Version
        {
            get;
            set;
        }

        public string Title
        {
            get;
            set;
        }

        public IEnumerable<string> Authors
        {
            get;
            set;
        }

        public IEnumerable<string> Owners
        {
            get;
            set;
        }

        public Uri IconUrl
        {
            get;
            set;
        }

        public Uri LicenseUrl
        {
            get;
            set;
        }

        public Uri ProjectUrl
        {
            get;
            set;
        }
        
        public bool RequireLicenseAcceptance
        {
            get;
            set;
        }

        public bool DevelopmentDependency
        {
            get;
            set;
        }

        public string Description
        {
            get;
            set;
        }

        public string Summary
        {
            get;
            set;
        }

        public string ReleaseNotes
        {
            get;
            set;
        }

        public string Language
        {
            get;
            set;
        }

        public string Tags
        {
            get;
            set;
        }

        public Uri ProjectSourceUrl { get; set; }
        public Uri PackageSourceUrl { get; set; }
        public Uri DocsUrl { get; set; }
        public Uri WikiUrl { get; set; }
        public Uri MailingListUrl { get; set; }
        public Uri BugTrackerUrl { get; set; }
        public IEnumerable<string> Replaces { get; set; }
        public IEnumerable<string> Provides { get; set; }
        public IEnumerable<string> Conflicts { get; set; }

        public string SoftwareDisplayName { get; set; }
        public string SoftwareDisplayVersion { get; set; }

        public Version MinClientVersion
        {
            get;
            private set;
        }

        public bool IsAbsoluteLatestVersion
        {
            get
            {
                return true;
            }
        }

        public bool IsLatestVersion
        {
            get
            {
                return this.IsReleaseVersion();
            }
        }

        public bool Listed
        {
            get;
            set;
        }

        public DateTimeOffset? Published
        {
            get;
            set;
        }

        public string Copyright
        {
            get;
            set;
        }

        public IEnumerable<PackageDependencySet> DependencySets
        {
            get;
            set;
        }

        public IEnumerable<FrameworkAssemblyReference> FrameworkAssemblies
        {
            get;
            set;
        }

        public IEnumerable<IPackageAssemblyReference> AssemblyReferences
        {
            get
            {
                if (_assemblyReferences == null)
                {
                    _assemblyReferences = GetAssemblyReferencesCore().ToList();
                }

                return _assemblyReferences;
            }
        }

        public ICollection<PackageReferenceSet> PackageAssemblyReferences
        {
            get;
            private set;
        }

        #region Server Metadata Only
        public string PackageHash
        {
            get
            {
                return string.Empty;
            }
        }

        public string PackageHashAlgorithm
        {
            get
            {
                return string.Empty;
            }
        }

        public long PackageSize
        {
            get
            {
                return -1;
            }
        }

        public Uri ReportAbuseUrl
        {
            get
            {
                return null;
            }
        }

        public int DownloadCount
        {
            get
            {
                return -1;
            }
        }

        public int VersionDownloadCount
        {
            get
            {
                return -1;
            }
        }

        public bool IsApproved
        {
            get
            {
                return false;
            }
        }

        public string PackageStatus
        {
            get
            {
                return "Unknown";
            }
        }

        public string PackageSubmittedStatus
        {
            get
            {
                return "Unknown";
            }
        }

        public string PackageTestResultStatus
        {
            get
            {
                return "Unknown";
            }
        }

        public DateTime? PackageTestResultStatusDate
        {
            get
            {
                return null;
            }
        }

        public string PackageValidationResultStatus
        {
            get
            {
                return "Unknown";
            }
        }

        public DateTime? PackageValidationResultDate
        {
            get
            {
                return null;
            }
        }

        public DateTime? PackageCleanupResultDate
        {
            get
            {
                return null;
            }
        }

        public DateTime? PackageReviewedDate
        {
            get
            {
                return null;
            }
        }

        public DateTime? PackageApprovedDate
        {
            get
            {
                return null;
            }
        }

        public string PackageReviewer
        {
            get
            {
                return string.Empty;
            }
        }

        public bool IsDownloadCacheAvailable
        {
            get
            {
                return false;
            }
        }

        public DateTime? DownloadCacheDate
        {
            get
            {
                return null;
            }
        }

        public IEnumerable<DownloadCache> DownloadCache
        {
            get
            {
                return Enumerable.Empty<DownloadCache>();
            }
        }

        #endregion
        
        public virtual IEnumerable<FrameworkName> GetSupportedFrameworks()
        {
            return FrameworkAssemblies.SelectMany(f => f.SupportedFrameworks).Distinct();
        }

        public IEnumerable<IPackageFile> GetFiles()
        {
            return GetFilesBase();
        }

        public abstract Stream GetStream();

        public abstract void ExtractContents(IFileSystem fileSystem, string extractPath);
        
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1024:UsePropertiesWhereAppropriate", Justification = "This operation can be expensive.")]
        protected abstract IEnumerable<IPackageFile> GetFilesBase();

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1024:UsePropertiesWhereAppropriate", Justification = "This operation can be expensive.")]
        protected abstract IEnumerable<IPackageAssemblyReference> GetAssemblyReferencesCore();

        protected void ReadManifest(Stream manifestStream)
        {
            Manifest manifest = Manifest.ReadFrom(manifestStream, validateSchema: false);

            IPackageMetadata metadata = manifest.Metadata;

            Id = metadata.Id;
            Version = metadata.Version;
            Title = metadata.Title;
            Authors = metadata.Authors;
            Owners = metadata.Owners;
            IconUrl = metadata.IconUrl;
            LicenseUrl = metadata.LicenseUrl;
            ProjectUrl = metadata.ProjectUrl;
            RequireLicenseAcceptance = metadata.RequireLicenseAcceptance;
            DevelopmentDependency = metadata.DevelopmentDependency;
            Description = metadata.Description;
            Summary = metadata.Summary;
            ReleaseNotes = metadata.ReleaseNotes;
            Language = metadata.Language;
            Tags = metadata.Tags;
            ProjectSourceUrl = metadata.ProjectSourceUrl;
            PackageSourceUrl = metadata.PackageSourceUrl;
            DocsUrl = metadata.DocsUrl;
            WikiUrl = metadata.WikiUrl;
            MailingListUrl = metadata.MailingListUrl;
            BugTrackerUrl = metadata.BugTrackerUrl;
            Replaces = metadata.Replaces;
            Provides = metadata.Provides;
            Conflicts = metadata.Conflicts;
            DependencySets = metadata.DependencySets;
            FrameworkAssemblies = metadata.FrameworkAssemblies;
            Copyright = metadata.Copyright;
            PackageAssemblyReferences = metadata.PackageAssemblyReferences;
            MinClientVersion = metadata.MinClientVersion;

            // Ensure tags start and end with an empty " " so we can do contains filtering reliably
            if (!String.IsNullOrEmpty(Tags))
            {
                Tags = " " + Tags + " ";
            }
        }

        internal protected static bool IsAssemblyReference(string filePath)
        {           
            // assembly reference must be under lib/
            if (!filePath.StartsWith(Constants.LibDirectory + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            var fileName = Path.GetFileName(filePath);

            // if it's an empty folder, yes
            if (fileName == Constants.PackageEmptyFileName)
            {
                return true;
            }

            // Assembly reference must have a .dll|.exe|.winmd extension and is not a resource assembly;
            return !filePath.EndsWith(ResourceAssemblyExtension, StringComparison.OrdinalIgnoreCase) &&
                Constants.AssemblyReferencesExtensions.Contains(Path.GetExtension(filePath), StringComparer.OrdinalIgnoreCase);
        }

        public override string ToString()
        {
            // extension method, must have 'this'.
            return this.GetFullName();
        }
        
        public void OverrideOriginalVersion(SemanticVersion version)
        {
            if (version != null) Version = version;
        }
    }
}