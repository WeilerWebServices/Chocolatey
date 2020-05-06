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
using System.Web.Mvc;

namespace NuGetGallery.Infrastructure
{
    /// <summary>
    /// Validates spam validation responses on server side; apply to ASP.NET MVC action methods.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public class ValidateFormResponseAttribute : FilterAttribute, IAuthorizationFilter
    {
        /// <summary>
        /// Authorizes the current action by validating spam prevention responses.
        /// </summary>
        /// <param name="filterContext">The filter's authorization context.</param>
        public void OnAuthorization(AuthorizationContext filterContext)
        {
            if (filterContext == null)
                throw new ArgumentNullException("filterContext");
            if (filterContext.HttpContext == null)
                throw new ArgumentException("The HTTP context must not be null.", "filterContext");
            if (filterContext.Controller == null
                || filterContext.Controller.ViewData == null
                || filterContext.Controller.ViewData.ModelState == null)
                throw new ArgumentException("The controller's view data model state must not be null.", "filterContext");


            var response = Captcha.ValidateCaptcha(filterContext.HttpContext.Request["g-recaptcha-response"]);
            if (!response.Success)
            {
                filterContext.Controller.ViewData.ModelState.AddModelError(
                    "SpamValidationResponse",
                    string.Format("Unable to correctly validate spam validation response: {0}", response.ErrorMessage[0]));
            }
        }
    }
}
