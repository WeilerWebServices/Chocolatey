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

using Amazon;
using Amazon.S3;

namespace NuGetGallery
{
    public class AmazonS3ClientWrapper : IAmazonS3Client
    {
        private readonly IConfiguration configuration;
        private readonly string accessKeyId = "";
        private readonly string accessSecret = "";
        private readonly string bucketName = "";
        private readonly string packagesUrl = "";

        public AmazonS3ClientWrapper(IConfiguration configuration)
        {
            this.configuration = configuration;
            accessKeyId = configuration.S3AccessKey;
            accessSecret = configuration.S3SecretKey;
            bucketName = configuration.S3Bucket;
            packagesUrl = configuration.PackagesUrl;
        }

        public string BucketName { get { return bucketName; } }
        public string PackagesUrl { get { return packagesUrl; } }

        public AmazonS3 CreateInstance()
        {
            return AWSClientFactory.CreateAmazonS3Client(accessKeyId, accessSecret);
        }
    }
}
