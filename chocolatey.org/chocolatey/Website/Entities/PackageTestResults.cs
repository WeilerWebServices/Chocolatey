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
using System.ComponentModel.DataAnnotations;

namespace NuGetGallery
{
    //note: don't store this in the database as a separate entity yet.
    //public class PackageTestResults : IEntity
    //{
    //    public int Key { get; set; }

    //    public Package Package { get; set; }
    //    public int PackageKey { get; set; }

    //    [MaxLength(60)]
    //    [Required]
    //    public string WindowsVersion { get; set; }

    //    [MaxLength(60)]
    //    [Required]
    //    public string MachineName { get; set; }

    //    [Required]
    //    public DateTime? TestDate { get; set; }

    //    [MaxLength(400)]
    //    [Required]
    //    public string ResultsUrl { get; set; }

    //    /// <remarks>
    //    ///   Has a max length of 4000. Is not indexed and not used for searches. Db column is nvarchar(max).
    //    /// </remarks>
    //    public string Summary { get; set; }
    //    public bool Success { get; set; }
    //}
}
