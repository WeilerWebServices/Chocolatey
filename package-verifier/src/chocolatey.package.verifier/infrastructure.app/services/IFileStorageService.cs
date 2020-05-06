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

using System.IO;

namespace chocolatey.package.verifier.infrastructure.app.services
{
    public interface IFileStorageService
    {
        void delete_file(string folderName, string fileName);

        bool file_exists(string folderName, string fileName);      
        
        Stream get_file(string folderName, string fileName, bool useCache);

        void save_file(string folderName, string fileName, Stream fileStream);

        string get_url(string folderName, string fileName);
    }
}