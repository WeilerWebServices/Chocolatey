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

namespace NuGetGallery
{
    [ModelBinder(typeof(BaseModelBinder<PackageScanResult>))]
    public class PackageScanResult
    {
        public string Sha256Checksum { get; set; }
        public string FileName { get; set; }
        public string ScanDetailsUrl { get; set; }
        public string Positives { get; set; }
        public string TotalScans { get; set; }
        public string ScanData { get; set; }
        public string ScanDate { get; set; }
    }
}
