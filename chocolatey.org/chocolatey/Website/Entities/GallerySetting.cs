
using System;

namespace NuGetGallery
{
    public class GallerySetting : IEntity
    {
        public int Key { get; set; }
        public int? DownloadStatsLastAggregatedId { get; set; }

        //[Obsolete("This has been deprecated to the config file")]
        public string SmtpHost { get; set; }
        //[Obsolete("This has been deprecated to the config file")]
        public string SmtpUsername { get; set; }
        //[Obsolete("This has been deprecated to the config file")]
        public string SmtpPassword { get; set; }
        //[Obsolete("This has been deprecated to the config file")]
        public int? SmtpPort { get; set; }
        //[Obsolete("This has been deprecated to the config file")]
        public bool UseSmtp { get; set; }
        //[Obsolete("This has been deprecated to the config file")]
        public string GalleryOwnerName { get; set; }
        //[Obsolete("This has been deprecated to the config file")]
        public string GalleryOwnerEmail { get; set; }
        //[Obsolete("This has been deprecated to the config file")]
        public bool ConfirmEmailAddresses { get; set; }
        //[Obsolete("This has been deprecated to the config file")]
        public int PackageOperationsUserKey { get; set; }
        //[Obsolete("This has been deprecated to the config file")]
        public string ScanResultsKey { get; set; }
    }
}