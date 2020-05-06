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
using System.Configuration;
using System.Linq;
using System.Web;

namespace NuGetGallery.MvcOverrides
{
    public static class AppHarbor
    {
        public static bool IsSecureConnection(HttpContextBase context)
        {
            //var context = HttpContext.Current;
            if (context == null) return false;

            if (context.Request.IsSecureConnection) return true;
            if (ConfigurationManager.AppSettings.Get("ForceSSL").Equals(bool.TrueString, StringComparison.InvariantCultureIgnoreCase)) return true; 

            var protoHeaders = context.Request.Headers.GetValues("X-Forwarded-Proto");
            if (protoHeaders != null)
            {
                if (string.Equals(
                    protoHeaders.FirstOrDefault(),
                    "https",
                    StringComparison.InvariantCultureIgnoreCase)) return true;
            }

            return false;
        }
    }
}
