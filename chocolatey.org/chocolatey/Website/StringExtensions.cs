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
    public static class StringExtensions
    {
        /// <summary>
        ///   Formats string with the formatting passed in. This is a shortcut to string.Format().
        /// </summary>
        /// <param name="input">The input.</param>
        /// <param name="formatting">The formatting.</param>
        /// <returns>A formatted string.</returns>
        public static string format_with(this string input, params object[] formatting)
        {
            if (string.IsNullOrWhiteSpace(input)) return string.Empty;

            try
            {
                return string.Format(input, formatting);
            }
            catch (Exception)
            {
                return input;
            }
        }

        /// <summary>
        ///   Gets a string representation unless input is null.
        /// </summary>
        /// <param name="input">The input.</param>
        /// <returns></returns>
        public static string to_string(this string input)
        {
            if (string.IsNullOrWhiteSpace(input)) return string.Empty;

            return input;
        }

        public static string to_lower(this string input)
        {
            if (string.IsNullOrWhiteSpace(input)) return input;

            return input.ToLower();
        }

        public static string to_lower_invariant(this string input)
        {
            if (string.IsNullOrWhiteSpace(input)) return input;

            return input.ToLowerInvariant();
        }

        public static string[] split_safe(this string s, char[] separator, StringSplitOptions stringSplitOptions)
        {
            if (s == null)
            {
                return new string[0];
            }

            return s.Split(separator, stringSplitOptions);
        }

        public static string clean_html(this string input)
        {
            if (string.IsNullOrWhiteSpace(input)) return input;

            return input.Replace("<","&lt;").Replace(">","&gt;");
        }

    }
}
