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
using System.Linq;
using System.ServiceModel.Syndication;
using System.Web.Mvc;
using System.Web.UI;
using NuGetGallery.MvcOverrides;
using NugetGallery;

namespace NuGetGallery
{
    public partial class RssController : Controller
    {
        private readonly IPackageService packageSvc;
        public IConfiguration Configuration { get; set; }

        public RssController(IPackageService packageSvc, IConfiguration configuration)
        {
            this.packageSvc = packageSvc;
            Configuration = configuration;
        }

        [ActionName("feed.rss"), HttpGet, OutputCache(VaryByParam = "page;pageSize", Location = OutputCacheLocation.Any, Duration = 3630)]
        public virtual ActionResult Feed(int? page, int? pageSize)
        {
            var siteRoot = EnsureTrailingSlash(Configuration.GetSiteRoot(AppHarbor.IsSecureConnection(HttpContext)));

            IEnumerable<Package> packageVersions = Cache.Get(string.Format("packageVersions-False"),
                   DateTime.UtcNow.AddMinutes(Cache.DEFAULT_CACHE_TIME_MINUTES),
                   () => packageSvc.GetPackagesForListing(includePrerelease: false).OrderByDescending(p => p.Published).ToList()
            );

            if (page != null && pageSize != null)
            {
                int skip = page.GetValueOrDefault() * pageSize.GetValueOrDefault(1);
                packageVersions = packageVersions.Skip(skip).Take(pageSize.GetValueOrDefault(1));
            } else if (pageSize != null) packageVersions = packageVersions.Take(pageSize.GetValueOrDefault(1));

            var feed = new SyndicationFeed("Chocolatey", "Chocolatey Packages", new Uri(siteRoot));
            feed.Copyright = new TextSyndicationContent("Chocolatey copyright RealDimensions Software, LLC, Packages copyright original maintainer(s), Products copyright original author(s).");
            feed.Language = "en-US";

            var items = new List<SyndicationItem>();
            foreach (Package package in packageVersions.ToList().OrEmptyListIfNull())
            {
                string title = string.Format("{0} ({1})", package.PackageRegistration.Id, package.Version);
                var galleryUrl = siteRoot + "packages/" + package.PackageRegistration.Id + "/" + package.Version;
                var item = new SyndicationItem(title, package.Summary, new Uri(galleryUrl), package.PackageRegistration.Id + "." + package.Version, package.Published);
                item.PublishDate = package.Published;

                items.Add(item);
            }

            try
            {
                var mostRecentPackage = packageVersions.FirstOrDefault();
                feed.LastUpdatedTime = mostRecentPackage == null ? DateTime.UtcNow : mostRecentPackage.Published;
            } catch (Exception)
            {
                feed.LastUpdatedTime = DateTime.UtcNow;
            }

            feed.Items = items;

            return new RSSActionResult {
                Feed = feed
            };
        }

        private static string EnsureTrailingSlash(string siteRoot)
        {
            if (!siteRoot.EndsWith("/", StringComparison.Ordinal)) siteRoot = siteRoot + '/';
            return siteRoot;
        }
    }
}
