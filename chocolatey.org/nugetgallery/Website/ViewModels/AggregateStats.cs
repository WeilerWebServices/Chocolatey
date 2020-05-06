
using System;

namespace NuGetGallery
{
    [Serializable]
    public class AggregateStats
    {
        public long Downloads { get; set; }

        public int UniquePackages { get; set; }

        public int TotalPackages { get; set; }
    }
}