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
using System.Collections.Specialized;
using System.IO;
using System.Net;
using System.Text;
using System.Web;
using ImageResizer;

namespace NuGetGallery
{
    public class ImageFileService : IImageFileService
    {
        private readonly IConfiguration _configuration;

        public ImageFileService(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        /// <summary>
        /// Gets the image.
        /// </summary>
        /// <param name="url">The URL.</param>
        /// <param name="packageId">The package id</param>
        /// <param name="version">The package version</param>
        /// <returns>The name of an image</returns>
        public string CacheAndGetImage(string url, string packageId, string version)
        {
            if (!_configuration.HostImages) return url;
            if (string.IsNullOrWhiteSpace(url)) return null;
            if (string.IsNullOrWhiteSpace(version)) version = "default";

            var imagesFolder = Path.GetFullPath(Path.Combine(HttpContext.Current.Server.MapPath("~/content/"), Constants.PackageImagesFolderName));

            var uri = new Uri(url);
            var originalExtension = Path.GetExtension(uri.AbsolutePath);
            if (string.IsNullOrEmpty(originalExtension))
            {
                originalExtension = Constants.ImageExtension;
            }
            
            var outputImage = GetOutputImagePath(imagesFolder, Path.DirectorySeparatorChar, packageId, version, originalExtension);

            if (!File.Exists(outputImage))
            {
                var originalImage = DownloadImage(url, imagesFolder, packageId, version);
                var instructions = GetImageConversionInstructions();
                outputImage = ConvertImage(originalImage, outputImage, instructions);
            }

            return outputImage;
        }

        public void DeleteCachedImage(string packageId, string version)
        {
            var imagesFolder = Path.GetFullPath(Path.Combine(HttpContext.Current.Server.MapPath("~/content/"), Constants.PackageImagesFolderName));

            var image = GetOutputImagePath(imagesFolder, Path.DirectorySeparatorChar, packageId, version, Constants.ImageExtension);
            if (File.Exists(image)) File.Delete(image);  
            
            image = GetOutputImagePath(imagesFolder, Path.DirectorySeparatorChar, packageId, version, ".svg");
            if (File.Exists(image)) File.Delete(image);
        }

        /// <summary>
        ///   Downloads the image.
        /// </summary>
        /// <param name="urlLocation">The URL location.</param>
        /// <param name="imagesFolder">The images folder.</param>
        /// <param name="packageId">The package id</param>
        /// <param name="version">The package version</param>
        /// <returns></returns>
        public string DownloadImage(string urlLocation, string imagesFolder,string packageId, string version)
        {
            var uri = new Uri(urlLocation);
            var fileName = Path.GetFileName(uri.AbsolutePath);

            try
            {
                var request = WebRequest.Create(urlLocation) as HttpWebRequest;
                if (request == null) return null;

                request.Method = WebRequestMethods.Http.Get;
                var response = request.GetResponse() as HttpWebResponse;

                string header = response.Headers["Content-Disposition"] ?? string.Empty;
                const string filename = "filename=";
                int index = header.LastIndexOf(filename, StringComparison.OrdinalIgnoreCase);

                if (index > -1) fileName = header.Substring(index + filename.Length).Replace("\"",string.Empty);
                var originalFilePath = Path.GetFullPath(Path.Combine(imagesFolder, "temp", string.Format("{0}.{1}.{2}", packageId, version, fileName)));

                var originalDirectory = Path.GetDirectoryName(originalFilePath);
                if (!Directory.Exists(originalDirectory)) Directory.CreateDirectory(originalDirectory);

                if (!File.Exists(originalFilePath))
                {
                    using (var client = new WebClient())
                    {
                        client.DownloadFile(new Uri(urlLocation), originalFilePath);
                    }
                }

                return originalFilePath;
            } catch (Exception)
            {
                return null;
            }
        }

        /// <summary>
        ///   Converts an image according to the instructions.
        /// </summary>
        /// <param name="originalImagePath">The original image path.</param>
        /// <param name="outImagePath">The out image path.</param>
        /// <param name="instructions">The instructions.</param>
        public string ConvertImage(string originalImagePath, string outImagePath, NameValueCollection instructions)
        {
            if (string.IsNullOrWhiteSpace(originalImagePath)) return null;

            originalImagePath = Path.GetFullPath(originalImagePath);
            if (!File.Exists(originalImagePath)) return null;

            var extension = Path.GetExtension(originalImagePath);
            if (extension == ".svg")
            {
                File.Copy(originalImagePath, outImagePath);
            } else
            {
                outImagePath = Path.GetFullPath(outImagePath);

                var outDirectory = Path.GetDirectoryName(outImagePath);
                if (!Directory.Exists(outDirectory)) Directory.CreateDirectory(outDirectory);

                var settings = new ResizeSettings();
                if (instructions != null) settings = new ResizeSettings(instructions);

                try
                {
                    ImageBuilder.Current.Build(originalImagePath, outImagePath, settings, disposeSource: false);
                }
                catch (Exception)
                {
                    // we end up here when the downloaded file isn't an image, such as when HTML redirects are at play.
                    return null;
                }
            }

            return outImagePath;
        }

        /// <summary>
        ///   Gets the output image path.
        /// </summary>
        /// <param name="imageOutputPath">The image output path.</param>
        /// <param name="pathSeparatorChar">The path separater char.</param>
        /// <param name="packageId">The package id</param>
        /// <param name="version">The package version</param>
        /// <param name="originalExtension">The extension, like .png</param>
        public string GetOutputImagePath(string imageOutputPath, char pathSeparatorChar, string packageId, string version, string originalExtension)
        {
            var outputImage = new StringBuilder();
            outputImage.Append(imageOutputPath);
            outputImage.Append(pathSeparatorChar);
            outputImage.Append(packageId);
            outputImage.Append("." + version);

            if (originalExtension == ".svg")
            {
                outputImage.Append(originalExtension);
            } else
            {
                outputImage.Append(Constants.ImageExtension);
            }

            return outputImage.ToString();
        }

        /// <summary>
        ///   Gets the image conversion instructions for formatting the specific image type
        /// </summary>
        /// <remarks>http://imageresizing.net/docs/reference</remarks>
        public NameValueCollection GetImageConversionInstructions()
        {
            var instructions = new NameValueCollection
            {
                { "format", Constants.ImageExtension.Replace(".", string.Empty) }
            };

            instructions.Add("quality", "90");
            instructions.Add("maxwidth", "128");
            instructions.Add("maxheight", "128");
            instructions.Add("zoom", "1.0");
            //instructions.Add("mode", "crop");

            return instructions;
        }
    }
}
