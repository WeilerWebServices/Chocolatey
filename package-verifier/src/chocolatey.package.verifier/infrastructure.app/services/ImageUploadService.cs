// Copyright © 2015 - Present RealDimensions Software, LLC
// 
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// 
// You may obtain a copy of the License at
// 
// 	http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

namespace chocolatey.package.verifier.infrastructure.app.services
{
    using configuration;
    using filesystem;
    using System;

    public class ImageUploadService : IImageUploadService
    {
        private readonly IConfigurationSettings _configuration;
        private readonly IFileSystem _fileSystem;
        private readonly IFileStorageService _imageStorageService;


        public ImageUploadService(
            IConfigurationSettings configuration,
            IFileSystem fileSystem,
            IFileStorageService imageStorageService)
        {
            _configuration = configuration;
            _fileSystem = fileSystem;
            _imageStorageService = imageStorageService;
        }

        public string upload_image(string imageLocation)
        {
            var imageLink = string.Empty;

            try
            {
                var imageStream = _fileSystem.open_file_readonly(imageLocation);
                var fileName = _fileSystem.get_file_name(imageLocation);

                _imageStorageService.save_file(_configuration.ImagesUploadFolder, fileName, imageStream);

                imageLink = _imageStorageService.get_url(_configuration.ImagesUploadFolder, fileName);
            }
            catch (Exception ex)
            {
                this.Log().Warn("Upload failed for image:{0} {1}".format_with(Environment.NewLine, ex.Message));
            }

            return imageLink;
        }
    }
}
