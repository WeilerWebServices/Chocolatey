// Copyright � 2015 - Present RealDimensions Software, LLC
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

namespace chocolatey.package.validator.infrastructure.services
{
    using app;

    /// <summary>
    ///   The file serving service
    /// </summary>
    public interface IFileServingService
    {
        /// <summary>
        ///   Gets the file information.
        /// </summary>
        /// <param name="fileName">Name of the file.</param>
        /// <param name="fileType">Type of the file.</param>
        /// <returns>The file information</returns>
        FileInformation get_file_information(string fileName, ApplicationFileType fileType);
    }
}
