// Copyright 2017 - 2019 Chocolatey Software
// Copyright 2011 - 2017RealDimensions Software, LLC, the original 
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
using System.Linq;
using System.Security.Principal;
using System.Web.Mvc;
using System.Web.UI;
using NuGetGallery.MvcOverrides;

namespace NuGetGallery
{
    public partial class CoursesController : AppController
    {
        private readonly IUserService userService;
        private readonly IPrincipal currentUser;
        private readonly ICourseAchievementsService courseAchievementsService;
        private readonly IEntityRepository<Course> courseRepo;
        private readonly IEntityRepository<CourseModule> courseModuleRepo;

        public CoursesController(
            IUserService userSvc,
            IPrincipal currentUser,
            ICourseAchievementsService courseAchievementsService,
            IEntityRepository<Course> courseRepo,
            IEntityRepository<CourseModule> courseModuleRepo
        )
        {
            userService = userSvc;
            this.currentUser = currentUser;
            this.courseAchievementsService = courseAchievementsService;
            this.courseRepo = courseRepo;
            this.courseModuleRepo = courseModuleRepo;
        }

        [HttpGet, OutputCache(VaryByParam = "username", Location = OutputCacheLocation.Any, Duration = 1800)]
        public virtual ActionResult Courses()
        {
            var courseModules = courseModuleRepo.GetAll()
                .Include(cm => cm.Course)
                .ToList();

            var courseModulesView = new List<CourseModuleViewModel>();

            // TODO need some automapper action here
            foreach (var module in courseModules.OrEmptyListIfNull())
            {
                courseModulesView.Add(
                    new CourseModuleViewModel()
                    {
                        CourseKey = module.CourseKey,
                        CourseModuleKey = module.Key,
                        CourseModuleDescription = module.Description,
                        CourseModuleName = module.Name,
                        ModuleOrder = module.Order,
                        ModuleNameType = module.CourseModuleNameType,
                    });
            }

            var model = new CourseViewModel
            {
                CourseModules = courseModulesView,
            };

            if (Request.IsAuthenticated)
            {
                var user = userService.FindByUsername(currentUser.Identity.Name);
                var userCourseAchievements = (from p in courseAchievementsService.GetUserCourseAchievements(user) orderby p.CompletedDate select p)
                    .Select(c => new CourseAchievementViewModel(c))
                    .ToList();

                model.Username = user.Username;
                model.UserKey = user.Key;
                model.UserCourseAchievements = userCourseAchievements;

                TempData.Clear();
            }

            return View("~/Views/Courses/Home.cshtml", model);
        }

        [HttpGet, OutputCache(VaryByParam = "username", Location = OutputCacheLocation.Any, Duration = 1800)]
        public virtual ActionResult CourseName(string courseName, string courseModuleName)
        {
            var courseNameType = CourseConstants.GetCourseTypeFromUrlFragment(courseName);
            var courseModuleNameType = CourseConstants.GetCourseModuleTypeFromUrlFragment(courseModuleName);
            var courseKey = CourseConstants.GetCourseKey(courseNameType);
            courseName = courseName.Replace("-", "");
            courseModuleName = courseModuleName.Replace("-", "");

            var courseModules = courseModuleRepo.GetAll()
                .Include(cm => cm.Course)
                .ToList();

            var courseModulesView = new List<CourseModuleViewModel>();

            // TODO need some automapper action here
            foreach (var module in courseModules.OrEmptyListIfNull())
            {
                courseModulesView.Add(
                    new CourseModuleViewModel()
                    {
                        CourseKey = module.CourseKey,
                        CourseModuleKey = module.Key,
                        CourseModuleDescription = module.Description,
                        CourseModuleName = module.Name,
                        ModuleOrder = module.Order,
                        ModuleNameType = module.CourseModuleNameType,
                    });
            }

            var model = new CourseViewModel
            {
                CourseModules = courseModulesView,
                // additional components
                CourseName = CourseConstants.GetCourseName(courseNameType),
                CourseKey = courseKey,
                CourseNameType = courseNameType,
                CourseModuleKey = CourseConstants.GetCourseModuleKey(courseModuleNameType),
            };

            if (Request.IsAuthenticated)
            {
                var user = userService.FindByUsername(currentUser.Identity.Name);
                var userCourseAchievements = (from p in courseAchievementsService.GetUserCourseAchievements(user) orderby p.CompletedDate select p)
                    .Select(c => new CourseAchievementViewModel(c))
                    .ToList();

                var completedCourse = false;
                DateTime? completionDate = null;
                var courseNameTypeString = courseNameType.to_string();
                var course = courseRepo.GetAll().FirstOrDefault(x => x.CourseNameTypeForDatabase == courseNameTypeString);
                if (course != null)
                {
                    var courseAchievement = userCourseAchievements.FirstOrDefault(x => x.CourseKey == course.Key);
                    if (courseAchievement != null)
                    {
                        completedCourse = courseAchievement.Completed;
                        completionDate = courseAchievement.CompletedDate;
                    }
                }

                model.Username = user.Username;
                model.UserKey = user.Key;
                model.UserCourseAchievements = userCourseAchievements;
                model.CompletedCourse = completedCourse;
                model.CompletedDate = completionDate;

                //delete TempData if not directly after a completed quiz
                if (!Request.QueryString.ToString().Contains("quiz=true"))
                {
                    TempData.Clear();
                }
            }

            return View("~/Views/Courses/{0}/{1}.cshtml".format_with(courseName, courseModuleName), model);
        }

        [Authorize, HttpPost, RequireHttpsAppHarbor, ValidateAntiForgeryToken, OutputCache(NoStore = true, Duration = 0, VaryByParam = "None")]
        public virtual ActionResult CourseName(CourseViewModel viewModel, string courseName, string courseModuleName)
        {
            courseName = courseName.Replace("-", "");
            courseModuleName = courseModuleName.Replace("-", "");

            if (ModelState.IsValid)
            {
                var user = userService.FindByUsername(currentUser.Identity.Name);
                TempData["Message"] = "You passed this course!";

                var courseAchievement = new UserCourseAchievement
                {
                    CourseKey = viewModel.CourseKey,
                    UserKey = user.Key
                };

                //courseAchievement.CourseModuleAchievements
                courseAchievementsService.SaveCourseAchievements(user, courseAchievement, viewModel.CourseModuleKey);

                return Redirect(ControllerContext.HttpContext.Request.UrlReferrer.ToString() + "?quiz=true");
            }

            return View("~/Views/Courses/{0}/{1}.cshtml".format_with(courseName, courseModuleName), viewModel);
        }
    }
}
