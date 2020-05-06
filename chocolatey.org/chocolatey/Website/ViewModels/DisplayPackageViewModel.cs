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
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Linq;
using System.Web.Mvc;
using NuGet;

namespace NuGetGallery
{
    public class DisplayPackageViewModel : ListPackageItemViewModel
    {
        public DisplayPackageViewModel(Package package, IEnumerable<ScanResult> scanResults)
            : this(package, scanResults, false)
        {
        }

        public DisplayPackageViewModel(Package package, IEnumerable<ScanResult> scanResults, bool isVersionHistory)
            : base(package)
        {
            Copyright = package.Copyright;
            if (!isVersionHistory)
            {
                Dependencies = new DependencySetsViewModel(package.Dependencies);
                PackageVersions = from p in package.PackageRegistration.Packages.ToList()
                                  orderby new SemanticVersion(p.Version) descending
                                  select new DisplayPackageViewModel(p, null, isVersionHistory: true);
            }

            IsTrusted = package.PackageRegistration.IsTrusted;

            Files = package.Files;
            ScanResults = scanResults;
            DownloadCount = package.DownloadCount;
        }

        public DependencySetsViewModel Dependencies { get; set; }
        public IEnumerable<DisplayPackageViewModel> PackageVersions { get; set; }
        public string Copyright { get; set; }

        public bool IsLatestVersionAvailable
        {
            get
            {
                // A package can be identified as the latest available a few different ways
                // First, if it's marked as the latest stable version
                return LatestStableVersion
                       // Or if it's marked as the latest version (pre-release)
                       || LatestVersion
                       // Or if it's the current version and no version is marked as the latest (because they're all unlisted)
                       || (IsCurrent(this) && !PackageVersions.Any(p => p.LatestVersion));
            }
        }

        public bool IsInstallOrPortable
        {
            get
            {
                if (Id.EndsWith(".install", ignoreCase: true, culture: CultureInfo.InvariantCulture)
                    || Id.EndsWith(".portable", ignoreCase:true, culture: CultureInfo.InvariantCulture)
                    || Id.EndsWith(".app", ignoreCase: true, culture: CultureInfo.InvariantCulture)
                    || Id.EndsWith(".tool", ignoreCase: true, culture: CultureInfo.InvariantCulture)
                    || Id.EndsWith(".commandline", ignoreCase: true, culture: CultureInfo.InvariantCulture)
                ) return true;

                return Dependencies.DependencySets.AnySafe(dependencySet =>
                {
                    var id = Id.to_lower();
                    return dependencySet.Value.AnySafe(d =>
                        d != null && ( 
                            d.Id.Equals("{0}.install".format_with(id), StringComparison.InvariantCultureIgnoreCase)
                            || d.Id.Equals("{0}.portable".format_with(id), StringComparison.InvariantCultureIgnoreCase)
                            || d.Id.Equals("{0}.app".format_with(id), StringComparison.InvariantCultureIgnoreCase)
                            || d.Id.Equals("{0}.tool".format_with(id), StringComparison.InvariantCultureIgnoreCase)
                            || d.Id.Equals("{0}.commandline".format_with(id), StringComparison.InvariantCultureIgnoreCase)
                        )
                    );
                });
            }
        }

        public IEnumerable<PackageFile> Files { get; private set; }
        public IEnumerable<ScanResult> ScanResults { get; private set; }

        [AllowHtml]
        [Display(Name = "Add to Review Comments")]
        [StringLength(1000)]
        public string NewReviewComments { get; set; }

        [Display(Name = "Trust this package id?")]
        public bool IsTrusted { get; private set; }
    }
}
