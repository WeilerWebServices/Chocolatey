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
using chocolatey.package.verifier.infrastructure.app.configuration;
using chocolatey.package.verifier.infrastructure.filesystem;

namespace chocolatey.package.verifier.infrastructure.app.services
{
    public class FileSystemFileStorageService : IFileStorageService
    {
        private readonly IConfigurationSettings configuration;
        private readonly IFileSystem fileSystemSvc;

        public FileSystemFileStorageService(IConfigurationSettings configuration, IFileSystem fileSystemSvc)
        {
            this.configuration = configuration;
            this.fileSystemSvc = fileSystemSvc;
        }

        private static string build_path(string imagesFolder, string folderName, string fileName)
        {
            return Path.Combine(imagesFolder, folderName, fileName);
        }

        public void delete_file(string folderName, string fileName)
        {
            if (String.IsNullOrWhiteSpace(folderName)) throw new ArgumentNullException("folderName");
            if (String.IsNullOrWhiteSpace(fileName)) throw new ArgumentNullException("fileName");

            var path = build_path(configuration.ImagesFolder, folderName, fileName);
            if (fileSystemSvc.file_exists(path)) fileSystemSvc.delete_file(path);
        }

        public bool file_exists(string folderName, string fileName)
        {
            if (String.IsNullOrWhiteSpace(folderName)) throw new ArgumentNullException("folderName");
            if (String.IsNullOrWhiteSpace(fileName)) throw new ArgumentNullException("fileName");

            var path = build_path(configuration.ImagesFolder, folderName, fileName);
            return fileSystemSvc.file_exists(path);
        }

        public Stream get_file(string folderName, string fileName)
        {
            if (String.IsNullOrWhiteSpace(folderName)) throw new ArgumentNullException("folderName");
            if (String.IsNullOrWhiteSpace(fileName)) throw new ArgumentNullException("fileName");

            var path = build_path(configuration.ImagesFolder, folderName, fileName);
            if (fileSystemSvc.file_exists(path)) return fileSystemSvc.open_file_readonly(path);
            else return null;
        }

        public Stream get_file(string folderName, string fileName, bool useCache)
        {
            return get_file(folderName, fileName);
        }

        public void save_file(string folderName, string fileName, Stream fileStream)
        {
            if (String.IsNullOrWhiteSpace(folderName)) throw new ArgumentNullException("folderName");
            if (String.IsNullOrWhiteSpace(fileName)) throw new ArgumentNullException("fileName");
            if (fileStream == null) throw new ArgumentNullException("fileStream");

            if (!fileSystemSvc.directory_exists(configuration.ImagesFolder)) fileSystemSvc.create_directory(configuration.ImagesFolder);

            var folderPath = Path.Combine(configuration.ImagesFolder, folderName);
            if (!fileSystemSvc.directory_exists(folderPath)) fileSystemSvc.create_directory(folderPath);

            var filePath = build_path(configuration.ImagesFolder, folderName, fileName);
            if (fileSystemSvc.file_exists(filePath)) fileSystemSvc.delete_file(filePath);

            fileSystemSvc.write_file(filePath, () => fileStream);
        }

        public string get_url(string folderName, string fileName)
        {
            if (String.IsNullOrWhiteSpace(folderName)) throw new ArgumentNullException("folderName");
            if (String.IsNullOrWhiteSpace(fileName)) throw new ArgumentNullException("fileName");

            folderName = (string.IsNullOrEmpty(folderName) ? String.Empty : folderName.Substring(folderName.Length - 1, 1) == "/" ? folderName : folderName + "/");
            if (!string.IsNullOrWhiteSpace(configuration.ImagesUrl))
            {
                return string.Format("{0}/{1}{2}", configuration.ImagesUrl, folderName, fileName);
            }

            return "";
        }
    }
}
