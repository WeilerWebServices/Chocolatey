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
using System.ComponentModel.DataAnnotations;

namespace NuGetGallery
{
    public class PackageOwnerRequest : IEntity
    {
        public int Key { get; set; }
        public int PackageRegistrationKey { get; set; }
        public int NewOwnerKey { get; set; }
        public User NewOwner { get; set; }
        public User RequestingOwner { get; set; }
        public int RequestingOwnerKey { get; set; }
        [StringLength(256)]
        public string ConfirmationCode { get; set; }
        public DateTime RequestDate { get; set; }
    }
}
