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
using System.Data.Entity.Migrations;
using System.Linq;

namespace NuGetGallery.Migrations
{
    public class MigrationsConfiguration : DbMigrationsConfiguration<EntitiesContext>
    {
        public MigrationsConfiguration()
        {
            AutomaticMigrationsEnabled = false;
        }

        protected override void Seed(EntitiesContext context)
        {
            SeedRoles(context);
            SeedGallerySettings(context);
            SeedCourses(context);
            SeedCourseModules(context);
        }

        private void SeedRoles(EntitiesContext context)
        {
            var roles = context.Set<Role>();
            if (!roles.Any(x => x.Name == Constants.AdminRoleName))
            {
                roles.Add(
                    new Role
                    {
                        Name = Constants.AdminRoleName
                    });
                context.SaveChanges();
            }
            if (!roles.Any(x => x.Name == Constants.ModeratorsRoleName))
            {
                roles.Add(
                    new Role
                    {
                        Name = Constants.ModeratorsRoleName
                    });
                context.SaveChanges();
            }
            if (!roles.Any(x => x.Name == Constants.ReviewersRoleName))
            {
                roles.Add(
                    new Role
                    {
                        Name = Constants.ReviewersRoleName
                    });
                context.SaveChanges();
            }
        }

        private void SeedGallerySettings(EntitiesContext context)
        {
            var gallerySettings = context.Set<GallerySetting>();
            if (!gallerySettings.Any())
            {
                gallerySettings.Add(
                    new GallerySetting
                    {
                        SmtpHost = "",
                        SmtpPort = 25,
                        GalleryOwnerEmail = "nobody@nowhere.com",
                        GalleryOwnerName = "Chocolatey Gallery - Local",
                        ConfirmEmailAddresses = false
                    });
                context.SaveChanges();
            }
        }

        private void SeedCourses(EntitiesContext context)
        {
            SeedCourse(context, CourseNameType.GettingStartedWithChocolatey, "Getting Started with Chocolatey");
            SeedCourse(context, CourseNameType.InstallingUpgradingUninstalling, "Installing, Upgrading, and Uninstalling Chocolatey");
            SeedCourse(context, CourseNameType.CreatingChocolateyPackages, "Creating Chocolatey Packages");
        }

        private void SeedCourse(EntitiesContext context, CourseNameType courseNameType, string name)
        {
            var courseNameTypeString = courseNameType.to_string();
            var courses = context.Set<Course>();
            if (!courses.Any(x => x.CourseNameTypeForDatabase == courseNameTypeString))
            {
                courses.Add(
                    new Course
                    {
                        Key = CourseConstants.GetCourseKey(courseNameType),
                        Name = name,
                       //Url = CourseConstants.GetCourseUrl(courseNameType),
                        CourseNameType = courseNameType,

                    });
                context.SaveChanges();
            }
        }

        private void SeedCourseModules(EntitiesContext context)
        {
            SeedCourseModule(context, CourseModuleNameType.GettingStarted_WhatIsChocolatey, CourseNameType.GettingStartedWithChocolatey, 1, "What Is Chocolatey?", "20 Minutes | Learn the basics of Chocolatey and how it can help manage your next project.");
            SeedCourseModule(context, CourseModuleNameType.GettingStarted_Requirements, CourseNameType.GettingStartedWithChocolatey, 2, "Requirements", "10 Minutes | Learn the requirements needed to get Chocolatey up and running on your machine.");
            SeedCourseModule(context, CourseModuleNameType.GettingStarted_HowToUse, CourseNameType.GettingStartedWithChocolatey, 3, "Using Chocolatey", "5 Minutes | Find examples and learn how to use Chocolatey.");
            SeedCourseModule(context, CourseModuleNameType.GettingStarted_Terminology, CourseNameType.GettingStartedWithChocolatey, 4, "Terminology", "3 Minutes | Understand the difference between Software and a Package and how they are related.");
            SeedCourseModule(context, CourseModuleNameType.GettingStarted_ChocolateyPackages, CourseNameType.GettingStartedWithChocolatey, 5, "Chocolatey Packages", "7 Minutes | Find out what exactly a Chocolatey Package is and why it's superior.");
            SeedCourseModule(context, CourseModuleNameType.GettingStarted_HowChocolateyWorks, CourseNameType.GettingStartedWithChocolatey, 6, "How Chocolatey Works", "10 Minutes | Learn how installing, upgrading, and uninstalling works with Chocolatey." );
            SeedCourseModule(context, CourseModuleNameType.GettingStarted_InstallationInDetail, CourseNameType.GettingStartedWithChocolatey, 7, "Installation in Detail", "10 Minutes | Includes details on where Chocolatey Packages are installed to, from, and how to manage existing installed software.");
            SeedCourseModule(context, CourseModuleNameType.Installation_Installing, CourseNameType.InstallingUpgradingUninstalling, 1, "Installing Chocolatey","10 Minutes | Learn how to install Chocolatey based on your preferred method.");
            SeedCourseModule(context, CourseModuleNameType.Installation_Upgrading, CourseNameType.InstallingUpgradingUninstalling, 2, "Upgrading Chocolatey", "< 1 Minute | Find out how to upgrade Chocolatey with one simple command.");
            SeedCourseModule(context, CourseModuleNameType.Installation_Uninstalling, CourseNameType.InstallingUpgradingUninstalling, 3, "Uninstalling Chocolatey", "5 Minutes | How to uninstall Chocolatey the correct way.");
            SeedCourseModule(context, CourseModuleNameType.CreatePackages_Summary, CourseNameType.CreatingChocolateyPackages, 1, "Course Summary, Rules, and Guidlines", "10 Minutes | Critical information you should know before creating your first package.");
            SeedCourseModule(context, CourseModuleNameType.CreatePackages_Nuspec, CourseNameType.CreatingChocolateyPackages, 2, "General Information and Nuspec", "5 Minutes | Nuget (and Chocolatey) General Information and Nuspec.");
            SeedCourseModule(context, CourseModuleNameType.CreatePackages_NewCommand, CourseNameType.CreatingChocolateyPackages, 3, "The Choco New Command", "5 Minutes | Learn the exit codes, options, and switches.");
            SeedCourseModule(context, CourseModuleNameType.CreatePackages_NamingVersioning, CourseNameType.CreatingChocolateyPackages, 4, "Naming, Description, and Versioning Recommendations", "15 Minutes | Learn best practices on the specification."); 
            SeedCourseModule(context, CourseModuleNameType.CreatePackages_InstallUpgradeUninstall, CourseNameType.CreatingChocolateyPackages, 5, "Installing, Upgrading, and Uninstalling Your Package", "10 Minutes | Learn about installation paths, upgrading, and uninstalling your package.");
            SeedCourseModule(context, CourseModuleNameType.CreatePackages_Shims, CourseNameType.CreatingChocolateyPackages, 6, "All About Shims", "5 Minutes | Learn what a shim is and what it enables.");
            SeedCourseModule(context, CourseModuleNameType.CreatePackages_Localization, CourseNameType.CreatingChocolateyPackages, 7, "Internationalization and Localization of Packages", "5 Minutes | Find examples of the various techniques used to localize a package."); 
            SeedCourseModule(context, CourseModuleNameType.CreatePackages_BuildTestingPush, CourseNameType.CreatingChocolateyPackages, 8, "Building, Testing, and Pushing Your Package", "10 Minutes | Learn best techniques for preparing and testing your package."); 
            SeedCourseModule(context, CourseModuleNameType.CreatePackages_AutomaticPackaging, CourseNameType.CreatingChocolateyPackages, 9, "Automatic Packaging", "5 Minutes | Requirements and setup instructions on how to automatically update a package.");
            SeedCourseModule(context, CourseModuleNameType.CreatePackages_PackageHandover, CourseNameType.CreatingChocolateyPackages, 10, "Package Maintainer Handover", "5 Minutes | Read the steps on how to properly take of an existing package");
        }

        private void SeedCourseModule(EntitiesContext context, CourseModuleNameType courseModuleNameType, CourseNameType courseNameType, int order, string name, string description)
        {
            var courseModuleNameTypeString = courseModuleNameType.to_string();
            var courseModules = context.Set<CourseModule>();
            if (!courseModules.Any(x => x.CourseModuleNameTypeForDatabase == courseModuleNameTypeString))
            {
                courseModules.Add(
                    new CourseModule
                    {
                        Key = CourseConstants.GetCourseModuleKey(courseModuleNameType),
                        Name = name,
                        Description = description,
                        //Url = CourseConstants.GetCourseModuleUrl(courseModuleNameType),
                        CourseModuleNameType = courseModuleNameType,
                        CourseKey = CourseConstants.GetCourseKey(courseNameType),
                        Order = order,
                    });
                context.SaveChanges();
            }
        }
    }
}
