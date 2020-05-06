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
using System.Data.Entity;
using System.Linq;

namespace NuGetGallery
{

    public class CourseAchievementsService : ICourseAchievementsService
    {
        private readonly IEntityRepository<UserCourseAchievement> _courseCourseAchievementRepository;
        private readonly IEntityRepository<Course> _courseRepository;

        public CourseAchievementsService(IEntityRepository<UserCourseAchievement> courseAchievementRepository, IEntityRepository<Course> courseRepository)
        {
            _courseCourseAchievementRepository = courseAchievementRepository;
            _courseRepository = courseRepository;
        }

        public IEnumerable<UserCourseAchievement> GetUserCourseAchievements(User user)
        {
            return _courseCourseAchievementRepository.GetAll()
                            .Where(x => x.UserKey == user.Key)
                            .Include(x => x.CourseModuleAchievements)
                            .ToList();
        }

        public void SaveCourseAchievements(User user, UserCourseAchievement achievement, int courseModuleKey)
        {
            var existingAchievements = GetUserCourseAchievements(user).AsQueryable();

            CompareAndPrepareCourseAchievement(achievement, courseModuleKey, user, existingAchievements);

            _courseCourseAchievementRepository.CommitChanges();
        }

        private void CompareAndPrepareCourseAchievement(
            UserCourseAchievement courseAchievementCandidate,
            int courseModuleKey,
            User user,
            IQueryable<UserCourseAchievement> existingAchievements)
        {
            var courseKey = courseAchievementCandidate.CourseKey;
            var courseAchievement = existingAchievements.FirstOrDefault(x => x.CourseKey == courseKey);
            if (courseAchievement == null)
            {
                courseAchievement = new UserCourseAchievement
                {
                    CourseKey = courseKey,
                    UserKey = user.Key
                };
                _courseCourseAchievementRepository.InsertOnCommit(courseAchievement);
            }

        
            if (courseAchievement.CourseModuleAchievements == null)
            {
                courseAchievement.CourseModuleAchievements = new List<UserCourseModuleAchievement>();
            }

            UserCourseModuleAchievement moduleAchievement = courseAchievement.CourseModuleAchievements.OrEmptyListIfNull().FirstOrDefault(a => a.CourseModuleKey == courseModuleKey);
            if (moduleAchievement == null)
            {
                moduleAchievement = new UserCourseModuleAchievement
                {
                    UserCourseAchievement = courseAchievement,
                    CourseModuleKey = courseModuleKey
                };

                courseAchievement.CourseModuleAchievements.Add(moduleAchievement);
            }

            moduleAchievement.CompletedDate = DateTime.UtcNow;

            var course = _courseRepository.GetAll().Include(c => c.CourseModules).FirstOrDefault(x => x.Key == courseKey);
            // should not be null
            if (course != null)
            {
                if (courseAchievement.CourseModuleAchievements.Count >= course.CourseModules.Count)
                {
                    courseAchievement.Completed = true;
                    courseAchievement.CompletedDate = DateTime.UtcNow;
                }
            }
        }
    }
}
