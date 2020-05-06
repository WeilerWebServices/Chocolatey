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
using Amazon.SQS;
using Amazon.SQS.Model;

namespace NuGetGallery
{
    public class AmazonSqsPackageStatisticsService : IPackageStatisticsService
    {
         private readonly IAmazonSqsClient clientContext;

         public AmazonSqsPackageStatisticsService(IAmazonSqsClient clientContext)
        {
            this.clientContext = clientContext;
        }

        public void RecordPackageDownloadStatistics(int packageKey, string userHostAddress, string userAgent)
        {
            var stats = new PackageStatistics
            {
                // IMPORTANT: We may be able to get timestamp from message
                IPAddress = userHostAddress,
                UserAgent = userAgent,
                PackageKey = packageKey
            };
            
            var message = stats.ToXml();

            var request = new SendMessageRequest();
            request.WithQueueUrl(clientContext.QueueUrl);
            request.WithDelaySeconds(0);
            request.WithMessageBody(message);

            using (AmazonSQS client = clientContext.CreateInstance())
            {
                SendMessageResponse response = WrapRequestInErrorHandler(() => client.SendMessage(request));
            } 
        }

        private T WrapRequestInErrorHandler<T>(Func<T> func)
        {
            try
            {
                return func.Invoke();
            }
            catch (AmazonSQSException amazonSqsException)
            {
                if (amazonSqsException.ErrorCode != null
                    && (
                           amazonSqsException.ErrorCode.Equals("InvalidAccessKeyId")
                           || amazonSqsException.ErrorCode.Equals("InvalidSecurity"))
                    )
                {
                    throw new AmazonSQSException(
                        "Please check the provided AWS Credentials. If you haven't signed up for Amazon Web Services, please visit http://aws.amazon.com",
                        amazonSqsException);
                }

                throw;
            }
        }
    }
}
