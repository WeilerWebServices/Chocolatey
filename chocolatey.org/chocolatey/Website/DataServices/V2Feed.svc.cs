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
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.Services;
using System.Globalization;
using System.Linq;
using System.Runtime.Versioning;
using System.ServiceModel.Web;
using System.Web;
using System.Web.Caching;
using System.Web.Mvc;
using System.Web.Routing;
using NuGet;

namespace NuGetGallery
{
    public class V2Feed : FeedServiceBase<V2FeedPackage>
    {
        private const int FeedVersion = 2;
        private const int DEFAULT_CACHE_TIME_SECONDS_V2FEED = 60;
        private readonly string _rejectedStatus = PackageStatusType.Rejected.GetDescriptionOrValue();

        public V2Feed()
        {
        }

        public V2Feed(IEntitiesContext entities, IEntityRepository<Package> repo, IConfiguration configuration, ISearchService searchService)
            : base(entities, repo, configuration, searchService)
        {
        }

        protected override FeedContext<V2FeedPackage> CreateDataSource()
        {
            return new FeedContext<V2FeedPackage>
            {
                Packages = PackageRepo.GetAll()
                                      .WithoutVersionSort()
                                      .ToV2FeedPackageQuery(Configuration.GetSiteRoot(UseHttps()))
            };
        }

        public static void InitializeService(DataServiceConfiguration config)
        {
            InitializeServiceBase(config);
            config.SetServiceOperationAccessRule("GetUpdates", ServiceOperationRights.AllRead);
        }

        [WebGet]
        public IQueryable<V2FeedPackage> Search(string searchTerm, string targetFramework, bool includePrerelease)
        {
            var packages = PackageRepo.GetAll()
                                      .Include(p => p.Authors)
                                      .Include(p => p.PackageRegistration)
                                      .Include(p => p.PackageRegistration.Owners)
                                      .Where(p => p.Listed);

            // Check if the caller is requesting packages or calling the count operation.
            bool requestingCount = HttpContext.Request.RawUrl.Contains("$count");

            var isEmptySearchTerm = string.IsNullOrEmpty(searchTerm);
            if ((requestingCount && isEmptySearchTerm) || isEmptySearchTerm)
            {
                // Fetch the cache key for the empty search query.
                string cacheKey = GetCacheKeyForEmptySearchQuery(includePrerelease);

                IQueryable<V2FeedPackage> searchResults;
                DateTime lastModified;

                var cachedObject = HttpContext.Cache.Get(cacheKey);
                var currentDateTime = DateTime.UtcNow;
                if (cachedObject == null && !requestingCount)
                {
                    searchResults = SearchV2FeedCore(packages, searchTerm, targetFramework, includePrerelease, useCache: false);

                    lastModified = currentDateTime;

                    // cache the most common search query
                    // note: this is per instance cache
                    var cachedPackages = searchResults.ToList();

                    // don't cache empty results in case we missed any potential ODATA expressions
                    if (!cachedPackages.Any())
                    {
                        var cachedSearchResult = new CachedSearchResult();
                        cachedSearchResult.LastModified = currentDateTime;
                        cachedSearchResult.Packages = cachedPackages;

                        HttpContext.Cache.Add(
                            cacheKey,
                            cachedSearchResult,
                            null,
                            currentDateTime.AddSeconds(DEFAULT_CACHE_TIME_SECONDS_V2FEED),
                            Cache.NoSlidingExpiration,
                            CacheItemPriority.Default,
                            null);
                    }
                }
                else if (cachedObject == null)
                {
                    // first hit on $count and nothing in cache yet;
                    // we can't cache due to the $count hijack in SearchV2FeedCore...
                    return SearchV2FeedCore(packages, searchTerm, targetFramework, includePrerelease, useCache: false);
                }
                else
                {
                    var cachedSearchResult = (CachedSearchResult)cachedObject;
                    searchResults = cachedSearchResult.Packages.AsQueryable();
                    lastModified = cachedSearchResult.LastModified;
                }
                // Clients should cache twice as long.
                // This way, they won't notice differences in the short-lived per instance cache.
                HttpContext.Response.Cache.SetCacheability(HttpCacheability.Public);
                HttpContext.Response.Cache.SetMaxAge(TimeSpan.FromSeconds(60));
                HttpContext.Response.Cache.SetExpires(currentDateTime.AddSeconds(DEFAULT_CACHE_TIME_SECONDS_V2FEED * 2));
                HttpContext.Response.Cache.SetLastModified(lastModified);
                HttpContext.Response.Cache.SetValidUntilExpires(true);

                return searchResults;
            }

            return SearchV2FeedCore(packages, searchTerm, targetFramework, includePrerelease, useCache: true);
        }

        private IQueryable<V2FeedPackage> SearchV2FeedCore(IQueryable<Package> packages, string searchTerm, string targetFramework, bool includePrerelease, bool useCache)
        {
            var searchFilter = GetSearchFilter(searchService.ContainsAllVersions, HttpContext.Request.RawUrl, searchTerm, includePrerelease);
            
            return SearchCore(packages, searchTerm, targetFramework, includePrerelease, searchFilter, useCache: useCache).ToV2FeedPackageQuery(GetSiteRoot());
        }

        [WebGet]
        public IQueryable<V2FeedPackage> FindPackagesById(string id)
        {
            var packages = PackageRepo.GetAll()
                            .Include(p => p.PackageRegistration)
                            .Where(p => p.PackageRegistration.Id.Equals(id) && (p.StatusForDatabase != _rejectedStatus || p.StatusForDatabase == null));

            //if (searchService.ContainsAllVersions)
            //{
            //    return NugetGallery.Cache.Get(
            //        string.Format("V2Feed-FindPackagesById-{0}", id.to_lower()),
            //        DateTime.UtcNow.AddSeconds(DEFAULT_CACHE_TIME_SECONDS_V2FEED),
            //        () =>
            //        {
            //            var searchFilter = GetSearchFilter(searchService.ContainsAllVersions, HttpContext.Request.RawUrl, id, includePrerelease: true);
            //            // Find packages by Id specific items
            //            searchFilter.ExactIdOnly = true;
            //            searchFilter.TakeAllResults = true;
            //            searchFilter.SortProperty = SortProperty.Version;
            //            searchFilter.SortDirection = SortDirection.Descending;

            //            return SearchCore(packages, id, string.Empty, includePrerelease: true, searchFilter: searchFilter, useCache: true)
            //                .ToV2FeedPackageQuery(GetSiteRoot())
            //                .ToList();
            //        }).AsQueryable();
            //}

            return NugetGallery.Cache.Get(
                string.Format("V2Feed-FindPackagesById-{0}", id.to_lower()),
                DateTime.UtcNow.AddSeconds(DEFAULT_CACHE_TIME_SECONDS_V2FEED * 3),
                () => packages
                        .ToV2FeedPackageQuery(GetSiteRoot())
                        .ToList()
            ).AsQueryable();
        }

        [WebGet]
        public IQueryable<V2FeedPackage> GetUpdates(string packageIds, string versions, bool includePrerelease, bool includeAllVersions, string targetFrameworks)
        {
            if (String.IsNullOrEmpty(packageIds) || String.IsNullOrEmpty(versions))
            {
                return Enumerable.Empty<V2FeedPackage>().AsQueryable();
            }

            var idValues = packageIds.Trim().Split(new[] { '|' }, StringSplitOptions.RemoveEmptyEntries);
            var versionValues = versions.Trim().Split(new[] { '|' }, StringSplitOptions.RemoveEmptyEntries);
            var targetFrameworkValues = String.IsNullOrEmpty(targetFrameworks)
                                            ? null
                                            : targetFrameworks.Split('|').Select(VersionUtility.ParseFrameworkName).ToList();

            if ((idValues.Length == 0) || (idValues.Length != versionValues.Length))
            {
                // Exit early if the request looks invalid
                return Enumerable.Empty<V2FeedPackage>().AsQueryable();
            }

            var versionLookup = new Dictionary<string, SemanticVersion>(idValues.Length, StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < idValues.Length; i++)
            {
                var id = idValues[i];
                SemanticVersion version;
                SemanticVersion currentVersion;

                if (SemanticVersion.TryParse(versionValues[i], out currentVersion) &&
                    (!versionLookup.TryGetValue(id, out version) || (currentVersion > version)))
                {
                    // If we've never added the package to lookup or we encounter the same id but with a higher version, then choose the higher version.
                    versionLookup[id] = currentVersion;
                }
            }

            var packages = PackageRepo.GetAll()
                                      .Include(p => p.PackageRegistration)
                                      .Include(p => p.SupportedFrameworks)
                                      .Where(p => p.Listed && (includePrerelease || !p.IsPrerelease) && idValues.Contains(p.PackageRegistration.Id))
                                      .OrderBy(p => p.PackageRegistration.Id);

            //GetUpdates(string packageIds, string versions, bool includePrerelease, bool includeAllVersions, string targetFrameworks
            return NugetGallery.Cache.Get(
                    string.Format("V2Feed-GetUpdates-{0}-{1}-{2}-{3}", string.Join("|", idValues).to_lower(), string.Join("|", versionValues).to_lower(), includePrerelease, includeAllVersions),
                    DateTime.UtcNow.AddSeconds(DEFAULT_CACHE_TIME_SECONDS_V2FEED),
                    () => GetUpdates(packages, versionLookup, targetFrameworkValues, includeAllVersions).AsQueryable().ToV2FeedPackageQuery(GetSiteRoot()).ToList()).AsQueryable();

            //return searchResults.AsQueryable();
            //return GetUpdates(packages, versionLookup, targetFrameworkValues, includeAllVersions).AsQueryable().ToV2FeedPackageQuery(GetSiteRoot());
        }

        private static IEnumerable<Package> GetUpdates(
            IEnumerable<Package> packages,
            Dictionary<string, SemanticVersion> versionLookup,
            IEnumerable<FrameworkName> targetFrameworkValues,
            bool includeAllVersions)
        {
            var updates = packages.AsEnumerable()
                                  .Where(
                                      p =>
                                      {
                                          // For each package, if the version is higher than the client version and we satisty the target framework, return it.
                                          // TODO: We could optimize for the includeAllVersions case here by short circuiting the operation once we've encountered the highest version
                                          // for a given Id
                                          var version = SemanticVersion.Parse(p.Version);
                                          SemanticVersion clientVersion;
                                          if (versionLookup.TryGetValue(p.PackageRegistration.Id, out clientVersion))
                                          {
                                              var supportedPackageFrameworks = p.SupportedFrameworks.Select(f => f.FrameworkName);

                                              return (version > clientVersion) &&
                                                     (targetFrameworkValues == null || targetFrameworkValues.Any(s => VersionUtility.IsCompatible(s, supportedPackageFrameworks)));
                                          }
                                          return false;
                                      });

            if (!includeAllVersions)
            {
                return updates.GroupBy(p => p.PackageRegistration.Id)
                              .Select(g => g.OrderByDescending(p => SemanticVersion.Parse(p.Version)).First());
            }
            return updates;
        }

        public override Uri GetReadStreamUri(
            object entity,
            DataServiceOperationContext operationContext)
        {
            var package = (V2FeedPackage)entity;
            var urlHelper = new UrlHelper(new RequestContext(HttpContext, new RouteData()));

            string url = urlHelper.PackageDownload(FeedVersion, package.Id, package.Version);

            return new Uri(url, UriKind.Absolute);
        }

        private string GetSiteRoot()
        {
            return Configuration.GetSiteRoot(UseHttps());
        }

        /// <summary>
        ///   The most common search queries should be cached and yield a cache-key.
        /// </summary>
        /// <param name="includePrerelease">
        ///   <code>True</code>, to include prereleases; otherwise <code>false</code>.
        /// </param>
        /// <returns>The cache key for the specified search criteria.</returns>
        private static string GetCacheKeyForEmptySearchQuery(bool includePrerelease)
        {
            string cacheKeyFormat = "V2Feed-Search-{0}";

            string prereleaseKey = "false";
            if (includePrerelease)
            {
                prereleaseKey = "true";
            }

            return string.Format(CultureInfo.InvariantCulture, cacheKeyFormat, prereleaseKey);
        }

        private class CachedSearchResult
        {
            public DateTime LastModified { get; set; }
            public List<V2FeedPackage> Packages { get; set; }
        }
    }
}
