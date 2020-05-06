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

using System.Web.Mvc;
using System.Web.UI;

namespace NuGetGallery.Controllers
{
    public class StyleguideController : AppController
    {

        private readonly IFileSystemService _fileSystem;

        public StyleguideController(IFileSystemService fileSystem)
        {
            _fileSystem = fileSystem;
        }

        [HttpGet, OutputCache(VaryByParam = "*", Location = OutputCacheLocation.Any, Duration = 7200)]
        public ActionResult Styleguide(string styleguideName)
        {
            styleguideName = styleguideName.Replace("-", "");
            var fileExists = _fileSystem.FileExists(Server.MapPath("~/Views/Styleguide/{0}.cshtml".format_with(styleguideName)));

            if (fileExists)
            {
                return View(styleguideName);
            }

            return RedirectToRoute(RouteName.Styleguide, new { styleguideName = "home" });
        }

    }
}
