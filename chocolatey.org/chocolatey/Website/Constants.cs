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

namespace NuGetGallery
{
    public static class Constants
    {
        public const string AdminRoleName = "Admins";
        public const string ModeratorsRoleName = "Moderators";
        public const string ReviewersRoleName = "Reviewers";
        public const string AlphabeticSortOrder = "package-title";
        public const int DefaultPackageListPageSize = 30;
        public const string DefaultPackageListSortOrder = "package-download-count";
        public const int DefaultPasswordResetTokenExpirationHours = 24;
        public const int MaxEmailSubjectLength = 255;
        public const string PackageContentType = "application/zip";
        public const string OctetStreamContentType = "application/octet-stream";
        public const string NuGetPackageFileExtension = ".nupkg";
        public const string PackageFileDownloadUriTemplate = "packages/{0}/{1}/download";
        public const string PackageFileSavePathTemplate = "{0}.{1}{2}";

        public const string PackagesFolderName = "packages";
        public const string DownloadsFolderName = "downloads";

        public const string PackageImagesFolderName = "packageimages";
        public const string ImageExtension = ".png";

        public const string PopularitySortOrder = "package-download-count";
        public const string RecentSortOrder = "package-created";
        public const string RelevanceSortOrder = "relevance";

        public const string SubmittedModerationStatus = "submitted-status";
        public const string PendingModerationStatus = "pending-status";
        public const string WaitingModerationStatus = "waiting-status";
        public const string RespondedModerationStatus = "responded-status";
        public const string ReadyModerationStatus = "ready-status";
        public const string UpdatedModerationStatus = "updated-status";
        public const string UnknownModerationStatus = "unknown-status";
        public const string AllModerationStatuses = "all-statuses";

        public const string Sha1HashAlgorithmId = "SHA1";
        public const string Sha512HashAlgorithmId = "SHA512";
        public const string PBKDF2HashAlgorithmId = "PBKDF2";

        public const string UploadFileNameTemplate = "{0}{1}";
        public const string UploadsFolderName = "uploads";
        public const string NuGetCommandLinePackageId = "NuGet.CommandLine";
        public const string ReturnUrlViewDataKey = "ReturnUrl";

        public const string MODERATION_SUBMITTED = "Submitted for Moderation Review";
        public const string MODERATION_FINISHED = "Finished Automated Moderation Review";
        public const string MODERATION_VERIFICATION_PASS = "Passed Verification Testing";
        public const string MODERATION_VALIDATION_PASS = "Passed Validation Testing - Findings";
        public const string MODERATION_VALIDATION_PASS_NO_FINDINGS = "Passed Validation Testing";
    }
}
