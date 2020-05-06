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
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.ServiceModel.Syndication;
using System.Text;
using System.Text.RegularExpressions;
using System.Web.Mvc;
using System.Web.UI;
using Markdig;
using NuGetGallery.MvcOverrides;

namespace NuGetGallery.Controllers
{
    public class BlogController : Controller
    {
        private readonly IFileSystemService _fileSystem;
        public IConfiguration Configuration { get; set; }
        public MarkdownPipeline MarkdownPipeline { get; set; }

        public BlogController(IFileSystemService fileSystem, IConfiguration configuration)
        {
            _fileSystem = fileSystem;
            Configuration = configuration;

            MarkdownPipeline = new MarkdownPipelineBuilder()
                 .UseSoftlineBreakAsHardlineBreak()
                 .UseAdvancedExtensions()
                 .Build();
        }

        public ActionResult Index()
        {
            var posts = GetPostsByMostRecentFirst();

            return View("~/Views/Pages/MarkdownPostIndex.cshtml", "~/Views/Blog/_Layout.cshtml", posts);
        }

        [ActionName("blog.rss"), HttpGet, OutputCache(VaryByParam = "page;pageSize", Location = OutputCacheLocation.Any, Duration = 3630)]
        public virtual ActionResult Feed(int? page, int? pageSize)
        {
            var blogRoot = EnsureTrailingSlash(Configuration.GetSiteRoot(AppHarbor.IsSecureConnection(HttpContext))) + "blog";

            var posts = GetPostsByMostRecentFirst();
            if (page != null && pageSize != null)
            {
                int skip = page.GetValueOrDefault() * pageSize.GetValueOrDefault(1);
                posts = posts.Skip(skip).Take(pageSize.GetValueOrDefault(1));
            }
            else if (pageSize != null) posts = posts.Take(pageSize.GetValueOrDefault(1));

            var feed = new SyndicationFeed("Chocolatey Blog", "The official blog for Chocolatey, the package manager for Windows!", new Uri(blogRoot));
            feed.Copyright = new TextSyndicationContent("Chocolatey copyright RealDimensions Software, LLC, posts copyright original author or authors.");
            feed.Language = "en-US";
            feed.Authors.Add(new SyndicationPerson(string.Empty, "The Chocolatey Team", string.Empty));

            var items = new List<SyndicationItem>();
            foreach (var post in posts.OrEmptyListIfNull())
            {
                var blogUrl = blogRoot + "/" + post.UrlPath;
                var item = new SyndicationItem(post.Title, post.Post, new Uri(blogUrl), post.UrlPath, post.Published.GetValueOrDefault());
                item.PublishDate = post.Published.GetValueOrDefault();
                item.Authors.Add(new SyndicationPerson(string.Empty, post.Author, string.Empty));
                item.Copyright = new TextSyndicationContent("Copyright RealDimensions Software LLC and original author(s).");

                items.Add(item);
            }

            try
            {
                var mostRecent = posts.FirstOrDefault();
                feed.LastUpdatedTime = mostRecent == null ? DateTime.UtcNow : mostRecent.Published.GetValueOrDefault();
            }
            catch (Exception)
            {
                feed.LastUpdatedTime = DateTime.UtcNow;
            }

            feed.Items = items;

            return new RSSActionResult
            {
                Feed = feed
            };

            //remove a10 - http://stackoverflow.com/a/15971300/18475
        }

        private static string EnsureTrailingSlash(string siteRoot)
        {
            if (!siteRoot.EndsWith("/", StringComparison.Ordinal)) siteRoot = siteRoot + '/';
            return siteRoot;
        }

        [HttpGet, OutputCache(VaryByParam = "*", Location = OutputCacheLocation.Any, Duration = 7200)]
        public ActionResult Article(string articleName)
        {
            var articleNameNoHyphens = articleName.Replace("-", "");
            var filePath = Server.MapPath("~/Views/Blog/{0}.md".format_with(articleNameNoHyphens));

            if (_fileSystem.FileExists(filePath))
            {
                return View("~/Views/Pages/MarkdownPostArticle.cshtml", "~/Views/Blog/_Layout.cshtml", GetPost(filePath, articleName));
            }
            else
            {
                // check by urls

                var post = GetPostsByMostRecentFirst().FirstOrDefault(p => p.UrlPath.Equals(articleName, StringComparison.OrdinalIgnoreCase));
                if (post != null) return View("~/Views/Pages/MarkdownPostArticle.cshtml", "~/Views/Blog/_Layout.cshtml", post);
            }

            return RedirectToAction("Index");
        }

        private IEnumerable<MarkdownPostViewModel> GetPostsByMostRecentFirst()
        {
            IList<MarkdownPostViewModel> posts = new List<MarkdownPostViewModel>();

            var postsDirectory = Server.MapPath("~/Views/Blog/");
            var postFiles = Directory.GetFiles(postsDirectory, "*.md", SearchOption.TopDirectoryOnly);
            foreach (var postFile in postFiles)
            {
                posts.Add(GetPost(postFile));
            }

            return posts.OrderByDescending(p => p.Published).ToList();
        }

        private MarkdownPostViewModel GetPost(string filePath, string articleName = null)
        {
            var model = new MarkdownPostViewModel();
            if (_fileSystem.FileExists(filePath))
            {
                var contents = string.Empty;
                using (var fileStream = System.IO.File.Open(filePath, FileMode.OpenOrCreate, FileAccess.Read, FileShare.Read))
                using (var streamReader = new StreamReader(fileStream, Encoding.UTF8))
                {
                    contents = streamReader.ReadToEnd();
                }

                model.UrlPath = GetPostMetadataValue("Url", contents);
                if (string.IsNullOrEmpty(model.UrlPath)) model.UrlPath = GetUrl(filePath, articleName);
                model.Title = GetPostMetadataValue("Title", contents);
                model.Author = GetPostMetadataValue("Author", contents);
                model.Keywords = GetPostMetadataValue("Keywords", contents);
                model.Summary = GetPostMetadataValue("Summary", contents);
                model.Tags = GetPostMetadataValue("Tags", contents);
                model.Published = DateTime.ParseExact(GetPostMetadataValue("Published", contents), "yyyyMMdd", CultureInfo.InvariantCulture);
                model.Post = Markdown.ToHtml(contents.Remove(0, contents.IndexOf("---") + 3), MarkdownPipeline);
                model.Image = Markdown.ToHtml(GetPostMetadataValue("Image", contents), MarkdownPipeline);
            }

            return model;
        }

        private string GetUrl(string filePath, string articleName = null)
        {
            if (!string.IsNullOrWhiteSpace(articleName)) return articleName;
            if (string.IsNullOrWhiteSpace(filePath)) return filePath;

            var fileName = Path.GetFileNameWithoutExtension(filePath);
            var hyphenatedValue = new StringBuilder();

            Char previousChar = '^';
            foreach (var valueChar in fileName)
            {
                if (Char.IsUpper(valueChar) && hyphenatedValue.Length != 0)
                {
                    hyphenatedValue.Append("-");
                }

                if (Char.IsDigit(valueChar) && !Char.IsDigit(previousChar) && hyphenatedValue.Length != 0)
                {
                    hyphenatedValue.Append("-");
                }

                previousChar = valueChar;
                hyphenatedValue.Append(valueChar.to_string());
            }

            return hyphenatedValue.to_string().to_lower();
        }

        private string GetPostMetadataValue(string name, string contents)
        {
            var regex = new Regex(@"(?:^{0}\s*:\s*)([^\r\n]*)(?>\s*\r?$)".format_with(name), RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.IgnoreCase | RegexOptions.Multiline);

            var match = regex.Match(contents);
            return match.Groups[1].Value;
        }

    }
}
