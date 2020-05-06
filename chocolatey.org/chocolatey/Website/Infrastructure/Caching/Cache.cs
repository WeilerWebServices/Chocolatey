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
using Glav.CacheAdapter.Core;

namespace NugetGallery
{
    /// <summary>
    ///   The caching mechanism for the application
    /// </summary>
    public static class Cache
    {
        private static ICacheProvider _cacheProvider;

        public const int DEFAULT_CACHE_TIME_MINUTES = 60;

        /// <summary>
        ///   Initializes the cache provider for use by the application
        /// </summary>
        /// <param name="cacheProvider">The cache provider.</param>
        public static void InitializeWith(ICacheProvider cacheProvider)
        {
            _cacheProvider = cacheProvider;
        }

        /// <summary>
        ///   Gets the specified cache key. If it doesn't exist, it caches it first.
        /// </summary>
        /// <typeparam name="T">Instance type to cache</typeparam>
        /// <param name="cacheKey">The cache key.</param>
        /// <param name="asbsoluteExpiryDate">The asbsolute expiry date.</param>
        /// <param name="getData">
        ///   The function that you want to cache the result of, should be an instance of type <see cref="T" />.
        /// </param>
        /// <returns></returns>
        public static T Get<T>(string cacheKey, DateTime asbsoluteExpiryDate, Func<T> getData)
            where T : class
        {
            if (_cacheProvider != null)
            {
                //typeof (Cache).Log().Debug(() => "Caching/getting key '{0}' with results of function".FormatWith(cacheKey));

                return _cacheProvider.Get(cacheKey, asbsoluteExpiryDate, getData);
            }

            return getData.Invoke();
        }

        /// <summary>
        ///   Invalidates the cache item.
        /// </summary>
        /// <param name="cacheKey">The cache key.</param>
        public static void InvalidateCacheItem(string cacheKey)
        {
            if (_cacheProvider != null)
            {
                //typeof (Cache).Log().Debug(() => "Erasing the cache for key '{0}'".FormatWith(cacheKey));

                _cacheProvider.InvalidateCacheItem(cacheKey);
            }
        }

        public static void ClearAll()
        {
            if (_cacheProvider != null)
            {
                //typeof (Cache).Log().Debug(() => "Erasing the cache for everything");

                _cacheProvider.ClearAll();
            }
        }
    }
}
