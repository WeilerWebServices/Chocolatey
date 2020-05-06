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
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace NuGetGallery
{
    public class CourseViewModel
    {
        public CourseNameType CourseNameType { get; set; }

        public string Username { get; set; }
        public int UserKey { get; set; }

        [Display(Name = "Question One")]
        public string QuestionOne { get; set; }

        [Display(Name = "Question Two")]
        public string QuestionTwo { get; set; }

        [Display(Name = "Question Three")]
        public string QuestionThree { get; set; }

        [Display(Name = "Question Four")]
        public string QuestionFour { get; set; }

        public string CourseName { get; set; }

        public int CourseKey { get; set; }

        public bool CompletedCourse { get; set; }

        public DateTime? CompletedDate { get; set; }

        public ICollection<CourseAchievementViewModel> UserCourseAchievements { get; set; }

        public IList<CourseModuleViewModel> CourseModules { get; set; }

        public int CourseModuleKey { get; set; }
    }
}
