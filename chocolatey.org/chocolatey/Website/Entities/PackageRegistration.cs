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

namespace NuGetGallery
{
    public class PackageRegistration : IEntity
    {
        public PackageRegistration()
        {
            Owners = new HashSet<User>();
            Packages = new HashSet<Package>();
        }

        public int Key { get; set; }

        [StringLength(128), Required]
        public string Id { get; set; }
        public int DownloadCount { get; set; }
        public virtual ICollection<User> Owners { get; set; }
        public virtual ICollection<Package> Packages { get; set; }

        public bool IsTrusted { get; set; }
        public DateTime? TrustedDate { get; set; }
        
        public virtual User TrustedBy { get; set; }
        public int? TrustedById { get; set; }
        
        public bool ExemptedFromVerification { get; set; }
        [MaxLength(500)]
        public string ExemptedFromVerificationReason { get; set; }
        public DateTime? ExemptedFromVerificationDate { get; set; }

        public virtual User ExemptedFromVerificationBy { get; set; }
        public int? ExemptedFromVerificationById { get; set; }
    }
}
