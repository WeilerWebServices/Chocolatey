
using System;

namespace NuGetGallery
{
    [Serializable]
    public class AggregateStats
    {
        public long Downloads { get; set; }
        public int UniquePackages { get; set; }
        public int TotalPackages { get; set; }
        public int PackagesReadyForReviewModeration { get; set; }
        public int TotalPackagesInModeration { get; set; }
        public int AverageModerationWaitTimeHours { get; set; }
        public int UpToDatePackages { get; set; }
        public int OlderThanOneYearPackages { get; set; }
        public int ApprovedPackages { get; set; }
        public int TotalApprovedPackages { get; set; }
        public int ManuallyApprovedPackages { get; set; }
        public int TotalManuallyApprovedPackages { get; set; }
        public int TrustedPackages { get; set; }
        public int TotalTrustedPackages { get; set; }
        public int TotalRejectedPackages { get; set; }
        public int ExemptedPackages { get; set; }
        public int TotalExemptedPackages { get; set; }
        public int UnknownPackages { get; set; }
        public int TotalUnknownPackages { get; set; }
        public int LatestPackagePrerelease { get; set; }
        public int TotalUnlistedPackages { get; set; }
        public int PackagesWithPackageSource { get; set; }
        public int PackagesPassingVerification { get; set; }
        public int PackagesFailingVerification { get; set; }
        public int PackagesPassingValidation { get; set; }
        public int PackagesFailingValidation { get; set; }
        public int PackagesCached { get; set; }
        public int TotalPackagesCached { get; set; }
        public int PackagesCachedAvailable { get; set; }
        public int TotalPackagesCachedAvailable { get; set; }
        public int PackagesCachedInvestigate { get; set; }
        public int TotalPackagesCachedInvestigate { get; set; }
        public int PackagesScanned { get; set; }
        public int TotalPackagesScanned { get; set; }
        public int PackagesScannedNotFlagged { get; set; }
        public int TotalPackagesScannedNotFlagged { get; set; }
        public int PackagesScannedFlagged { get; set; }
        public int TotalPackagesScannedFlagged { get; set; }
        public int PackagesScannedExempted { get; set; }
        public int TotalPackagesScannedExempted { get; set; }
        public int PackagesScannedInvestigate { get; set; }
        public int TotalPackagesScannedInvestigate { get; set; }
        public int TotalFileScanOverlaps { get; set; }
        public int TotalFileScans { get; set; }

    }
}