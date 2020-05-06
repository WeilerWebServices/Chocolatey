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
using System.Collections;
using System.Collections.Generic;
using System.Data.Services;
using System.Data.Services.Common;
using System.Data.Services.Providers;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.ServiceModel;
using System.Text.RegularExpressions;
using System.Web;
using System.Web.Mvc;
using Elmah;
using NuGetGallery.MvcOverrides;
using QueryInterceptor;

namespace NuGetGallery
{
    [ServiceBehavior(IncludeExceptionDetailInFaults = true, ConcurrencyMode = ConcurrencyMode.Multiple)]
    public abstract class FeedServiceBase<TPackage> : DataService<FeedContext<TPackage>>, IDataServiceStreamProvider, IServiceProvider, IDataServicePagingProvider
    {
        static readonly Regex packagesByIdPathRegexV1 = new Regex(@"/api/v1/Packages\(.*\)", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        static readonly Regex packagesByIdPathRegexV2 = new Regex(@"/api/v2/Packages\(.*\)", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        /// <summary>
        ///   Determines the maximum number of packages returned in a single page of an OData result.
        /// </summary>
        private const int MaxPageSize = 40;
        private readonly IEntitiesContext entities;
        private readonly IEntityRepository<Package> packageRepo;
        private readonly IConfiguration configuration;
        private HttpContextBase httpContext;
        private int _currentSkip;
        private object[] _continuationToken;
        private readonly Type _packageType;
        protected readonly ISearchService searchService;


        public FeedServiceBase()
            : this(
                DependencyResolver.Current.GetService<IEntitiesContext>(), DependencyResolver.Current.GetService<IEntityRepository<Package>>(), DependencyResolver.Current.GetService<IConfiguration>(), DependencyResolver.Current.GetService<ISearchService>())
        {
        }

        protected FeedServiceBase(IEntitiesContext entities, IEntityRepository<Package> packageRepo, IConfiguration configuration, ISearchService searchService)
        {
            this.entities = entities;
            this.packageRepo = packageRepo;
            this.configuration = configuration;
            this.searchService = searchService;
            _currentSkip = 0;
            _packageType = typeof(TPackage);
        }

        protected IEntitiesContext Entities { get { return entities; } }

        protected IEntityRepository<Package> PackageRepo { get { return packageRepo; } }

        protected IConfiguration Configuration { get { return configuration; } }

        protected ISearchService SearchService { get { return searchService; } }

        protected internal virtual HttpContextBase HttpContext
        {
            get { return httpContext ?? new HttpContextWrapper(System.Web.HttpContext.Current); }
            set { httpContext = value; }
        }

        protected internal string SiteRoot
        {
            get
            {
                string siteRoot = Configuration.GetSiteRoot(UseHttps());
                return EnsureTrailingSlash(siteRoot);
            }
        }

        // This method is called only once to initialize service-wide policies.
        protected static void InitializeServiceBase(DataServiceConfiguration config)
        {
            config.SetServiceOperationAccessRule("Search", ServiceOperationRights.AllRead);
            config.SetServiceOperationAccessRule("FindPackagesById", ServiceOperationRights.AllRead);
            config.SetEntitySetAccessRule("Packages", EntitySetRights.AllRead);
            config.SetEntitySetPageSize("Packages", MaxPageSize);
            config.DataServiceBehavior.MaxProtocolVersion = DataServiceProtocolVersion.V2;
            config.UseVerboseErrors = true;
        }

        public void DeleteStream(object entity, DataServiceOperationContext operationContext)
        {
            throw new NotSupportedException();
        }

        public Stream GetReadStream(object entity, string etag, bool? checkETagForEquality, DataServiceOperationContext operationContext)
        {
            throw new NotSupportedException();
        }

        public abstract Uri GetReadStreamUri(object entity, DataServiceOperationContext operationContext);

        public string GetStreamContentType(object entity, DataServiceOperationContext operationContext)
        {
            return "application/zip";
        }

        public string GetStreamETag(object entity, DataServiceOperationContext operationContext)
        {
            return null;
        }

        public Stream GetWriteStream(object entity, string etag, bool? checkETagForEquality, DataServiceOperationContext operationContext)
        {
            throw new NotSupportedException();
        }

        public string ResolveType(string entitySetName, DataServiceOperationContext operationContext)
        {
            throw new NotSupportedException();
        }

        public int StreamBufferSize { get { return 64000; } }

        public object GetService(Type serviceType)
        {
            if (serviceType == typeof(IDataServiceStreamProvider)) return this;
            // At the very least FindPackagesById needs to be using the SearchFilter for this to work
            //if (serviceType == typeof(IDataServicePagingProvider)) return this;

            return null;
        }

        protected virtual IQueryable<Package> SearchCore(IQueryable<Package> packages, string searchTerm, string targetFramework, bool includePrerelease, SearchFilter searchFilter, bool useCache)
        {
            // we don't allow an empty search for all versions.
            if (searchFilter.FilterInvalidReason == SearchFilterInvalidReason.DueToAllVersionsRequested && string.IsNullOrWhiteSpace(searchTerm))
            {
                searchFilter.IsValid = true;
            }

            // When the SearchFilter is valid, we can use Lucene
            // We can only use Lucene if the client queries for the latest versions (IsLatest \ IsLatestStable) versions of a package
            // and specific sort orders that we have in the index.
            if (searchFilter.IsValid)
            {
                searchFilter.SearchTerm = searchTerm;
                searchFilter.IncludePrerelease = includePrerelease;

                return GetResultsFromSearchService(searchFilter);
            }

            var invalidSearchFilterMessage = "Search filter was invalid ('{0}') Raw Url: '{1}' .".format_with(searchFilter.FilterInvalidReason, HttpContext.Request.RawUrl);
            Trace.WriteLine(invalidSearchFilterMessage);
            // raise this as an exception for even better visibility
            ErrorSignal.FromCurrentContext().Raise(new SystemException(invalidSearchFilterMessage));

            if (!includePrerelease)
            {
                packages = packages.Where(p => !p.IsPrerelease);
            }

            if (useCache)
            {
                return NugetGallery.Cache.Get(
                    string.Format(
                        "V2Feed-Search-{0}-{1}-{2}",
                        searchTerm.to_lower(),
                        targetFramework.to_lower(),
                        includePrerelease
                        ),
                    DateTime.UtcNow.AddSeconds(3600),
                    () =>
                    {
                        Trace.WriteLine("Database search results hit for API (caching results) Search term: '{0}' (prerelease? {1}).".format_with(searchTerm, includePrerelease));
                        return packages.Search(searchTerm, lowerCaseExpression: false).ToList();

                    }).AsQueryable();
            }

            Trace.WriteLine("Database search results hit for API (not caching results) Search term: '{0}' (prerelease? {1}).".format_with(searchTerm, includePrerelease));


            if (searchFilter.Skip != 0)
            {
                packages = packages.Skip(searchFilter.Skip);
            }

            packages = packages.Take(MaxPageSize);
            if (searchFilter.Take != 0)
            {
                packages = packages.Take(searchFilter.Take);
            }

            switch (searchFilter.SortProperty)
            {
                case SortProperty.Relevance:
                    //do not change the search order
                    break;
                case SortProperty.DownloadCount:
                    packages = packages.OrderByDescending(p => p.PackageRegistration.DownloadCount);
                    break;
                case SortProperty.DisplayName:
                    packages = searchFilter.SortDirection == SortDirection.Ascending ? packages.OrderBy(p => p.Title) : packages.OrderByDescending(p => p.Title);
                    break;
                case SortProperty.Recent:
                    packages = packages.OrderByDescending(p => p.Published);
                    break;
                default:
                    //do not change the search order
                    break;
            }

            return packages.Search(searchTerm, lowerCaseExpression: false);
        }
       
         internal IQueryable<Package> GetResultsFromSearchService(SearchFilter searchFilter)
         {
             var result = SearchService.Search(searchFilter);
 
             // For count queries, we can ask the SearchService to not filter the source results. This would avoid hitting the database and consequently make
             // it very fast.
             if (searchFilter.CountOnly)
             {
                 // At this point, we already know what the total count is. We can have it return this value very quickly without doing any SQL.
                 return result.Data.InterceptWith(new CountInterceptor(result.Hits));
             }
 
             // For relevance search, Lucene returns us a paged\sorted list. OData tries to apply default ordering and Take \ Skip on top of this.
             // We avoid it by yanking these expressions out of out the tree.
             return result.Data.InterceptWith(new DisregardODataInterceptor());
         }

         protected static SearchFilter GetSearchFilter(bool allVersionsInIndex, string url, string searchTerm, bool includePrerelease)
         {
             if (url == null)
             {
                 return SearchFilter.Empty();
             }

             int indexOfQuestionMark = url.IndexOf('?');
             if (indexOfQuestionMark == -1)
             {
                 return SearchFilter.Empty();
             }

             string path = url.Substring(0, indexOfQuestionMark);
             string query = url.Substring(indexOfQuestionMark + 1);

             if (string.IsNullOrEmpty(query))
             {
                 return SearchFilter.Empty();
             }

             var searchFilter = new SearchFilter()
             {
                 // The way the default paging works is WCF attempts to read up to the MaxPageSize elements. If it finds as many, it'll assume there 
                 // are more elements to be paged and generate a continuation link. Consequently we'll always ask to pull MaxPageSize elements so WCF generates the 
                 // link for us and then allow it to do a Take on the results. Further down, we'll also parse $skiptoken as a custom IDataServicePagingProvider
                 // sneakily injects the Skip value in the continuation token.
                 Take = MaxPageSize,
                 Skip = 0,
                 CountOnly = path.EndsWith("$count", StringComparison.Ordinal),
                 IsValid = true,
                 SearchTerm = searchTerm,
                 IncludePrerelease = includePrerelease,
             };

             string[] props = query.Split('&');

             IDictionary<string, string> queryTerms = new Dictionary<string, string>();
             foreach (string prop in props)
             {
                 string[] nameValue = prop.Split('=');
                 if (nameValue.Length == 2)
                 {
                     queryTerms[nameValue[0]] = nameValue[1];
                 }
             }
             searchFilter.QueryTerms = queryTerms;

             string filter;
             if (queryTerms.TryGetValue("$filter", out filter))
             {
                 if (!(filter.Equals("IsLatestVersion", StringComparison.Ordinal) || filter.Equals("IsAbsoluteLatestVersion", StringComparison.Ordinal) 
                   || filter.Contains("IsLatestVersion") || filter.Contains("IsAbsoluteLatestVersion")))
                 {
                     searchFilter.IncludeAllVersions = true;
                 }
             }
             else
             {
                 searchFilter.IncludeAllVersions = true;
             }

             // We'll only use the index if we the query searches for latest \ latest-stable packages
             // if all versions are not available in the index
             if (searchFilter.IncludeAllVersions && !allVersionsInIndex)
             {
                 searchFilter.IsValid = false;
                 searchFilter.FilterInvalidReason = SearchFilterInvalidReason.DueToAllVersionsRequested;
             }
             
             string skipStr;
             if (queryTerms.TryGetValue("$skip", out skipStr))
             {
                 int skip;
                 if (int.TryParse(skipStr, out skip))
                 {
                     searchFilter.Skip = skip;
                 }
             } 

             string topStr;
             if (queryTerms.TryGetValue("$top", out topStr))
             {
                 int top;
                 if (int.TryParse(topStr, out top))
                 {
                     searchFilter.Take = Math.Min(top, MaxPageSize);
                 }
             }

             string skipTokenStr;
             if (queryTerms.TryGetValue("$skiptoken", out skipTokenStr))
             {
                 var skipTokenParts = skipTokenStr.Split(',');
                 if (skipTokenParts.Length == 3) // this means our custom IDataServicePagingProvider did its magic by sneaking the Skip value into the SkipToken
                 {
                     int skip;
                     if (int.TryParse(skipTokenParts[2], out skip))
                     {
                         searchFilter.Skip = skip;
                     }
                 }
             }

             //  only certain orderBy clauses are supported from the Lucene search
             string orderBy;
             if (queryTerms.TryGetValue("$orderby", out orderBy))
             {
                 if (string.IsNullOrEmpty(orderBy))
                 {
                     searchFilter.SortProperty = SortProperty.Relevance;
                 }
                 else if (orderBy.StartsWith("DownloadCount", StringComparison.Ordinal))
                 {
                     searchFilter.SortProperty = SortProperty.DownloadCount;
                 }
                 else if (orderBy.StartsWith("Published", StringComparison.Ordinal))
                 {
                     searchFilter.SortProperty = SortProperty.Recent;
                 }
                 else if (orderBy.StartsWith("LastEdited", StringComparison.Ordinal))
                 {
                     searchFilter.SortProperty = SortProperty.Recent;
                 }
                 else if (orderBy.StartsWith("Id", StringComparison.Ordinal))
                 {
                     searchFilter.SortProperty = SortProperty.DisplayName;
                 }
                 else if (orderBy.StartsWith("concat", StringComparison.Ordinal))
                 {
                     searchFilter.SortProperty = SortProperty.DisplayName;

                     if (orderBy.Contains("%20desc"))
                     {
                         searchFilter.SortDirection = SortDirection.Descending;
                     }
                 }
                 else
                 {
                     searchFilter.IsValid = false;
                 }
             }
             else
             {
                 searchFilter.SortProperty = SortProperty.Relevance;
             }

            return searchFilter;
        }

        protected virtual bool UseHttps()
        {
            return AppHarbor.IsSecureConnection(HttpContext);
        }

        private static string EnsureTrailingSlash(string siteRoot)
        {
            if (!siteRoot.EndsWith("/", StringComparison.Ordinal)) siteRoot = siteRoot + '/';
            return siteRoot;
        }

        public void SetContinuationToken(IQueryable query, ResourceType resourceType, object[] continuationToken)
        {
            if (resourceType.FullName != _packageType.FullName) throw new ArgumentException("The paging provider can not construct a meaningful continuation token because its type is different from the ResourceType for which a continuation token is requested.");

            var materializedQuery = (query as IQueryable<TPackage>).ToList();
            var lastElement = materializedQuery.LastOrDefault();
            if (lastElement != null && materializedQuery.Count == MaxPageSize)
            {
                string packageId = _packageType.GetProperty("Id").GetValue(lastElement, null).ToString();
                string packageVersion = _packageType.GetProperty("Version").GetValue(lastElement, null).ToString();
                _continuationToken = new object[] { packageId, packageVersion, _currentSkip + Math.Min(materializedQuery.Count, MaxPageSize) };
            } else _continuationToken = null;
        }

        public object[] GetContinuationToken(IEnumerator enumerator)
        {
            return _continuationToken;
        }

        protected override void OnStartProcessingRequest(ProcessRequestArgs args)
        {
            base.OnStartProcessingRequest(args);

            if (ShouldCacheOutput(HttpContext))
            {
                var cache = HttpContext.Response.Cache;
                cache.SetCacheability(HttpCacheability.ServerAndPrivate);
                cache.SetExpires(DateTime.UtcNow.AddMinutes(5));

                cache.VaryByHeaders["Accept"] = true;
                cache.VaryByHeaders["Accept-Charset"] = true;
                cache.VaryByHeaders["Accept-Encoding"] = true;
                cache.VaryByParams["*"] = true;

                cache.SetValidUntilExpires(true);
            }
        }

        private static bool ShouldCacheOutput(HttpContextBase context)
        {
            try
            {
                return context.Request.HttpMethod.Equals("GET", StringComparison.OrdinalIgnoreCase)
               && string.IsNullOrEmpty(context.Request.Url.Query)
               && (packagesByIdPathRegexV2.IsMatch(context.Request.Path) || packagesByIdPathRegexV1.IsMatch(context.Request.Path));
            }
            catch (Exception)
            {
                return false;
            }
        }
    }
}
