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
using System.Xml.Serialization;

namespace NuGetGallery
{
    // IMPORTANT:   Removed the TimeStamp column from this class because 
    //              it's completely tracked by the database layer. Don't
    //              add it back! :) It will be created by the migration.
    [Serializable]
    public class PackageStatistics : IEntity
    {
        public int Key { get; set; }

        [XmlIgnore]
        public Package Package { get; set; }
        public int PackageKey { get; set; }

        // do not convert this yet
        //[StringLength(100)]
        public string IPAddress { get; set; }
        // do not convert this yet
        //[StringLength(2000)]
        public string UserAgent { get; set; }
    }
}
