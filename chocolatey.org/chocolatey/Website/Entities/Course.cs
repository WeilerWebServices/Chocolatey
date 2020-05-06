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
using System.Collections.Generic;

namespace NuGetGallery
{
    public class Course : IEntity
    {
        public int Key { get; set; }
        [StringLength(255)]
        public string Name { get; set; }
        //[StringLength(255)]
        //public string Url { get; set; }
        //[StringLength(400)]
        //public string BadgeImage { get; set; }
        //public string BadgeImageAlt { get; set; }

        //public string CourseLength { get; set; }
        //public string CourseLevel { get; set; }
        //public string CourseDescription { get; set; }
        //public int CourseModuleCount { get; set; }

        public CourseNameType CourseNameType { get; set; }
        [MaxLength(100)]
        [Column("CourseNameType")]
        public string CourseNameTypeForDatabase
        {
            get { return CourseNameType.ToString(); }
            set
            {
                if (value == null) CourseNameType = CourseNameType.Unknown;
                else CourseNameType = (CourseNameType)Enum.Parse(typeof(CourseNameType), value);
            }
        }

        public virtual ICollection<CourseModule> CourseModules { get; set; }
    }
}
