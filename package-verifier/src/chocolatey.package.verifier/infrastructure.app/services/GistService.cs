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
    using System;
    using System.Collections.Generic;
    using System.Text;
    using System.Threading.Tasks;
    using configuration;
    using domain;
    using filesystem;
    using Octokit;

    public class GistService : IGistService
    {
        private readonly IConfigurationSettings _configuration;
        private readonly IFileSystem _fileSystem;

        public GistService(IConfigurationSettings configuration, IFileSystem fileSystem)
        {
            _configuration = configuration;
            _fileSystem = fileSystem;
        }

        public async Task<Uri> create_gist(string description, bool isPublic, IList<PackageTestLog> logs)
        {
            this.Log().Debug(() => "Creating gist with description '{0}'.".format_with(description.escape_curly_braces()));

            var gitHubClient = this.create_git_hub_client();

            var gist = new NewGist
            {
                Description = description,
                Public = isPublic
            };

            foreach (var log in logs)
            {
                gist.Files.Add(log.Name, log.Contents);
            }

            debug_gist(gist);

            var createdGist = await gitHubClient.Gist.Create(gist); //.ConfigureAwait(continueOnCapturedContext:false);

            return new Uri(createdGist.HtmlUrl);
        }

        private void debug_gist(NewGist gist)
        {
            if (!_configuration.IsDebugMode) return;

            var gistFilesLocation = _fileSystem.combine_paths(_fileSystem.get_temp_path(), ApplicationParameters.Name, "Gist_" + DateTime.Now.ToString("yyyyMMdd_HHmmss_ffff"));
            this.Log().Info(() => "Generating gist files for gist '{0}' at '{1}'.".format_with(gist.Description.escape_curly_braces(), gistFilesLocation));
            _fileSystem.create_directory_if_not_exists(gistFilesLocation);
            _fileSystem.write_file(_fileSystem.combine_paths(gistFilesLocation, "description.txt"), gist.Description, Encoding.UTF8);

            foreach (var file in gist.Files)
            {
                _fileSystem.write_file(_fileSystem.combine_paths(gistFilesLocation, file.Key), file.Value, Encoding.UTF8);
            }
        }

        private GitHubClient create_git_hub_client()
        {
            // assume that these values will be correctly set
            Credentials credentials;
            if (!string.IsNullOrWhiteSpace(_configuration.GitHubToken)) credentials = new Credentials(_configuration.GitHubToken);
            else credentials = new Credentials(_configuration.GitHubUserName, _configuration.GitHubPassword);

            var gitHubClient = new GitHubClient(new ProductHeaderValue("ChocolateyPackageVerifier"))
            {
                Credentials = credentials
            };
            return gitHubClient;
        }
    }
}
