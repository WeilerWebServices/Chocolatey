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
    public interface IConfiguration
    {
        string AzureStorageAccessKey { get; }
        string AzureStorageAccountName { get; }
        string AzureStorageBlobUrl { get; }
        string FileStorageDirectory { get; }
        string AzureCdnHost { get; }
        PackageStoreType PackageStoreType { get; }
        PackageStatisticsStoreType PackageStatisticsStoreType { get; }

        string GetSiteRoot(bool useHttps);

        string GalleryOwnerName { get; }
        string GalleryOwnerEmail { get; }
        bool ConfirmEmailAddresses { get; }

        string S3Bucket { get; }
        string PackagesUrl { get; }
        string S3AccessKey { get; }
        string S3SecretKey { get; }
        string SqsServiceUrl { get; }

        bool UseSmtp { get;  }
        string SmtpHost { get; }
        string SmtpUsername { get;}
        string SmtpPassword { get; }
        int? SmtpPort { get; }
        bool SmtpEnableSsl { get; }
        

        bool UseCaching { get; }
        bool HostImages { get; }
        int PackageOperationsUserKey { get; }
        string ScanResultsKey { get; }
        bool IndexContainsAllVersions { get; }

        bool UseBackgroundJobsDatabaseUser { get; }
        string BackgroundJobsDatabaseUserId { get; }
        string BackgroundJobsDatabaseUserPassword { get; }

    }
}
