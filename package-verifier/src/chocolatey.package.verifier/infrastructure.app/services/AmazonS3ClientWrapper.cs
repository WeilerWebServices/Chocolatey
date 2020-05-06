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
using chocolatey.package.verifier.infrastructure.app.configuration;

namespace chocolatey.package.verifier.infrastructure.app.services
{
    public class AmazonS3ClientWrapper : IAmazonS3Client
    {
        private readonly IConfigurationSettings configuration;
        private readonly string accessKeyId = "";
        private readonly string accessSecret = "";

        public AmazonS3ClientWrapper(IConfigurationSettings configuration)
        {
            this.configuration = configuration;
            accessKeyId = configuration.S3AccessKey;
            accessSecret = configuration.S3SecretKey;
            BucketName = configuration.S3Bucket;
            ImagesUrl = configuration.ImagesUrl;
        }

        public string BucketName { get; private set; }
        public string ImagesUrl { get; private set; }

        public AmazonS3 create_instance()
        {
            return AWSClientFactory.CreateAmazonS3Client(accessKeyId, accessSecret);
        }
    }
}
