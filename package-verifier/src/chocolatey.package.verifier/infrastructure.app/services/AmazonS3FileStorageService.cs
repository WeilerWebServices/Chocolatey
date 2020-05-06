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
using System.IO;
using System.Net;
using Amazon.S3;
using Amazon.S3.Model;

namespace chocolatey.package.verifier.infrastructure.app.services
{
    public class AmazonS3FileStorageService : IFileStorageService
    {
        private readonly IAmazonS3Client clientContext;

        public AmazonS3FileStorageService(IAmazonS3Client clientContext)
        {
            this.clientContext = clientContext;
        }

        private T wrap_request_in_error_handler<T>(Func<T> func)
        {
            try
            {
                return func.Invoke();
            }
            catch (AmazonS3Exception amazonS3Exception)
            {
                if (amazonS3Exception.ErrorCode != null
                    && (
                           amazonS3Exception.ErrorCode.Equals("InvalidAccessKeyId")
                           || amazonS3Exception.ErrorCode.Equals("InvalidSecurity"))
                    )
                {
                    throw new AmazonS3Exception(
                        "Please check the provided AWS Credentials. If you haven't signed up for Amazon S3, please visit http://aws.amazon.com/s3",
                        amazonS3Exception);
                }

                throw;
            }
        }

        public void delete_file(string folderName, string fileName)
        {
            // It's allowed to have an empty folder name. 
            // if (String.IsNullOrWhiteSpace(folderName)) throw new ArgumentNullException("folderName");
            if (String.IsNullOrWhiteSpace(fileName)) throw new ArgumentNullException("fileName");

            folderName = (string.IsNullOrEmpty(folderName) ? String.Empty : folderName.Substring(folderName.Length - 1, 1) == "/" ? folderName : folderName + "/");
            fileName = string.Format("{0}{1}", folderName, fileName);

            var request = new DeleteObjectRequest();
            request.WithBucketName(clientContext.BucketName);
            request.WithKey(fileName);

            using (AmazonS3 client = clientContext.create_instance())
            {
                S3Response response = wrap_request_in_error_handler(() => client.DeleteObject(request));
            }
        }

        public bool file_exists(string folderName, string fileName)
        {
            // It's allowed to have an empty folder name. 
            // if (String.IsNullOrWhiteSpace(folderName)) throw new ArgumentNullException("folderName");
            if (String.IsNullOrWhiteSpace(fileName)) throw new ArgumentNullException("fileName");

            folderName = (string.IsNullOrEmpty(folderName) ? String.Empty : folderName.Substring(folderName.Length - 1, 1) == "/" ? folderName : folderName + "/");
            fileName = string.Format("{0}{1}", folderName, fileName);

            var request = new ListObjectsRequest();
            request.WithBucketName(clientContext.BucketName);
            request.WithPrefix(fileName);

            using (AmazonS3 client = clientContext.create_instance())
            {
                ListObjectsResponse response = wrap_request_in_error_handler(() => client.ListObjects(request));
                var count = response.S3Objects.Count;
                if (count == 1) return true;
            }

            return false;
        }

        public Stream get_file(string folderName, string fileName, bool useCache)
        {
            // It's allowed to have an empty folder name. 
            // if (String.IsNullOrWhiteSpace(folderName)) throw new ArgumentNullException("folderName");
            if (String.IsNullOrWhiteSpace(fileName)) throw new ArgumentNullException("fileName");

            folderName = (string.IsNullOrEmpty(folderName) ? String.Empty : folderName.Substring(folderName.Length - 1, 1) == "/" ? folderName : folderName + "/");
            fileName = string.Format("{0}{1}", folderName, fileName);

            if (useCache && !string.IsNullOrWhiteSpace(clientContext.ImagesUrl))
            {
                var url = new Uri(string.Format("{0}/{1}", clientContext.ImagesUrl, fileName));
                WebRequest request = WebRequest.Create(url);
                WebResponse response = request.GetResponse();

                return response.GetResponseStream();
            }
            else
            {
                var request = new GetObjectRequest();
                request.WithBucketName(clientContext.BucketName);
                request.WithKey(fileName);
                request.WithTimeout((int)TimeSpan.FromMinutes(30).TotalMilliseconds);

                using (AmazonS3 client = clientContext.create_instance())
                {
                    try
                    {
                        S3Response response = wrap_request_in_error_handler(() => client.GetObject(request));

                        if (response != null) return response.ResponseStream;
                    }
                    catch (Exception)
                    {
                        //hate swallowing an error
                    }

                    return null;
                }
            }
        }

        public void save_file(string folderName, string fileName, Stream fileStream)
        {
            // It's allowed to have an empty folder name. 
            // if (String.IsNullOrWhiteSpace(folderName)) throw new ArgumentNullException("folderName");
            if (String.IsNullOrWhiteSpace(fileName)) throw new ArgumentNullException("fileName");
            if (fileStream == null) throw new ArgumentNullException("fileStream");

            folderName = (string.IsNullOrEmpty(folderName) ? String.Empty : folderName.Substring(folderName.Length - 1, 1) == "/" ? folderName : folderName + "/");
            fileName = string.Format("{0}{1}", folderName, fileName);

            var request = new PutObjectRequest();
            request.WithBucketName(clientContext.BucketName);
            request.WithKey(fileName);
            request.WithInputStream(fileStream);
            request.AutoCloseStream = true;
            request.CannedACL = S3CannedACL.PublicRead;
            request.WithTimeout((int)TimeSpan.FromMinutes(30).TotalMilliseconds);

            using (AmazonS3 client = clientContext.create_instance())
            {
                S3Response response = wrap_request_in_error_handler(() => client.PutObject(request));
            }
        }

        public string get_url(string folderName, string fileName)
        {
            folderName = (string.IsNullOrEmpty(folderName) ? String.Empty : folderName.Substring(folderName.Length-1,1) == "/" ? folderName : folderName + "/");

            if (!string.IsNullOrWhiteSpace(clientContext.ImagesUrl))
            {
                return string.Format("{0}/{1}{2}", clientContext.ImagesUrl, folderName, fileName);
            }
            return string.Format("https://{0}.s3.amazonaws.com/{1}{2}", clientContext.BucketName, folderName, fileName);
        }
    }
}
