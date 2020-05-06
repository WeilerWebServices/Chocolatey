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

using System.Linq;
using System.Security.Principal;
using System.Web.Mvc;
using MvcHaack.Ajax;

namespace NuGetGallery
{
    public partial class JsonApiController : JsonController
    {
        private readonly IPackageService packageSvc;
        private readonly IUserService userSvc;
        private readonly IEntityRepository<PackageOwnerRequest> packageOwnerRequestRepository;
        private readonly IMessageService messageSvc;

        public JsonApiController(IPackageService packageSvc, IUserService userSvc, IEntityRepository<PackageOwnerRequest> packageOwnerRequestRepository, IMessageService messageService)
        {
            this.packageSvc = packageSvc;
            this.userSvc = userSvc;
            this.packageOwnerRequestRepository = packageOwnerRequestRepository;
            messageSvc = messageService;
        }

        private bool UserHasPackageChangePermissions(IPrincipal user, PackageRegistration package)
        {
            if (user != null && (package.IsOwner(user) || user.IsModerator())) return true;

            return false;
        }

        [Authorize]
        public virtual object GetPackageOwners(string id, string version)
        {
            var package = packageSvc.FindPackageByIdAndVersion(id, version, allowPrerelease:true, useCache:false);
            if (package == null)
            {
                return new {
                    message = "Package not found"
                };
            }

            if (!UserHasPackageChangePermissions(HttpContext.User, package.PackageRegistration)) return new HttpStatusCodeResult(401, "Unauthorized");

            var owners = from u in package.PackageRegistration.Owners
                         select new OwnerModel {
                             name = u.Username,
                             current = u.Username == HttpContext.User.Identity.Name,
                             pending = false
                         };

            var pending = from u in packageOwnerRequestRepository.GetAll()
                          where u.PackageRegistrationKey == package.PackageRegistration.Key
                          select new OwnerModel {
                              name = u.NewOwner.Username,
                              current = false,
                              pending = true
                          };

            return owners.Union(pending);
        }

        public object AddPackageOwner(string id, string username, bool addDirectly = false)
        {
            var package = packageSvc.FindPackageRegistrationById(id, useCache:false);
            if (package == null)
            {
                return new {
                    success = false,
                    message = "Package not found"
                };
            }
            if (!UserHasPackageChangePermissions(HttpContext.User, package))
            {
                return new {
                    success = false,
                    message = "You are not the package maintainer."
                };
            }
            var user = userSvc.FindByUsername(username);
            if (user == null)
            {
                return new {
                    success = false,
                    message = "Maintainer not found"
                };
            }

            var currentUser = userSvc.FindByUsername(HttpContext.User.Identity.Name);

            var ownerRequest = packageSvc.CreatePackageOwnerRequest(package, currentUser, user);
            var success = true;
            if (!addDirectly)
            {
                var confirmationUrl = Url.ConfirmationUrl(MVC.Packages.ConfirmOwner().AddRouteValue("id", package.Id), user.Username, ownerRequest.ConfirmationCode, Request.Url.Scheme);
                messageSvc.SendPackageOwnerRequest(currentUser, user, package, confirmationUrl);
            } else
            {
                success = packageSvc.ConfirmPackageOwner(package, user, ownerRequest.ConfirmationCode);
                if (success) messageSvc.SendPackageOwnerConfirmation(currentUser, user, package);
            }

            return new {
                success,
                name = user.Username,
                pending = !addDirectly
            };
        }

        public object RemovePackageOwner(string id, string username)
        {
            var package = packageSvc.FindPackageRegistrationById(id, useCache:false);
            if (package == null)
            {
                return new {
                    success = false,
                    message = "Package not found"
                };
            }

            if (!UserHasPackageChangePermissions(HttpContext.User, package))
            {
                return new {
                    success = false,
                    message = "You are not the package maintainer."
                };
            }
            var user = userSvc.FindByUsername(username);
            if (user == null)
            {
                return new {
                    success = false,
                    message = "Maintainer not found"
                };
            }

            packageSvc.RemovePackageOwner(package, user);
            return new {
                success = true
            };
        }

        public class OwnerModel
        {
            public string name { get; set; }
            public bool current { get; set; }
            public bool pending { get; set; }
        }
    }
}
