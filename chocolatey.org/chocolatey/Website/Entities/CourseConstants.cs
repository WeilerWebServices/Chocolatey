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

namespace NuGetGallery
{
    /// <summary>
    /// Constants to be used with the Courses and Achievements sections of the site
    /// </summary>
    /// <remarks>Some of this will ultimately move to be stored in the DB</remarks>
    public static class CourseConstants
    {
        public static string GetCourseName(int courseKey)
        {
            return GetCourseName((CourseNameType)courseKey);
        }

        public static string GetCourseName(CourseNameType courseNameType)
        {
            switch (courseNameType)
            {
                case CourseNameType.GettingStartedWithChocolatey:
                    return "Getting Started with Chocolatey";
                case CourseNameType.InstallingUpgradingUninstalling:
                    return "Installing, Upgrading, and Uninstalling Chocolatey";
                case CourseNameType.CreatingChocolateyPackages:
                    return "Creating Chocolatey Packages";
                default:
                    return string.Empty;
            }
        }

        public static int GetCourseKey(CourseNameType courseNameType)
        {
            return (int)courseNameType;
        }

        public static string GetCourseUrl(int courseKey)
        {
            return GetCourseUrl((CourseNameType)courseKey);
        }

        public static string GetCourseUrl(CourseNameType courseNameType)
        {
            switch (courseNameType)
            {
                case CourseNameType.GettingStartedWithChocolatey:
                    return "getting-started";
                case CourseNameType.InstallingUpgradingUninstalling:
                    return "installation";
                case CourseNameType.CreatingChocolateyPackages:
                    return "creating-chocolatey-packages";
                default:
                    return string.Empty;
            }
        }

        /// <summary>
        /// Gets the course type from URL fragment. This is the value after the "/"
        /// </summary>
        /// <param name="urlFragment">The URL fragment.</param>
        /// <returns></returns>
        public static CourseNameType GetCourseTypeFromUrlFragment(string urlFragment)
        {
            if (string.IsNullOrWhiteSpace(urlFragment)) return CourseNameType.Unknown;

            switch (urlFragment.to_lower())
            {
                case "getting-started":
                    return CourseNameType.GettingStartedWithChocolatey;
                case "installation":
                    return CourseNameType.InstallingUpgradingUninstalling;
                case "creating-chocolatey-packages":
                    return CourseNameType.CreatingChocolateyPackages;
                default:
                    return CourseNameType.Unknown;
            }
        }

        private const string BadgeUrlPath = "~/Content/Images/Badges";

        private static string BadgeUrl(string fileName)
        {
            return T4MVCHelpers.ProcessVirtualPath(BadgeUrlPath + "/" + fileName);
        }

        public static string GetBadgeUrl(int courseKey)
        {
            return GetBadgeUrl((CourseNameType)courseKey);
        }

        public static string GetBadgeUrl(CourseNameType courseNameType)
        {
            return BadgeUrl(courseNameType.ToString() + ".png");
        }

        public static int GetCourseModuleKey(CourseModuleNameType courseModuleNameType)
        {
            return (int)courseModuleNameType;
        }

        public static string GetCourseModuleUrl(int courseModuleKey)
        {
            return GetCourseModuleUrl((CourseModuleNameType)courseModuleKey);
        }

        public static string GetCourseModuleUrl(CourseModuleNameType courseModuleNameType)
        {
            switch (courseModuleNameType)
            {
                case CourseModuleNameType.GettingStarted_WhatIsChocolatey:
                    return "what-is-chocolatey";
                case CourseModuleNameType.GettingStarted_Requirements:
                    return "requirements";
                case CourseModuleNameType.GettingStarted_HowToUse:
                    return "how-to-use";
                case CourseModuleNameType.GettingStarted_Terminology:
                    return "terminology";
                case CourseModuleNameType.GettingStarted_ChocolateyPackages:
                    return "chocolatey-packages";
                case CourseModuleNameType.GettingStarted_HowChocolateyWorks:
                    return "how-chocolatey-works";
                case CourseModuleNameType.GettingStarted_InstallationInDetail:
                    return "installation-in-detail";
                case CourseModuleNameType.Installation_Installing:
                    return "installing";
                case CourseModuleNameType.Installation_Upgrading:
                    return "upgrading";
                case CourseModuleNameType.Installation_Uninstalling:
                    return "uninstalling";
                case CourseModuleNameType.CreatePackages_Summary:
                    return "summary-rules-and-guidlines";
                case CourseModuleNameType.CreatePackages_Nuspec:
                    return "nuget-packages-and-nuspec";
                case CourseModuleNameType.CreatePackages_NewCommand:
                    return "choco-new-command";
                case CourseModuleNameType.CreatePackages_NamingVersioning:
                    return "naming-description-and-versioning";
                case CourseModuleNameType.CreatePackages_InstallUpgradeUninstall:
                    return "installing-upgrading-and-uninstalling";
                case CourseModuleNameType.CreatePackages_Shims:
                    return "shims";
                case CourseModuleNameType.CreatePackages_Localization:
                    return "internationalization-and-localization";
                case CourseModuleNameType.CreatePackages_BuildTestingPush:
                    return "building-testing-and-pushing";
                case CourseModuleNameType.CreatePackages_AutomaticPackaging:
                    return "automatic-packaging";
                case CourseModuleNameType.CreatePackages_PackageHandover:
                    return "package-maintainer-handover";
                default:
                    return string.Empty;
            }
        }

        public static CourseModuleNameType GetCourseModuleTypeFromUrlFragment(string courseModuleName)
        {

            switch (courseModuleName)
            {
                case "what-is-chocolatey":
                    return CourseModuleNameType.GettingStarted_WhatIsChocolatey;

                case "requirements":
                    return CourseModuleNameType.GettingStarted_Requirements;
                case "how-to-use":
                    return CourseModuleNameType.GettingStarted_HowToUse;
                case "terminology":
                    return CourseModuleNameType.GettingStarted_Terminology;
                case "chocolatey-packages":
                    return CourseModuleNameType.GettingStarted_ChocolateyPackages;
                case "how-chocolatey-works":
                    return CourseModuleNameType.GettingStarted_HowChocolateyWorks;
                case "installation-in-detail":
                    return CourseModuleNameType.GettingStarted_InstallationInDetail;
                case "installing":
                    return CourseModuleNameType.Installation_Installing;
                case "upgrading":
                    return CourseModuleNameType.Installation_Upgrading;
                case "uninstalling":
                    return CourseModuleNameType.Installation_Uninstalling;
                case "summary-rules-and-guidlines":
                    return CourseModuleNameType.CreatePackages_Summary;
                case "nuget-packages-and-nuspec":
                    return CourseModuleNameType.CreatePackages_Nuspec;
                case "choco-new-command":
                    return CourseModuleNameType.CreatePackages_NewCommand;
                case "naming-description-and-versioning":
                    return CourseModuleNameType.CreatePackages_NamingVersioning;
                case "installing-upgrading-and-uninstalling":
                    return CourseModuleNameType.CreatePackages_InstallUpgradeUninstall;
                case "shims":
                    return CourseModuleNameType.CreatePackages_Shims;
                case "internationalization-and-localization":
                    return CourseModuleNameType.CreatePackages_Localization;
                case "building-testing-and-pushing":
                    return CourseModuleNameType.CreatePackages_BuildTestingPush;
                case "automatic-packaging":
                    return CourseModuleNameType.CreatePackages_AutomaticPackaging;
                case "package-maintainer-handover":
                    return CourseModuleNameType.CreatePackages_PackageHandover;
                default:
                    return CourseModuleNameType.Unknown;
            }
        }
 
    }
}
