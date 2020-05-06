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
using System.Web.Mvc;
using NuGetGallery.Infrastructure;

namespace NuGetGallery
{
    public class ReportAbuseViewModel : ISpamValidationModel
    {
        public string PackageId { get; set; }
        public string PackageVersion { get; set; }

        [AllowHtml]
        [Required]
        [StringLength(4000)]
        [Display(Name = "Abuse Report")]
        public string Message { get; set; }

        [Display(Name = "Send me a copy")]
        public bool CopySender { get; set; }

        [Range(typeof(bool), "true", "true", ErrorMessage = "Please check the box to agree to terms of this form.")]
        public bool CheckBox { get; set; }

        [Required(ErrorMessage = "Please enter your email address.")]
        [StringLength(4000)]
        [DataType(DataType.EmailAddress)]
        [RegularExpression(@"[.\S]+\@[.\S]+\.[.\S]+", ErrorMessage = "This doesn't appear to be a valid email address.")]
        public string Email { get; set; }

        public bool ConfirmedUser { get; set; }

        [ScaffoldColumn(false)]
        public string SpamValidationResponse { get; set; }
    }
}
