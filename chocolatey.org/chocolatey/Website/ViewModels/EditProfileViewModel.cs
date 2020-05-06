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

using System.ComponentModel.DataAnnotations;

namespace NuGetGallery
{
    public class EditProfileViewModel
    {
        [Required]
        [StringLength(150)]
        [Display(Name = "Email")]
        [DataType(DataType.EmailAddress)]
        [RegularExpression(@"[.\S]+\@[.\S]+\.[.\S]+", ErrorMessage = "This doesn't appear to be a valid email address.")]
        public string EmailAddress { get; set; }

        public string PendingNewEmailAddress { get; set; }

        [Display(Name = "Receive Email Notifications")]
        public bool EmailAllowed { get; set; }     
        
        [Display(Name = "Receive Email For All Moderation-Related Notifications")]
        public bool EmailAllModerationNotifications { get; set; }

        [Display(Name = "Twitter Username")]
        [StringLength(255)]
        public string TwitterUserName { get; set; }

        [Display(Name = "Github Username")]
        [StringLength(255)]
        public string GithubUserName { get; set; }

        [Display(Name = "Codeplex Username")]
        [StringLength(255)]
        public string CodeplexUserName { get; set; }

        [Display(Name = "StackExchange Profile Url")]
        [StringLength(255)]
        public string StackExchangeUrl { get; set; }

        [Display(Name = "Homepage Url")]
        [StringLength(255)]
        public string HomepageUrl { get; set; }

        [Display(Name = "Blog Url")]
        [StringLength(255)]
        public string BlogUrl { get; set; }

        [Display(Name = "Chocolatey Packages Repository")]
        [StringLength(255)]
        public string PackagesRepository { get; set; }

        [Display(Name = "Chocolatey Automatic Packages Repository")]
        [StringLength(255)]
        public string PackagesRepositoryAuto { get; set; }
    }
}
