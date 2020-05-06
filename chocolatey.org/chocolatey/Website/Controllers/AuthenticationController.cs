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
using System.Web.Mvc;
using NuGetGallery.MvcOverrides;

namespace NuGetGallery
{
    public partial class AuthenticationController : Controller
    {
        private readonly IFormsAuthenticationService formsAuthSvc;
        private readonly IUserService userSvc;

        public AuthenticationController(IFormsAuthenticationService formsAuthSvc, IUserService userSvc)
        {
            this.formsAuthSvc = formsAuthSvc;
            this.userSvc = userSvc;
        }

        [RequireHttpsAppHarbor]
        public virtual ActionResult LogOn()
        {
            if (TempData.ContainsKey(Constants.ReturnUrlViewDataKey)) ViewData[Constants.ReturnUrlViewDataKey] = TempData[Constants.ReturnUrlViewDataKey];

            if (Request.IsAuthenticated) return SafeRedirect(ViewData[Constants.ReturnUrlViewDataKey] as string);

            return View();
        }

        [HttpPost, RequireHttpsAppHarbor, ValidateAntiForgeryToken]
        public virtual ActionResult LogOn(SignInRequest request, string returnUrl)
        {
            // TODO: modify the Object.cshtml partial to make the first text box autofocus, or use additional metadata

            ViewData[Constants.ReturnUrlViewDataKey] = returnUrl;

            if (!ModelState.IsValid) return View();

            var user = userSvc.FindByUsernameOrEmailAddressAndPassword(request.UserNameOrEmail, request.Password);

            if (user == null)
            {
                ModelState.AddModelError(String.Empty, Strings.UserNotFound);

                return View();
            }

            if (!user.Confirmed)
            {
                ViewBag.ConfirmationRequired = true;
                return View();
            }

            IEnumerable<string> roles = null;
            if (user.Roles.AnySafe()) roles = user.Roles.Select(r => r.Name);

            formsAuthSvc.SetAuthCookie(user.Username, true, roles);

            return SafeRedirect(returnUrl);
        }

        [HttpPost, RequireHttpsAppHarbor, ValidateAntiForgeryToken]
        public virtual ActionResult LogOff(string returnUrl)
        {
            formsAuthSvc.SignOut();

            return SafeRedirect(returnUrl);
        }

        [NonAction]
        public virtual ActionResult SafeRedirect(string returnUrl)
        {
            if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl) && returnUrl.Length > 1 && returnUrl.StartsWith("/") && !returnUrl.StartsWith("//") && !returnUrl.StartsWith("/\\")) return Redirect(returnUrl);

            return Redirect(Url.Home());
        }
    }
}
