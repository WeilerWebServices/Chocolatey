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

namespace NuGetGallery
{
    public class CourseAchievementViewModel
    {
        public CourseAchievementViewModel(UserCourseAchievement courseAchievement)
        {
            Key = courseAchievement.Key;
            UserKey = courseAchievement.UserKey;
            User = courseAchievement.User;
            CourseKey = courseAchievement.CourseKey;
            Completed = courseAchievement.Completed;
            CompletedDate = courseAchievement.CompletedDate;

            CourseModuleAchievements = new List<CourseModuleAchievementViewModel>();
            foreach (var moduleAchievement in courseAchievement.CourseModuleAchievements.OrEmptyListIfNull())
            {
                CourseModuleAchievements.Add(new CourseModuleAchievementViewModel(moduleAchievement));
            }
        }

        public int Key { get; set; }
        public int UserKey { get; set; }
        public User User { get; set; }
        public int CourseKey { get; set; }
        public ICollection<CourseModuleAchievementViewModel> CourseModuleAchievements { get; private set; }
        public bool Completed { get; set; }
        public DateTime? CompletedDate { get; set; }
    }
}
