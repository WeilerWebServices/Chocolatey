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

using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Web.Mvc;
using NuGetGallery.Infrastructure;

namespace NuGetGallery
{
    public class ContactBlockedViewModel : ISpamValidationModel
    {
        [AllowHtml]
        [Display(Name = "Enter your message. If you have additional blocked IP addresses, please list them here.")]
        [Required(ErrorMessage = "Please enter a message.")]
        [StringLength(4000)]
        public string Message { get; set; }

        [Required(ErrorMessage = "Please enter your email address.")]
        [StringLength(150)]
        [DataType(DataType.EmailAddress)]
        [RegularExpression(@"[.\S]+\@[.\S]+\.[.\S]+", ErrorMessage = "This doesn't appear to be a valid email address.")]
        public string Email { get; set; }

        [Display(Name = "First name")]
        [Required(ErrorMessage = "Please enter your first name.")]
        [Hint("Provide your email address so we can follow up with you.")]
        public string FirstName { get; set; }

        [Display(Name = "Last name")]
        [Required(ErrorMessage = "Please enter your last name.")]
        public string LastName { get; set; }

        [Display(Name = "Company")]
        public string CompanyName { get; set; }

        [Display(Name = "What is your blocked IP address?")]
        [Required(ErrorMessage = "Please enter your blocked IP address.")]
        public string BlockedIP { get; set; }

        [Display(Name = "Phone number")]
        public string PhoneNumber { get; set; }

        [Range(typeof(bool), "true", "true", ErrorMessage = "Please check the box to agree to terms of this form.")]
        public bool CheckBox { get; set; }

        [ScaffoldColumn(false)]
        public string SpamValidationResponse { get; set; }
    }
}
