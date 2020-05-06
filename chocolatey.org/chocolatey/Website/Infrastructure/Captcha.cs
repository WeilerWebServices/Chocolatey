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

using System.Collections.Generic;
using System.Collections.Specialized;
using System.Net;
using System.Text;
using System.Web.Configuration;
using Newtonsoft.Json;

namespace NuGetGallery.Infrastructure
{
    public static class Captcha
    {
        public static CaptchaResponse ValidateCaptcha(string response)
        {
            using (var client = new WebClient())
            {
                var requestParameters = new NameValueCollection();
                requestParameters.Add("secret", WebConfigurationManager.AppSettings["reCAPTCHA::PrivateKey"]);
                requestParameters.Add("response", response);
                var responseBytes = client.UploadValues("https://www.google.com/recaptcha/api/siteverify", "POST", requestParameters);
                var responseBody = Encoding.UTF8.GetString(responseBytes);
                return JsonConvert.DeserializeObject<CaptchaResponse>(responseBody);
            }
        }
    }

    public class CaptchaResponse
    {
        [JsonProperty("success")]
        public bool Success
        {
            get;
            set;
        }

        [JsonProperty("error-codes")]
        public List<string> ErrorMessage
        {
            get;
            set;
        }
    }
}
