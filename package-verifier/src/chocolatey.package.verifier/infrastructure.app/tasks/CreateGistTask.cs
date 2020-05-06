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
    using infrastructure.messaging;
    using infrastructure.tasks;
    using messaging;
    using registration;
    using services;

    public class CreateGistTask : ITask
    {
        private IDisposable _subscription;
        private readonly IGistService _gistService;

        public CreateGistTask(IGistService gistService)
        {
            _gistService = gistService;
        }

        public void initialize()
        {
            _subscription = EventManager.subscribe<PackageTestResultMessage>(create_gist, null, null);
            this.Log().Info(() => "{0} is now ready and waiting for PackageTestResultMessage".format_with(GetType().Name));
        }

        public void shutdown()
        {
            if (_subscription != null) _subscription.Dispose();
        }

        private async void create_gist(PackageTestResultMessage message)
        {
            this.Log().Info(
                () => "Creating gist for Package: {0} Version: {1}. Result: {2}".format_with(message.PackageId, message.PackageVersion, message.Success ? "Pass" : "Fail"));

            var gistDescription = "{0} v{1} - {2} - Package Tests Results".format_with(
                message.PackageId,
                message.PackageVersion,
                message.Success ? "Passed" : "Failed");

            try
            {
                var createdGistUrl = await _gistService.create_gist(gistDescription, isPublic: true, logs: message.Logs); //.ConfigureAwait(continueOnCapturedContext:false);

                EventManager.publish(new FinalPackageTestResultMessage(message.PackageId, message.PackageVersion, createdGistUrl.ToString(), message.Success));
            }
            catch (Exception ex)
            {
                Bootstrap.handle_exception(
                    new ApplicationException(
                        "Error creating Gist for {0} v{1}, likely because Gists API is sometimes janky and throws more temper tantrums than a 2 year old. The service will try to test the package again later. Until then enjoy this error log heartbeat. The service is still running, yay!"
                            .format_with(message.PackageId, message.PackageVersion),
                        ex));
            }
        }
    }
}
