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
    public class CourseModule : IEntity
    {
        public int Key { get; set; }

        public Course Course { get; set; }
        public int CourseKey { get; set; }

        [StringLength(255)]
        public string Name { get; set; }

        [StringLength(200)]
        public string Description { get; set; }
        //[StringLength(255)]
        //public string Url { get; set; }
        [StringLength(10)]
        public string ModuleLength { get; set; }
        public int ModuleQuestionCount { get; set; }
        public int Order { get; set; }

        public CourseModuleNameType CourseModuleNameType { get; set; }
        [MaxLength(100)]
        [Column("CourseModuleNameType")]
        public string CourseModuleNameTypeForDatabase
        {
            get { return CourseModuleNameType.ToString(); }
            set
            {
                if (value == null) CourseModuleNameType = CourseModuleNameType.Unknown;
                else CourseModuleNameType = (CourseModuleNameType)Enum.Parse(typeof(CourseModuleNameType), value);
            }
        }

    }
}