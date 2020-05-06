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

namespace chocolatey.package.verifier.host.infrastructure.registration
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using SimpleInjector;
    using verifier.infrastructure.app.configuration;
    using verifier.infrastructure.app.services;
    using verifier.infrastructure.app.tasks;
    using verifier.infrastructure.configuration;
    using verifier.infrastructure.filesystem;
    using verifier.infrastructure.tasks;

    /// <summary>
    ///   The inversion container binding for the application.
    ///   This is client project specific - contains items that are only available in the client project.
    ///   Look for the broader application container in the core project.
    /// </summary>
    public class ContainerBindingConsole
    {
        /// <summary>
        ///   Loads the module into the kernel.
        /// </summary>
        /// <param name="container">The container.</param>
        public void register_components(Container container)
        {
            var config = Config.get_configuration_settings();
            var packagesCheckTask = new CheckForPackagesTask(config);
            if (config.PackageTypesToVerify.to_lower() != "submitted")
            {
                var thirtyDaysAgo = DateTime.UtcNow.AddDays(-30);
                packagesCheckTask.ServiceEndpoint = "/api/v2/";

                // we should not have any existing packages that have not been tested
                //pv.PackageTestResultStatus == null
                //todo: We are going to rerun all existing packages, even failing for now - after this run is complete, we will only rerun passing
                // we are doing this because we've completed some major fixes to the verifier that may fix some of the failing packages.
                //&& (pv.PackageTestResultStatus == "Passing" && pv.PackageTestResultStatusDate < thirtyDaysAgo)

                packagesCheckTask.AdditionalPackageSelectionFilters = p => p.Where(
                    pv => pv.IsLatestVersion
                          && (pv.PackageTestResultStatusDate == null || pv.PackageTestResultStatusDate < thirtyDaysAgo || pv.PackageTestResultStatus == "Pending")
                          && pv.PackageTestResultStatus != "Exempted"
                    );
            }
            else
            {
                packagesCheckTask.AdditionalPackageSelectionFilters = p => p.Where(
                    pv => (pv.PackageTestResultStatus == null 
                           || pv.PackageTestResultStatus == "Pending" 
                           || pv.PackageTestResultStatus == "Unknown")
                          );
            }

            container.Register<IEnumerable<ITask>>(
                () =>
                {
                    var list = new List<ITask>
                    {
                        new StartupTask(),
                        packagesCheckTask,
                        new TestPackageTask(container.GetInstance<IPackageTestService>(),container.GetInstance<IFileSystem>(),container.GetInstance<IConfigurationSettings>(), container.GetInstance<IImageUploadService>()),
                        new CreateGistTask(container.GetInstance<IGistService>()),
                        new UpdateWebsiteInformationTask(container.GetInstance<IConfigurationSettings>(),container.GetInstance<INuGetService>())
                    };

                    return list.AsReadOnly();
                },
                Lifestyle.Singleton);
        }
    }
}
