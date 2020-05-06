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
using System.Linq;
using System.Net.Mail;
using System.Security.Principal;
using System.Web.Mvc;
using System.Web.UI;
using NuGet;
using NuGetGallery.Infrastructure;
using NuGetGallery.MvcOverrides;

namespace NuGetGallery
{
    public partial class UsersController : AppController
    {
        private readonly IUserService userService;
        private readonly IPackageService packageService;
        private readonly IMessageService messageService;
        private readonly IConfiguration settings;
        private readonly IPrincipal currentUser;
        private readonly IUserSiteProfilesService profilesService;
        private readonly ICourseAchievementsService courseAchievementsService;

        public UsersController(
            IUserService userSvc,
            IPackageService packageService,
            IMessageService messageService,
            IConfiguration settings,
            IPrincipal currentUser,
            IUserSiteProfilesService profilesService,
            ICourseAchievementsService courseAchievementsService
        )
        {
            userService = userSvc;
            this.packageService = packageService;
            this.messageService = messageService;
            this.settings = settings;
            this.currentUser = currentUser;
            this.profilesService = profilesService;
            this.courseAchievementsService = courseAchievementsService;
        }
        
        [Authorize, RequireHttpsAppHarbor, OutputCache(NoStore = true, Duration = 0, VaryByParam = "None")]
        public virtual ActionResult Account()
        {
            var user = GetService<IUserByUsernameQuery>().Execute(Identity.Name);
            var curatedFeeds = GetService<ICuratedFeedsByManagerQuery>().Execute(user.Key);

            return View(
                "~/Views/Users/Account.cshtml",
                new AccountViewModel
                {
                    UserName = user.Username,
                    ApiKey = user.ApiKey.ToString(),
                    CuratedFeeds = curatedFeeds.Select(cf => cf.Name),
                });
        }

        [Authorize, RequireHttpsAppHarbor, OutputCache(NoStore = true, Duration = 0, VaryByParam = "None")]
        public virtual ActionResult Edit()
        {
            var user = userService.FindByUsername(currentUser.Identity.Name);
            var profiles = profilesService.GetUserProfiles(user);
            var twitter = profiles.Where(x => x.Name == SiteProfileConstants.Twitter).Select(p => p.Url.Replace(SiteProfileConstants.TwitterProfilePrefix, string.Empty)).FirstOrDefault();
            var github = profiles.Where(x => x.Name == SiteProfileConstants.Github).Select(p => p.Url.Replace(SiteProfileConstants.GithubProfilePrefix, string.Empty)).FirstOrDefault();
            var codeplex = profiles.Where(x => x.Name == SiteProfileConstants.Codeplex).Select(p => p.Url.Replace(SiteProfileConstants.CodeplexProfilePrefix, string.Empty)).FirstOrDefault();
            var stackExchange = profiles.Where(x => x.Name == SiteProfileConstants.StackExchange).Select(p => p.Url).FirstOrDefault();
            var homepage = profiles.Where(x => x.Name == SiteProfileConstants.Homepage).Select(p => p.Url).FirstOrDefault();
            var blogUrl = profiles.Where(x => x.Name == SiteProfileConstants.Blog).Select(p => p.Url).FirstOrDefault();
            var packagesRepo = profiles.Where(x => x.Name == SiteProfileConstants.PackagesRepository).Select(p => p.Url).FirstOrDefault();
            var packagesRepoAuto = profiles.Where(x => x.Name == SiteProfileConstants.PackagesRepositoryAuto).Select(p => p.Url).FirstOrDefault();

            var model = new EditProfileViewModel
            {
                EmailAddress = user.EmailAddress,
                EmailAllowed = user.EmailAllowed,
                EmailAllModerationNotifications = user.EmailAllModerationNotifications,
                PendingNewEmailAddress = user.UnconfirmedEmailAddress,
                TwitterUserName = twitter,
                GithubUserName = github,
                CodeplexUserName = codeplex,
                StackExchangeUrl = stackExchange,
                HomepageUrl = homepage,
                BlogUrl = blogUrl,
                PackagesRepository = packagesRepo,
                PackagesRepositoryAuto = packagesRepoAuto
            };
            return View("~/Views/Users/Edit.cshtml", model);
        }

        [Authorize, HttpPost, RequireHttpsAppHarbor, ValidateAntiForgeryToken, OutputCache(NoStore = true, Duration = 0, VaryByParam = "None")]
        public virtual ActionResult Edit(EditProfileViewModel profile)
        {
            if (ModelState.IsValid)
            {
                var user = userService.FindByUsername(currentUser.Identity.Name);
                if (user == null) return HttpNotFound();

                string existingConfirmationToken = user.EmailConfirmationToken;
                try
                {
                    userService.UpdateProfile(user, profile.EmailAddress, profile.EmailAllowed, profile.EmailAllModerationNotifications);
                }
                catch (EntityException ex)
                {
                    ModelState.AddModelError(String.Empty, ex.Message);
                    return View("~/Views/Users/Edit.cshtml", profile);
                }

                if (existingConfirmationToken == user.EmailConfirmationToken) TempData["Message"] = "Account settings saved!";
                else
                {
                    TempData["Message"] =
                        "Account settings saved! We sent a confirmation email to verify your new email. When you confirm the email address, it will take effect and we will forget the old one.";

                    var confirmationUrl = Url.ConfirmationUrl(MVC.Users.Confirm(), user.Username, user.EmailConfirmationToken, protocol: Request.Url.Scheme);
                    messageService.SendEmailChangeConfirmationNotice(new MailAddress(profile.EmailAddress, user.Username), confirmationUrl);
                }

                profilesService.SaveProfiles(user, profile);

                return RedirectToAction(MVC.Users.Account());
            }
            return View("~/Views/Users/Edit.cshtml", profile);
        }

        [RequireHttpsAppHarbor]
        public virtual ActionResult Register()
        {
            return View();
        }

        [HttpPost, RequireHttpsAppHarbor, ValidateAntiForgeryToken, ValidateFormResponse]
        public virtual ActionResult Register(RegisterRequest request)
        {
            // TODO: consider client-side validation for unique username

            if (!ModelState.IsValid) return View("~/Views/Users/Register.cshtml");

            var errors = userService.CheckForStrongPassword(request.Password);
            if (errors.Any())
            {
                foreach (var error in errors)
                {
                    ModelState.AddModelError(string.Empty, error);
                }

                return View("~/Views/Users/Register.cshtml", request);
            }

            User user;
            try
            {
                user = userService.Create(request.Username, request.Password, request.EmailAddress);
            }
            catch (EntityException ex)
            {
                ModelState.AddModelError(String.Empty, ex.Message);
                return View("~/Views/Users/Register.cshtml");
            }

            if (settings.ConfirmEmailAddresses)
            {
                // Passing in scheme to force fully qualified URL
                var confirmationUrl = Url.ConfirmationUrl(MVC.Users.Confirm(), user.Username, user.EmailConfirmationToken, protocol: Request.Url.Scheme);
                messageService.SendNewAccountEmail(new MailAddress(request.EmailAddress, user.Username), confirmationUrl);
            }
            return RedirectToAction(MVC.Users.Thanks());
        }

        public virtual ActionResult Thanks()
        {
            if (settings.ConfirmEmailAddresses) return View("~/Views/Users/Thanks.cshtml");
            else
            {
                var model = new EmailConfirmationModel
                {
                    SuccessfulConfirmation = true,
                    ConfirmingNewAccount = true
                };
                return View("~/Views/Users/Confirm.cshtml", model);
            }
        }

        [Authorize, RequireHttpsAppHarbor, OutputCache(NoStore = true, Duration = 0, VaryByParam = "None")]
        public virtual ActionResult Packages()
        {
            var user = userService.FindByUsername(currentUser.Identity.Name);
            var packages = packageService.FindPackagesByOwner(user);

            var published = from p in packages
                orderby new SemanticVersion(p.Version) descending
                group p by p.PackageRegistration.Id;

            var model = new ManagePackagesViewModel
            {
                Packages = from pr in published
                    select new PackageViewModel(pr.First())
                    {
                        DownloadCount = pr.Sum(p => p.DownloadCount),
                        Listed = pr.Any(p => p.Listed)
                    },
            };
            return View("~/Views/Users/Packages.cshtml", model);
        }

        [Authorize, ValidateAntiForgeryToken, HttpPost, OutputCache(NoStore = true, Duration = 0, VaryByParam = "None")]
        public virtual ActionResult GenerateApiKey()
        {
            userService.GenerateApiKey(currentUser.Identity.Name);
            return RedirectToAction(MVC.Users.Account());
        }

        public virtual ActionResult ForgotPassword()
        {
            return View("~/Views/Users/ForgotPassword.cshtml");
        }

        [HttpPost, ValidateAntiForgeryToken, OutputCache(NoStore = true, Duration = 0, VaryByParam = "None")]
        public virtual ActionResult ForgotPassword(ForgotPasswordViewModel model)
        {
            if (ModelState.IsValid)
            {
                var user = userService.GeneratePasswordResetToken(model.Email, Constants.DefaultPasswordResetTokenExpirationHours * 60);
                if (user != null)
                {
                    var resetPasswordUrl = Url.ConfirmationUrl(MVC.Users.ResetPassword(), user.Username, user.PasswordResetToken, protocol: Request.Url.Scheme);
                    messageService.SendPasswordResetInstructions(user, resetPasswordUrl);

                    TempData["Email"] = user.EmailAddress;
                    return RedirectToAction(MVC.Users.PasswordSent());
                }

                ModelState.AddModelError("Email", "Could not find anyone with that email.");
            }

            return View("~/Views/Users/ForgotPassword.cshtml", model);
        }

        public virtual ActionResult ResendConfirmation()
        {
            return View("~/Views/Users/ResendConfirmation.cshtml");
        }

        [HttpPost, ValidateAntiForgeryToken, OutputCache(NoStore = true, Duration = 0, VaryByParam = "None")]
        public virtual ActionResult ResendConfirmation(ResendConfirmationEmailViewModel model)
        {
            if (ModelState.IsValid)
            {
                var user = userService.FindByUnconfimedEmailAddress(model.Email);
                if (user != null && !user.Confirmed)
                {
                    var confirmationUrl = Url.ConfirmationUrl(MVC.Users.Confirm(), user.Username, user.EmailConfirmationToken, protocol: Request.Url.Scheme);
                    messageService.SendNewAccountEmail(new MailAddress(user.UnconfirmedEmailAddress, user.Username), confirmationUrl);
                    return RedirectToAction(MVC.Users.Thanks());
                }
                ModelState.AddModelError("Email", "There was an issue resending your confirmation token.");
            }

            return View("~/Views/Users/ResendConfirmation.cshtml", model);
        }

        public virtual ActionResult PasswordSent()
        {
            ViewBag.Email = TempData["Email"];
            ViewBag.Expiration = Constants.DefaultPasswordResetTokenExpirationHours;

            return View("~/Views/Users/PasswordSent.cshtml");
        }

        public virtual ActionResult ResetPassword()
        {
            ViewBag.ResetTokenValid = true;
            return View("~/Views/Users/ResetPassword.cshtml");
        }

        [HttpPost, ValidateAntiForgeryToken, OutputCache(NoStore = true, Duration = 0, VaryByParam = "None")]
        public virtual ActionResult ResetPassword(string username, string token, PasswordResetViewModel model)
        {
            ViewBag.ResetTokenValid = userService.ResetPasswordWithToken(username, token, model.NewPassword);

            if (!ViewBag.ResetTokenValid)
            {
                ModelState.AddModelError("", "The Password Reset Token is not valid or expired.");
                return View("~/Views/Users/ResetPassword.cshtml", model);
            }
            return RedirectToAction(MVC.Users.PasswordChanged());
        }

        public virtual ActionResult Confirm(string username, string token)
        {
            if (String.IsNullOrEmpty(token)) return HttpNotFound();
            var user = userService.FindByUsername(username);
            if (user == null) return HttpNotFound();

            string existingEmail = user.EmailAddress;
            var model = new EmailConfirmationModel
            {
                ConfirmingNewAccount = String.IsNullOrEmpty(existingEmail),
                SuccessfulConfirmation = userService.ConfirmEmailAddress(user, token)
            };

            if (!model.ConfirmingNewAccount) messageService.SendEmailChangeNoticeToPreviousEmailAddress(user, existingEmail);
            return View("~/Views/Users/Confirm.cshtml", model);
        }

        [HttpGet, OutputCache(VaryByParam = "username", Location = OutputCacheLocation.Any, Duration = 1800)]
        public virtual ActionResult Profiles(string username)
        {
            var user = userService.FindByUsername(username);
            if (user == null) return HttpNotFound();

            var packages = (from p in packageService.FindPackagesByOwner(user) where p.Listed orderby p.Version descending group p by p.PackageRegistration.Id).Select(c => new PackageViewModel(c.First()))
                .ToList();

            var packagesInModeration =
                (from p in packageService.FindPackagesByOwner(user) where p.Status == PackageStatusType.Submitted orderby p.Version descending group p by p.PackageRegistration.Id).Select(
                    c => new PackageViewModel(c.First())).ToList();

            //var userProfiles = profilesService.GetUserProfiles(user).ToList();
            var userProfiles = (from p in profilesService.GetUserProfiles(user) orderby p.Name select p).Select(c => new UserSiteProfileViewModel(c)).ToList();
            var completedCourses = (from p in courseAchievementsService.GetUserCourseAchievements(user) orderby p.CompletedDate select p).Select(c => new CourseAchievementViewModel(c)).ToList();
            var model = new UserProfileModel
            {
                Username = user.Username,
                EmailAddress = user.EmailAddress,
                Packages = packages,
                PackagesModerationQueue = packagesInModeration,
                TotalPackageDownloadCount = packages.Sum(p => p.TotalDownloadCount),
                UserProfiles = userProfiles,
                CompletedCourses = completedCourses,
            };

            return View("~/Views/Users/Profiles.cshtml", model);
        }

        [Authorize, RequireHttpsAppHarbor, OutputCache(NoStore = true, Duration = 0, VaryByParam = "None")]
        public virtual ActionResult ChangePassword()
        {
            return View("~/Views/Users/ChangePassword.cshtml");
        }

        [HttpPost, RequireHttpsAppHarbor, ValidateAntiForgeryToken, Authorize, OutputCache(NoStore = true, Duration = 0, VaryByParam = "None")]
        public virtual ActionResult ChangePassword(PasswordChangeViewModel model)
        {
            if (ModelState.IsValid) if (!userService.ChangePassword(currentUser.Identity.Name, model.OldPassword, model.NewPassword)) ModelState.AddModelError("OldPassword", Strings.CurrentPasswordIncorrect);

            if (!ModelState.IsValid) return View("~/Views/Users/ChangePassword.cshtml", model);

            var errors = userService.CheckForStrongPassword(model.NewPassword);
            if (errors.Any())
            {
                foreach (var error in errors)
                {
                    ModelState.AddModelError(string.Empty, error);
                }

                return View("~/Views/Users/ChangePassword.cshtml", model);
            }

            return RedirectToAction(MVC.Users.PasswordChanged());
        }

        public virtual ActionResult PasswordChanged()
        {
            return View("~/Views/Users/PasswordChanged.cshtml");
        }
    }
}
