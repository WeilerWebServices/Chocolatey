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

namespace chocolatey.package.verifier.infrastructure.app.configuration
{
    using System.Collections.Generic;
    using filesystem;

    public interface IConfigurationSettings
    {

        /// <summary>
        /// Gets the name of the service instance. This allows multiple instances to be installed.
        /// </summary>
        /// <value>
        /// The name of the instance.
        /// </value>
        string InstanceName { get; }

        /// <summary>
        ///   Gets the system email address.
        /// </summary>
        string SystemEmailAddress { get; }

        /// <summary>
        ///   Gets a value indicating whether this instance is in debug mode.
        /// </summary>
        /// <value>
        ///   <c>true</c> if this instance is debug mode; otherwise, <c>false</c>.
        /// </value>
        bool IsDebugMode { get; }
        
        /// <summary>
        ///   Gets the files path.
        /// </summary>
        string FilesPath { get; }
        
        /// <summary>
        ///   Gets an email to use as an override instead of the provided email. If null, use the provided email.
        /// </summary>
        string TestEmailOverride { get; }

        /// <summary>
        ///   Gets the number of seconds for a command to run before timing out.
        /// </summary>
        int CommandExecutionTimeoutSeconds { get; }

        /// <summary>
        /// Gets the path to vagrant. Used for isolating different vagrants.
        /// </summary>
        /// <value>
        /// The path to vagrant.
        /// </value>
        string PathToVagrant { get; }

        /// <summary>
        ///   The url used for testing packages and submitting results.
        /// </summary>
        string PackagesUrl { get; }

        /// <summary>
        /// Gets the package types to verify - if submitted it checks submitted packages. Otherwise checks approved packages
        /// </summary>
        /// <value>
        /// The package types to verify.
        /// </value>
        string PackageTypesToVerify { get; }

        /// <summary>
        ///   The api key used for submitting test results to the PackagesUrl.
        /// </summary>
        string PackagesApiKey { get; }

        /// <summary>
        ///   Gets the Token for accessing GitHub.
        /// </summary>
        string GitHubToken { get; }

        /// <summary>
        ///   Gets the UserName for accessing GitHub.
        /// </summary>
        string GitHubUserName { get; }

        /// <summary>
        ///   Gets the Password for accessing GitHub.
        /// </summary>
        string GitHubPassword { get; }

        /// <summary>
        /// Gets the path to virtual box.
        /// </summary>
        /// <value>
        /// The path to virtual box.
        /// </value>
        string PathToVirtualBox { get; }
        
        /// <summary>
        /// Gets the vbox identifier file path. Used to pull out the current id for the vbox in use.
        /// </summary>
        /// <value>
        /// The vbox identifier file path.
        /// </value>
        string VboxIdPath { get;}

        /// <summary>
        /// Gets the S3 Bucket name
        /// </summary>
        /// <value>
        /// The S3 Bucket name
        /// </value>
        string S3Bucket { get; }

        /// <summary>
        /// Gets the local folder where images will be stored.
        /// </summary>
        /// <value>
        /// The local ImagesFolder directory name
        /// </value>
        string ImagesFolder { get; }

        /// <summary>
        /// Gets the folder where images will be uploaded.
        /// </summary>
        /// <value>
        /// The ImagesUploadFolder directory name
        /// </value>
        string ImagesUploadFolder { get; }

        /// <summary>
        /// Gets the friendly URL for the images folder.
        /// </summary>
        /// <value>
        /// The friendly URL to the images
        /// </value>
        string ImagesUrl { get; }

        /// <summary>
        /// Gets the S3 Access Key
        /// </summary>
        /// <value>
        /// The Amazon S3 access key
        /// </value>
        string S3AccessKey { get; }

        /// <summary>
        /// Gets the S3 Secret Key
        /// </summary>
        /// <value>
        /// The Amazon S3 secret key
        /// </value>
        string S3SecretKey { get; }

        /// <summary>
        /// Get the type of images store
        /// </summary>
        /// <value>
        /// The Images Store type you want.
        /// </value>
        ImagesStoreType ImagesStoreType { get; }
    }
}
