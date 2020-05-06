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
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Web.Mvc;
using System.Web.UI;
using System.Collections.Generic;
using Markdig;

namespace NuGetGallery.Controllers
{
    public class DocumentationController : Controller
    {
        private readonly IFileSystemService _fileSystem;
        public IConfiguration Configuration { get; set; }
        public MarkdownPipeline MarkdownPipeline { get; set; }

        public DocumentationController(IFileSystemService fileSystem, IConfiguration configuration)
        {
            _fileSystem = fileSystem;
            Configuration = configuration;

            MarkdownPipeline = new MarkdownPipelineBuilder()
                 .UseSoftlineBreakAsHardlineBreak()
                 .UseAutoLinks()
                 .UseGridTables()
                 .UsePipeTables()
                 .UseAutoIdentifiers()
                 .UseEmphasisExtras()
                 .UseNoFollowLinks()
                 .UseCustomContainers()
                 .UseBootstrap()
                 .UseEmojiAndSmiley()
                 .Build();
        }

        [HttpGet, OutputCache(VaryByParam = "*", Location = OutputCacheLocation.Any, Duration = 7200)]
        public ActionResult Documentation(string docName, string q)
        {
            var filePath = Server.MapPath("~/Views/Documentation/Files/{0}.md".format_with(docName));
            q = (q ?? string.Empty).Trim();
            ViewBag.SearchTerm = q;

            // Get title
            TextInfo textInfo = new CultureInfo("en-US", false).TextInfo;
            Uri uri = new Uri(Request.Url.GetLeftPart(UriPartial.Path));
            var title = uri.Segments.Last();
            ViewBag.Title = title
                .Replace(title, textInfo.ToTitleCase(title))
                .Replace("-", " ")
                .Replace("To ", "to ")
                .Replace("And ", "and ")
                .Replace("An ", "an ")
                .Replace("For ", "for ")
                .Replace("With ", "with ")
                .Replace("In ", "in ");

            if (_fileSystem.FileExists(filePath))
            {
                return View("~/Views/Documentation/Documentation.cshtml", "~/Views/Documentation/_Layout.cshtml", GetPost(filePath, docName));
            }
            else if (docName == "search")
            {
                return View("~/Views/Documentation/Search.cshtml", "~/Views/Documentation/_Layout.cshtml", GetSearch(docName, q));
            }
            else
            {
                var post = GetHyphenatedPosts().FirstOrDefault(p => p.UrlPath.Equals(docName, StringComparison.OrdinalIgnoreCase));
                if (post != null) return View("~/Views/Documentation/Documentation.cshtml", "~/Views/Documentation/_Layout.cshtml", post);
            }

            return RedirectToRoute(RouteName.Docs, new { docName = "home" });
        }

        private DocumentationSearchViewModel GetSearch(string docName, string q)
        {
            q = (q ?? string.Empty).Trim();
            ViewBag.SearchTerm = q;

            var model = new DocumentationSearchViewModel(q);
            
            return model;
        }

        private DocumentationViewModel GetPost(string filePath, string docName = null)
        {
            var model = new DocumentationViewModel();
            if (_fileSystem.FileExists(filePath))
            {
                var contents = string.Empty;
                using (var fileStream = System.IO.File.Open(filePath, FileMode.OpenOrCreate, FileAccess.Read, FileShare.Read))
                using (var streamReader = new StreamReader(fileStream, Encoding.UTF8))
                {
                    contents = streamReader.ReadToEnd();
                }

                var urlOnePattern = @"\[\[(.*?)\|(.*?)\]\]";
                MatchCollection urlOnePatternMatches = Regex.Matches(contents, urlOnePattern);
                foreach (Match match in urlOnePatternMatches)
                {
                    var hyphenatedValue = new StringBuilder();

                    Char previousChar = '^';
                    foreach (var valueChar in match.Groups[2].Value.ToString())
                    {
                        // Filenames that contain both a "-" and camel casing
                        if (match.Groups[2].Value.Contains("-") && Char.IsLower(previousChar) && Char.IsUpper(valueChar))
                        {
                            hyphenatedValue.Append("-");
                        }

                        if (Char.IsUpper(valueChar) && hyphenatedValue.Length != 0 && !Char.IsUpper(previousChar) && !match.Groups[2].Value.Contains("-"))
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

                    contents = contents.Replace(match.Value, "[" + match.Groups[1].Value + "](/docs/" + hyphenatedValue.ToString().ToLower() + ")");
                }

                var urlTwoPattern = @"\[(.*?)\]\((.*?)\)";
                MatchCollection urlTwoPatternMatches = Regex.Matches(contents, urlTwoPattern);
                foreach (Match match in urlTwoPatternMatches)
                {
                    contents = contents.Replace(match.Groups[2].Value, match.Groups[2].Value
                        .Replace("f-a-q", "faq")
                        .Replace("p-s1", "ps1")
                        .Replace("---", "-")
                        .Replace("--", "-"));
                }

                contents = contents
                    .Replace("<!--remove", "")
                    .Replace("remove-->", "")
                    .Replace("~~~sh", "~~~language-none")
                    .Replace("(images/", "(/content/images/docs/");

                // Get Url
                model.UrlPath = GetUrl(filePath, docName);

                // Convert using Markdig
                model.Post = Markdown.ToHtml(contents, MarkdownPipeline);

                // Remove "." from header ID's
                var headerPattern = @"(<h\d id=)(.*?)(>)";
                MatchCollection matches = Regex.Matches(model.Post, headerPattern);
                foreach (Match match in matches)
                {
                    model.Post = Regex.Replace(model.Post, match.Groups[2].Value, match.Groups[2].Value.Replace(".", ""));
                }
            }

            return model;
        }

        private IEnumerable<DocumentationViewModel> GetHyphenatedPosts()
        {
            IList<DocumentationViewModel> posts = new List<DocumentationViewModel>();

            var postsDirectory = Server.MapPath("~/Views/Documentation/Files");
            var postFiles = Directory.GetFiles(postsDirectory, "*.md", SearchOption.TopDirectoryOnly);
            foreach (var postFile in postFiles)
            {
                posts.Add(GetPost(postFile));
            }

            return posts.ToList();
        }

        private string GetUrl(string filePath, string docName = null)
        {
            if (!string.IsNullOrWhiteSpace(docName)) return docName;
            if (string.IsNullOrWhiteSpace(filePath)) return filePath;

            var fileName = Path.GetFileNameWithoutExtension(filePath);
            if (fileName.Equals("CommandsApiKey"))
            {
                fileName = fileName.Replace("CommandsApiKey", "CommandsApikey");
            }
            if (fileName.Equals("CommandsSetapiKey"))
            {
                fileName = fileName.Replace("CommandsSetapiKey", "CommandsSetapikey");
            }

            var hyphenatedValue = new StringBuilder();

            Char previousChar = '^';
            foreach (var valueChar in fileName)
            {
                // Filenames that contain both a "-" and camel casing
                if (fileName.Contains("-") && Char.IsLower(previousChar) && Char.IsUpper(valueChar))
                {
                    hyphenatedValue.Append("-");
                }

                if (Char.IsUpper(valueChar) && hyphenatedValue.Length != 0 && !Char.IsUpper(previousChar) && !fileName.Contains("-"))
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
    }
}