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
using System.Web.Mvc;

namespace NuGetGallery
{
    public class FileSystemFileStorageService : IFileStorageService
    {
        private readonly IConfiguration configuration;
        private readonly IFileSystemService fileSystemSvc;

        public FileSystemFileStorageService(
            IConfiguration configuration,
            IFileSystemService fileSystemSvc)
        {
            this.configuration = configuration;
            this.fileSystemSvc = fileSystemSvc;
        }

        private static string BuildPath(
            string fileStorageDirectory,
            string folderName,
            string fileName)
        {
            return Path.Combine(fileStorageDirectory, folderName, fileName);
        }

        public ActionResult CreateDownloadFileActionResult(
            string folderName,
            string fileName,
            bool useCache)
        {
            if (String.IsNullOrWhiteSpace(folderName)) throw new ArgumentNullException("folderName");
            if (String.IsNullOrWhiteSpace(fileName)) throw new ArgumentNullException("fileName");

            var path = BuildPath(configuration.FileStorageDirectory, folderName, fileName);
            if (!fileSystemSvc.FileExists(path)) return new HttpNotFoundResult();

            var result = new FilePathResult(path, GetContentType(folderName));
            result.FileDownloadName = new FileInfo(fileName).Name;
            return result;
        }

        private static string GetContentType(string folderName)
        {
            switch (folderName)
            {
                case Constants.PackagesFolderName :
                    return Constants.PackageContentType;
                case Constants.DownloadsFolderName :
                    return Constants.OctetStreamContentType;
                default :
                    throw new InvalidOperationException(
                        String.Format(CultureInfo.CurrentCulture, "The folder name {0} is not supported.", folderName));
            }
        }

        public void DeleteFile(
            string folderName,
            string fileName)
        {
            if (String.IsNullOrWhiteSpace(folderName)) throw new ArgumentNullException("folderName");
            if (String.IsNullOrWhiteSpace(fileName)) throw new ArgumentNullException("fileName");

            var path = BuildPath(configuration.FileStorageDirectory, folderName, fileName);
            if (fileSystemSvc.FileExists(path)) fileSystemSvc.DeleteFile(path);
        }

        public bool FileExists(
            string folderName,
            string fileName)
        {
            if (String.IsNullOrWhiteSpace(folderName)) throw new ArgumentNullException("folderName");
            if (String.IsNullOrWhiteSpace(fileName)) throw new ArgumentNullException("fileName");

            var path = BuildPath(configuration.FileStorageDirectory, folderName, fileName);
            return fileSystemSvc.FileExists(path);
        }

        public Stream GetFile(
            string folderName,
            string fileName)
        {
            if (String.IsNullOrWhiteSpace(folderName)) throw new ArgumentNullException("folderName");
            if (String.IsNullOrWhiteSpace(fileName)) throw new ArgumentNullException("fileName");

            var path = BuildPath(configuration.FileStorageDirectory, folderName, fileName);
            if (fileSystemSvc.FileExists(path)) return fileSystemSvc.OpenRead(path);
            else return null;
        }

        public Stream GetFile(string folderName, string fileName, bool useCache)
        {
            return GetFile(folderName, fileName);
        }

        public void SaveFile(
            string folderName,
            string fileName,
            Stream packageFile)
        {
            if (String.IsNullOrWhiteSpace(folderName)) throw new ArgumentNullException("folderName");
            if (String.IsNullOrWhiteSpace(fileName)) throw new ArgumentNullException("fileName");
            if (packageFile == null) throw new ArgumentNullException("packageFile");

            if (!fileSystemSvc.DirectoryExists(configuration.FileStorageDirectory)) fileSystemSvc.CreateDirectory(configuration.FileStorageDirectory);

            var folderPath = Path.Combine(configuration.FileStorageDirectory, folderName);
            if (!fileSystemSvc.DirectoryExists(folderPath)) fileSystemSvc.CreateDirectory(folderPath);

            var filePath = BuildPath(configuration.FileStorageDirectory, folderName, fileName);
            if (fileSystemSvc.FileExists(filePath)) fileSystemSvc.DeleteFile(filePath);

            using (var file = fileSystemSvc.OpenWrite(filePath))
            {
                packageFile.CopyTo(file);
            }
        }
    }
}
