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
using System.Xml.Serialization;

namespace NuGetGallery
{
    [Serializable]
    public class ScanResult : IEntity
    {
        public ScanResult()
        {
            Packages = new HashSet<Package>();
        }

        public int Key { get; set; }

        public virtual ICollection<Package> Packages { get; set; }

        [MaxLength(400)]
        public string Sha256Checksum { get; set; }

        [MaxLength(400)]
        public string FileName { get; set; }

        [MaxLength(400)]
        public string ScanDetailsUrl { get; set; }

        public int Positives { get; set; }

        public int TotalScans { get; set; }

        [StringLength(2500)]
        public string ScanData { get; set; }

        public DateTime? ScanDate { get; set; }
    }
}
