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
using System.Linq;

namespace NuGetGallery
{
    [Serializable]
    public class SearchResults
    {
        public int Hits { get; private set; }
        public DateTime? IndexTimestampUtc { get; private set; }
        public IQueryable<Package> Data { get; private set; }

        public SearchResults(int hits, DateTime? indexTimestampUtc)
            : this(hits, indexTimestampUtc, Enumerable.Empty<Package>().AsQueryable())
        {
        }

        public SearchResults(int hits, DateTime? indexTimestampUtc, IQueryable<Package> data)
        {
            Hits = hits;
            Data = data;
            IndexTimestampUtc = indexTimestampUtc;
        }
    }
}
