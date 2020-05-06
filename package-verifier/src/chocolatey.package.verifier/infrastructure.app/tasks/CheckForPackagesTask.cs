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

namespace chocolatey.package.verifier.infrastructure.app.tasks
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Timers;
    using configuration;
    using infrastructure.messaging;
    using infrastructure.tasks;
    using messaging;
    using NuGetGallery;
    using registration;
    using services;

    public class CheckForPackagesTask : ITask
    {
        private readonly IConfigurationSettings _configurationSettings;
        private const double TIMER_INTERVAL = 600000;
        private string _serviceEndpoint = "/api/v2/submitted/";
        private readonly Timer _timer = new Timer();
        private IDisposable _subscription;

        public string ServiceEndpoint { get { return _serviceEndpoint; } set { _serviceEndpoint = value; } }

        public Func<IQueryable<V2FeedPackage>, IQueryable<V2FeedPackage>> AdditionalPackageSelectionFilters { get; set; }

        public CheckForPackagesTask(IConfigurationSettings configurationSettings)
        {
            _configurationSettings = configurationSettings;
        }

        public void initialize()
        {
            _subscription = EventManager.subscribe<StartupMessage>((message) => timer_elapsed(null, null), null, null);
            _timer.Interval = TIMER_INTERVAL;
            _timer.Elapsed += timer_elapsed;
            _timer.Start();
            this.Log().Info(() => "{0} will check for packages to verify every {1} minutes".format_with(GetType().Name, TIMER_INTERVAL / 60000));
        }

        public void shutdown()
        {
            if (_subscription != null) _subscription.Dispose();

            if (_timer != null)
            {
                _timer.Stop();
                _timer.Dispose();
            }
        }

        private void timer_elapsed(object sender, ElapsedEventArgs e)
        {
            _timer.Stop();

            this.Log().Info(() => "Checking for packages to verify.");

            try
            {
                var submittedPackagesUri = NuGetService.get_service_endpoint_url(_configurationSettings.PackagesUrl, ServiceEndpoint);

                var lastPackage = string.Empty;

                //for (var i = 0; i < 10; i++)
                //{
                this.Log().Info(() => "Grabbing next available package for verification.");

                var service = new FeedContext_x0060_1(submittedPackagesUri)
                {
                    Timeout = 70
                };

                var cacheTimeout = DateTime.UtcNow.AddMinutes(-31);
                // this only returns 40 results at a time but at least we'll have something to start with
                IQueryable<V2FeedPackage> packageQuery =
                    service.Packages.Where(p => p.Created < cacheTimeout);

                if (AdditionalPackageSelectionFilters != null) packageQuery = AdditionalPackageSelectionFilters.Invoke(packageQuery);

                //int total = packageQuery.Count();
                //packageQuery = packageQuery.Where(p => p.Id == "");

                packageQuery = packageQuery.OrderBy(p => p.Created);

                // let's specifically reduce the call to 30 results so we get back results faster from Chocolatey.org
                IList<V2FeedPackage> packagesToValidate = packageQuery.Take(30).ToList();

                if (packagesToValidate.Count == 0)
                {
                    this.Log().Info("No packages to verify.");
                    //break;
                }

                //remove the random for now, we'll just grab the top result
                //this.Log().Info("Pulled in {0} packages, picking one at random to verify.".format_with(packagesToValidate.Count));

                //var skip = new Random().Next(packagesToValidate.Count);
                //if (skip < 0) skip = 0;
                //packagesToValidate = packagesToValidate.Skip(skip).Take(1).ToList();

                //var package = packagesToValidate.FirstOrDefault();
                //if (package == null) continue;

                //var packageId = "{0}:{1}".format_with(package.Id, package.Version);
                //if (lastPackage == packageId)
                //{
                //    this.Log().Info("Already verified {0} v{1} recently.".format_with(package.Title, package.Version));
                //    continue;
                //}
                //lastPackage = packageId;

                //this.Log().Info("{0} v{1} found for review.".format_with(package.Title, package.Version));
                //EventManager.publish(new VerifyPackageMessage(package.Id, package.Version));

                foreach (var package in packagesToValidate.or_empty_list_if_null())
                {
                    this.Log().Info("{0} v{1} found for review.".format_with(package.Title, package.Version));
                    EventManager.publish(new VerifyPackageMessage(package.Id, package.Version));
                }
                // }
            }
            catch (Exception ex)
            {
                Bootstrap.handle_exception(ex);
            }

            this.Log().Info(() => "Finished checking for packages to verify. Sleeping for {0} minutes.".format_with(TIMER_INTERVAL / 60000));

            _timer.Start();
        }
    }
}
