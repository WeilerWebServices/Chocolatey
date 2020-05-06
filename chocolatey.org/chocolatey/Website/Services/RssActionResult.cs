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

using System.ServiceModel.Syndication;
using System.Web.Mvc;
using System.Xml;

namespace NuGetGallery
{
    public class RSSActionResult : ActionResult
    {
        public SyndicationFeed Feed { get; set; }

        public override void ExecuteResult(ControllerContext context)
        {
            context.HttpContext.Response.ContentType = "application/rss+xml";

            if (Feed != null)
            {
                var rssFormatter = new Rss20FeedFormatter(Feed);
                //remove a10 - http://stackoverflow.com/a/15971300/18475

                using (var xmlWriter = new XmlTextWriter(context.HttpContext.Response.Output))
                {
                    xmlWriter.Formatting = Formatting.Indented;
                    rssFormatter.WriteTo(xmlWriter);
                }
            }
        }
    }
}
