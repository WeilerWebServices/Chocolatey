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

namespace NuGetGallery
{
    public class MarkdownPostViewModel
    {
        public DateTime? Published { get; set; }
        public string UrlPath { get; set; }
        public string Title { get; set; }
        public string Author { get; set; }
        public string Post { get; set; }
        public string Tags { get; set; }
        public string Keywords { get; set; }
        public string Summary { get; set; }
        public string Image { get; set; }
    }
}
