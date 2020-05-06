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

namespace chocolatey.package.verifier.tests.integration.infastructure.app.services
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Threading.Tasks;
    using domain;
    using Should;
    using verifier.infrastructure.app.configuration;
    using verifier.infrastructure.app.services;
    using verifier.infrastructure.filesystem;

    public class GistServiceSpecs
    {
        public abstract class GistServiceSpecsBase : TinySpec
        {
            protected GistService service;
            protected IConfigurationSettings configurationSettings = new ConfigurationSettings();

            public override void Context()
            {
                service = new GistService(configurationSettings, new DotNetFileSystem());
            }
        }

        public class when_GistService_is_creating_a_gist : GistServiceSpecsBase
        {
            private Uri result;
            private readonly IList<PackageTestLog> logs = new List<PackageTestLog>();
            private readonly string description = "apacheds v2.0.0.20 - Passed - Package Tests Results";
            private Func<Task<Uri>> because;

            public override void Context()
            {
                base.Context();

                var gistFilesLocation = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "context", "gist");

                //grabs logs
                foreach (var file in Directory.GetFiles(gistFilesLocation))
                {
                    logs.Add(new PackageTestLog(Path.GetFileName(file), File.ReadAllText(file)));
                }
            }

            //NOTE: in the app.config, please insert credentials.
            public override void Because()
            {
                //because = () => service.create_gist(description, true, logs);
                Task<Uri> task = service.create_gist(description, true, logs);
                task.Wait(30000);
                result = task.Result;

                Console.WriteLine(result);
            }

            [Fact]
            public void should_not_error()
            {
                // nothing to do here
            }

            [Fact]
            public void should_return_a_valid_url()
            {
                result.ShouldNotBeNull();
            }
        }
    }
}
