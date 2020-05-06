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
    public interface IImageUploadService
    {
        /// <summary>
        /// Uploads an image - returns the link to the url
        /// </summary>
        /// <param name="imageLocation">The full path to the image to upload</param>
        /// <returns>If successful, the link returned will be a direct link to the url. Otherwise will be empty</returns>
        string upload_image(string imageLocation);

    }
}
