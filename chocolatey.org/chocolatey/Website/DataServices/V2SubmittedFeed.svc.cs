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
using System.Data.Entity;
using System.Data.Services;
using System.Linq;
using System.ServiceModel.Web;
using System.Web.Mvc;
using System.Web.Routing;

namespace NuGetGallery
{
    public class V2SubmittedFeed : FeedServiceBase<V2FeedPackage>
    {
        private const int FeedVersion = 2;
        private readonly string submittedStatus = PackageStatusType.Submitted.ToStringSafe();

        public V2SubmittedFeed()
        {
        }

        public V2SubmittedFeed(IEntitiesContext entities, IEntityRepository<Package> repo, IConfiguration configuration, ISearchService searchService) : base(entities, repo, configuration, searchService)
        {
        }

        protected override FeedContext<V2FeedPackage> CreateDataSource()
        {
            return new FeedContext<V2FeedPackage>
            {
                Packages = PackageRepo.GetAll().Where(p => p.StatusForDatabase == submittedStatus).WithoutVersionSort().ToV2FeedPackageQuery(GetSiteRoot())
            };
        }

        public static void InitializeService(DataServiceConfiguration config)
        {
            InitializeServiceBase(config);
        }

        [WebGet]
        public IQueryable<V2FeedPackage> Search(string searchTerm, string targetFramework, bool includePrerelease)
        {
            var packages = PackageRepo.GetAll().Where(p => p.StatusForDatabase == PackageStatusType.Submitted.GetDescriptionOrValue());
            var packageVersions = SearchCore(packages, searchTerm, targetFramework, includePrerelease, SearchFilter.Empty(isValid:true), useCache:false).ToV2FeedPackageQuery(GetSiteRoot()).ToList();

            if (!includePrerelease)
            {
                return packageVersions.Where(p => !p.IsPrerelease).AsQueryable();
            }

            return packageVersions.AsQueryable();
        }

        [WebGet]
        public IQueryable<V2FeedPackage> FindPackagesById(string id)
        {
            return
                PackageRepo.GetAll()
                           .Include(p => p.PackageRegistration)
                           .Where(p => p.PackageRegistration.Id.Equals(id, StringComparison.OrdinalIgnoreCase) && p.StatusForDatabase == submittedStatus)
                           .ToV2FeedPackageQuery(GetSiteRoot());
        }

        public override Uri GetReadStreamUri(object entity, DataServiceOperationContext operationContext)
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
    }
}
