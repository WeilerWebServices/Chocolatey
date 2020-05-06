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
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Web.Mvc;

namespace NuGetGallery
{
    public static class EnumerationExtensions
    {
        public static IEnumerable<SelectListItem> GetEnumerationItems(this Enum enumeration)
        {
            var listItems = Enum
                .GetValues(enumeration.GetType())
                .OfType<Enum>()
                .Select(
                    e =>
                    new SelectListItem
                    {
                        Text = e.GetDescriptionOrValue(),
                        Value = e.ToString(),
                        Selected = e.Equals(enumeration)
                    });

            return listItems;
        }

        /// <summary>
        ///   Gets the description [Description("")] or ToString() value of an enumeration.
        /// </summary>
        /// <param name="enumeration">The enumeration item.</param>
        public static string GetDescriptionOrValue(this Enum enumeration)
        {
            string description = enumeration.ToString();

            Type type = enumeration.GetType();
            MemberInfo[] memInfo = type.GetMember(description);

            if (memInfo != null && memInfo.Length > 0)
            {
                var attrib =
                    memInfo[0].GetCustomAttributes(typeof(DescriptionAttribute), false)
                              .Cast<DescriptionAttribute>()
                              .SingleOrDefault();

                if (attrib != null) description = attrib.Description;
            }

            return description;
        }
    }
}
