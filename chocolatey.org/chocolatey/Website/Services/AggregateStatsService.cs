using System;
using System.Data;
using NugetGallery;

namespace NuGetGallery
{
    public class AggregateStatsService : IAggregateStatsService
    {
        public AggregateStats GetAggregateStats()
        {
            return Cache.Get("aggregatestats",
                    DateTime.UtcNow.AddMinutes(5),
                    () =>
                    {
                        using (var dbContext = new EntitiesContext())
                        {
                            var database = dbContext.Database;
                            using (var command = database.Connection.CreateCommand())
                            {
                                command.CommandText = @"SELECT
    SUM(Case When [IsLatest] = 1 Then 1 Else 0 End) AS UniquePackages
  , SUM(CASE WHEN Listed = 1 THEN 1 ELSE 0 END) AS TotalPackages
  , SUM(DownloadCount) AS DownloadCount
  , SUM(Case When [Status] = 'Submitted' And SubmittedStatus <> 'Waiting' Then 1 Else 0 End) AS PackagesReadyForReview
  , SUM(Case When [Status] = 'Submitted' Then 1 Else 0 End) AS AllPackagesUnderModeration
  , (Select AVG(DATEDIFF(HOUR, Created, ApprovedDate)) From dbo.Packages With (NOLOCK) Where [Status] = 'Approved' And ReviewedById is Not Null And Created >= DATEADD(DAY, -30, GETUTCDATE())) AS AverageModerationWaitTime
  , SUM(Case When IsLatest = 1 And Created >= DATEADD(MONTH, -4, GETUTCDATE()) Then 1 Else 0 End) AS UpToDatePackages
  , SUM(Case When IsLatest = 1 And Created < DATEADD(YEAR, -1, GETUTCDATE()) Then 1 Else 0 End) AS OlderThanOneYearPackages
  , SUM(Case When [Status] = 'Approved' And [IsLatestStable] = 1 Then 1 Else 0 End) AS ApprovedPackages
  , SUM(Case When [Status] = 'Approved' Then 1 Else 0 End) AS TotalApprovedPackages
  , SUM(Case When [Status] = 'Approved' And [ReviewedById] Is Not Null And [IsLatestStable] = 1 Then 1 Else 0 End) AS ManuallyApprovedPackages
  , SUM(Case When [Status] = 'Approved' And [ReviewedById] Is Not Null Then 1 Else 0 End) AS TotalManuallyApprovedPackages
  , SUM(Case When [Status] = 'Approved' And [ReviewedById] Is Null And [IsLatestStable] = 1 Then 1 Else 0 End) AS TrustedPackages
  , SUM(Case When [Status] = 'Approved' And [ReviewedById] Is Null Then 1 Else 0 End) AS TotalTrustedPackages
  , SUM(Case When [Status] = 'Rejected' Then 1 Else 0 End) AS TotalRejectedPackages
  , SUM(Case When [Status] = 'Exempted' And [ReviewedById] Is Not Null And [IsLatestStable] = 1 Then 1 Else 0 End) AS ExemptedPackages
  , SUM(Case When [Status] = 'Exempted' And [ReviewedById] Is Not Null Then 1 Else 0 End) AS TotalExemptedPackages
  , SUM(Case When ([Status] <> 'Approved' Or [Status] Is Null) And [IsLatestStable] = 1 Then 1 Else 0 End) AS UnknownPackages
  , SUM(Case When [Status] <> 'Approved' Or [Status] Is Null Then 1 Else 0 End) AS TotalUnknownPackages
  , SUM(Case When [IsLatest] = 1 And [IsLatestStable] = 0 Then 1 Else 0 End) AS LatestPackagePrerelease
  , SUM(Case When [Listed] = 0 Then 1 Else 0 End) AS TotalUnlistedPackages
  , SUM(Case When [IsLatestStable] = 1 And [PackageSourceUrl] Is Not Null Then 1 Else 0 End) AS PackagesWithPackageSource
  , SUM(Case When IsLatestStable = 1 And PackageTestResultStatus = 'Passing' Then 1 Else 0 End) AS PackagesPassingVerification
  , SUM(Case When IsLatestStable = 1 And PackageTestResultStatus = 'Failing' Then 1 Else 0 End) AS PackagesFailingVerification
  , SUM(Case When [IsLatestStable] = 1 And PackageValidationResultStatus = 'Passing' Then 1 Else 0 End) AS PackagesPassingValidation
  , SUM(Case When [IsLatestStable] = 1 And PackageValidationResultStatus = 'Failing' Then 1 Else 0 End) AS PackagesFailingValidation
  , SUM(Case When [IsLatestStable] = 1 And DownloadCacheDate Is Not Null Then 1 Else 0 End) AS PackagesCached
  , SUM(Case When DownloadCacheDate Is Not Null Then 1 Else 0 End) AS TotalPackagesCached
  , SUM(Case When [IsLatestStable] = 1 And DownloadCacheStatus = 'Available' Then 1 Else 0 End) AS PackagesCachedAvailable
  , SUM(Case When DownloadCacheStatus = 'Available' Then 1 Else 0 End) AS TotalPackagesCachedAvailable
  , SUM(Case When [IsLatestStable] = 1 And DownloadCacheStatus = 'Investigate' Then 1 Else 0 End) AS PackagesCachedInvestigate
  , SUM(Case When DownloadCacheStatus = 'Investigate' Then 1 Else 0 End) AS TotalPackagesCachedInvestigate
  , SUM(Case When [IsLatestStable] = 1 And PackageScanResultDate Is Not Null Then 1 Else 0 End) AS PackagesScanned
  , SUM(Case When PackageScanResultDate Is Not Null Then 1 Else 0 End) AS TotalPackagesScanned
  , SUM(Case When [IsLatestStable] = 1 And PackageScanStatus = 'NotFlagged' Then 1 Else 0 End) AS PackagesScannedNotFlagged
  , SUM(Case When PackageScanStatus = 'NotFlagged' Then 1 Else 0 End) AS TotalPackagesScannedNotFlagged
  , SUM(Case When [IsLatestStable] = 1 And PackageScanStatus = 'Flagged' Then 1 Else 0 End) AS PackagesScannedFlagged
  , SUM(Case When PackageScanStatus = 'Flagged' Then 1 Else 0 End) AS TotalPackagesScannedFlagged
  , SUM(Case When [IsLatestStable] = 1 And PackageScanStatus = 'Exempted' Then 1 Else 0 End) AS PackagesScannedExempted
  , SUM(Case When PackageScanStatus = 'Exempted' Then 1 Else 0 End) AS TotalPackagesScannedExempted
  , SUM(Case When [IsLatestStable] = 1 And PackageScanStatus = 'Investigate' Then 1 Else 0 End) AS PackagesScannedInvestigate
  , SUM(Case When PackageScanStatus = 'Investigate' Then 1 Else 0 End) AS TotalPackagesScannedInvestigate
  , (Select COUNT(ScanOverlaps) From (select count([ScanResultKey]) as ScanOverlaps from [dbo].[PackageScanResults] with (nolock) group by ScanResultKey having count(ScanResultKey) > 1) overlaps) AS TotalFileScanOverlaps
  , (Select COUNT([Key]) From [dbo].[ScanResults] With (NOLOCK)) AS TotalFileScans
FROM dbo.Packages
";

                                database.Connection.Open();
                                using (var reader = command.ExecuteReader(CommandBehavior.CloseConnection | CommandBehavior.SingleRow))
                                {
                                    bool hasData = reader.Read();
                                    if (!hasData) return new AggregateStats();

                                    return new AggregateStats
                                    {
                                        UniquePackages = reader.IsDBNull(0) ? 0 : reader.GetInt32(0),
                                        TotalPackages = reader.IsDBNull(1) ? 0 : reader.GetInt32(1),
                                        Downloads = reader.IsDBNull(2) ? 0 : reader.GetInt32(2),
                                        PackagesReadyForReviewModeration = reader.IsDBNull(3) ? 0 : reader.GetInt32(3),
                                        TotalPackagesInModeration = reader.IsDBNull(4) ? 0 : reader.GetInt32(4),
                                        AverageModerationWaitTimeHours = reader.IsDBNull(5) ? 0 : reader.GetInt32(5),
                                        UpToDatePackages = reader.IsDBNull(6) ? 0 : reader.GetInt32(6),
                                        OlderThanOneYearPackages = reader.IsDBNull(7) ? 0 : reader.GetInt32(7),
                                        ApprovedPackages = reader.IsDBNull(8) ? 0 : reader.GetInt32(8),
                                        TotalApprovedPackages = reader.IsDBNull(9) ? 0 : reader.GetInt32(9),
                                        ManuallyApprovedPackages = reader.IsDBNull(10) ? 0 : reader.GetInt32(10),
                                        TotalManuallyApprovedPackages = reader.IsDBNull(11) ? 0 : reader.GetInt32(11),
                                        TrustedPackages = reader.IsDBNull(12) ? 0 : reader.GetInt32(12),
                                        TotalTrustedPackages = reader.IsDBNull(13) ? 0 : reader.GetInt32(13),
                                        TotalRejectedPackages = reader.IsDBNull(14) ? 0 : reader.GetInt32(14),
                                        ExemptedPackages = reader.IsDBNull(15) ? 0 : reader.GetInt32(15),
                                        TotalExemptedPackages = reader.IsDBNull(16) ? 0 : reader.GetInt32(16),
                                        UnknownPackages = reader.IsDBNull(17) ? 0 : reader.GetInt32(17),
                                        TotalUnknownPackages = reader.IsDBNull(18) ? 0 : reader.GetInt32(18),
                                        LatestPackagePrerelease = reader.IsDBNull(19) ? 0 : reader.GetInt32(19),
                                        TotalUnlistedPackages = reader.IsDBNull(20) ? 0 : reader.GetInt32(20),
                                        PackagesWithPackageSource = reader.IsDBNull(21) ? 0 : reader.GetInt32(21),
                                        PackagesPassingVerification = reader.IsDBNull(22) ? 0 : reader.GetInt32(22),
                                        PackagesFailingVerification = reader.IsDBNull(23) ? 0 : reader.GetInt32(23),
                                        PackagesPassingValidation = reader.IsDBNull(24) ? 0 : reader.GetInt32(24),
                                        PackagesFailingValidation = reader.IsDBNull(25) ? 0 : reader.GetInt32(25),
                                        PackagesCached = reader.IsDBNull(26) ? 0 : reader.GetInt32(26),
                                        TotalPackagesCached = reader.IsDBNull(27) ? 0 : reader.GetInt32(27),
                                        PackagesCachedAvailable = reader.IsDBNull(28) ? 0 : reader.GetInt32(28),
                                        TotalPackagesCachedAvailable = reader.IsDBNull(29) ? 0 : reader.GetInt32(29),
                                        PackagesCachedInvestigate = reader.IsDBNull(30) ? 0 : reader.GetInt32(30),
                                        TotalPackagesCachedInvestigate = reader.IsDBNull(31) ? 0 : reader.GetInt32(31),
                                        PackagesScanned = reader.IsDBNull(32) ? 0 : reader.GetInt32(32),
                                        TotalPackagesScanned = reader.IsDBNull(33) ? 0 : reader.GetInt32(33),
                                        PackagesScannedNotFlagged = reader.IsDBNull(34) ? 0 : reader.GetInt32(34),
                                        TotalPackagesScannedNotFlagged = reader.IsDBNull(35) ? 0 : reader.GetInt32(35),
                                        PackagesScannedFlagged = reader.IsDBNull(36) ? 0 : reader.GetInt32(36),
                                        TotalPackagesScannedFlagged = reader.IsDBNull(37) ? 0 : reader.GetInt32(37),
                                        PackagesScannedExempted = reader.IsDBNull(38) ? 0 : reader.GetInt32(38),
                                        TotalPackagesScannedExempted = reader.IsDBNull(39) ? 0 : reader.GetInt32(39),
                                        PackagesScannedInvestigate = reader.IsDBNull(40) ? 0 : reader.GetInt32(40),
                                        TotalPackagesScannedInvestigate = reader.IsDBNull(41) ? 0 : reader.GetInt32(41),
                                        TotalFileScanOverlaps = reader.IsDBNull(42) ? 0 : reader.GetInt32(42),
                                        TotalFileScans = reader.IsDBNull(43) ? 0 : reader.GetInt32(43),
                                    };
                                }
                            }
                        }
                    });
        }
    }
}