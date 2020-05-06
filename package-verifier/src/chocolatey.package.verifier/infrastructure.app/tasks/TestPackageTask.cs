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
    using System.Reflection;
    using System.Text;
    using System.Threading;
    using commands;
    using configuration;
    using domain;
    using filesystem;
    using infrastructure.messaging;
    using infrastructure.results;
    using infrastructure.tasks;
    using messaging;
    using registration;
    using results;
    using services;

    public class TestPackageTask : ITask
    {
        private readonly IPackageTestService _testService;
        private readonly IFileSystem _fileSystem;
        private readonly IConfigurationSettings _configuration;
        private readonly IImageUploadService _imageUploadService;
        private IDisposable _subscription;
        private readonly string _vboxManageExe;
        private const string PROC_LOCK_NAME = "proc_test";
        private const string _imageFormat = "{0}.{1}.{2}.{3}.png";
        private const string _dateTimeFormat = "yyyyMMddHHmmss";

        public TestPackageTask(IPackageTestService testService, IFileSystem fileSystem, IConfigurationSettings configuration, IImageUploadService imageUploadService)
        {
            _testService = testService;
            _fileSystem = fileSystem;
            _configuration = configuration;
            _imageUploadService = imageUploadService;

            if (!string.IsNullOrWhiteSpace(_configuration.PathToVirtualBox))
            {
                _vboxManageExe = _fileSystem.combine_paths(_configuration.PathToVirtualBox, "vboxmanage.exe");
            }
        }

        public void initialize()
        {
            _subscription = EventManager.subscribe<VerifyPackageMessage>(test_package, null, null);
            this.Log().Info(() => "{0} is now ready and waiting for VerifyPackageMessage".format_with(GetType().Name));
        }

        public void shutdown()
        {
            if (_subscription != null) _subscription.Dispose();
            _testService.shutdown();
        }


        private bool had_environment_errors(TestCommandOutputResult results)
        {
            var environmentErrors = false;

            if (results.Logs.Contains("The term 'choco.exe' is not")) environmentErrors = true;
            if (results.Logs.Contains("The term 'choco' is not")) environmentErrors = true;
            if (results.Logs.Contains("Cannot remove item C:\\Windows\\Temp\\WinRM_Elevated_Shell.log")) environmentErrors = true;

            if (environmentErrors)
            {
                _testService.destroy();
                Bootstrap.handle_exception(new ApplicationException("Unable to test package due to testing environment issues. See log for details"));
            }

            return environmentErrors;
        }

        private void test_package(VerifyPackageMessage message)
        {
            //var lockTaken = TransactionLock.acquire(VAGRANT_LOCK_NAME, 7200);
            //if (!lockTaken)
            //{
            //    Bootstrap.handle_exception(new ApplicationException("Testing package {0} v{1} timed out waiting on transaction lock to open".format_with(message.PackageId, message.PackageVersion)));
            //    return;
            //}

            try
            {
                this.Log().Info(() => "========== {0} v{1} ==========".format_with(message.PackageId, message.PackageVersion));
                this.Log().Info(() => "Testing Package: {0} Version: {1}".format_with(message.PackageId, message.PackageVersion));
                

                _fileSystem.delete_file(".\\choco_logs\\chocolatey.log");
                var prepSuccess = _testService.prep();
                var resetSuccess = _testService.reset();
                if (!prepSuccess || !resetSuccess)
                {
                    Bootstrap.handle_exception(new ApplicationException("Unable to test package due to testing service issues. See log for details"));
                    return;
                }

                this.Log().Info(() => "Checking install.");

                const string imageDirectory = ".\\images";
                _fileSystem.create_directory_if_not_exists(imageDirectory);
                var installImage = string.Empty;

                var installResults = _testService.run(
                    "choco.exe install {0} --version {1} -fdvy --execution-timeout={2} --allow-downgrade".format_with(
                        message.PackageId,
                        message.PackageVersion,
                        _configuration.CommandExecutionTimeoutSeconds),
                    () =>
                    {
                        this.Log().Info(() => "Timeout triggered.");
                        if (string.IsNullOrWhiteSpace(_vboxManageExe) || string.IsNullOrWhiteSpace(_configuration.VboxIdPath)) return;
                        if (!_fileSystem.file_exists(_configuration.VboxIdPath)) return;
                        var vmId = _fileSystem.read_file(_configuration.VboxIdPath);
                        if (string.IsNullOrWhiteSpace(vmId)) return;

                        var imageLocation = _fileSystem.combine_paths(imageDirectory, _imageFormat.format_with(
                            message.PackageId,
                            message.PackageVersion,
                            DateTime.Now.ToString(_dateTimeFormat),
                            "install"
                        ));

                        try
                        {
                            CommandExecutor.execute_static(_vboxManageExe, 
                                "controlvm {" + vmId + "} screenshotpng " + imageLocation, 
                                30,
                                _fileSystem.get_directory_name(Assembly.GetExecutingAssembly().Location),
                                (o, e) =>
                                {
                                    if (e == null || string.IsNullOrWhiteSpace(e.Data)) return;
                                    this.Log().Debug(() => " [VboxManage] {0}".format_with(e.Data));
                                },
                                (o, e) =>
                                {
                                    if (e == null || string.IsNullOrWhiteSpace(e.Data)) return;
                                    this.Log().Warn(() => " [VboxManage][Error] {0}".format_with(e.Data));
                                },
                                null, 
                                updateProcessPath: false,
                                allowUseWindow: false);

                            if (_fileSystem.file_exists(imageLocation))
                            {
                                installImage = _imageUploadService.upload_image(imageLocation);
                            }
                        }
                        catch (Exception ex)
                        {
                            this.Log().Warn("Image capture failed for {0}v{1}:{2} {3}".format_with(message.PackageId, message.PackageVersion, Environment.NewLine, ex.Message));
                        }
                    });

                installResults.ImageLink = installImage;

                if (had_environment_errors(installResults)) return;

                this.Log().Debug(() => "Grabbing actual log file to include in report.");
                var installLog = string.Empty;
                var installLogFile = ".\\choco_logs\\chocolatey.log";
                try
                {
                    if (_fileSystem.file_exists(installLogFile))
                    {
                        installLog = _fileSystem.read_file(installLogFile);
                        _fileSystem.delete_file(installLogFile);
                    }
                }
                catch (Exception ex)
                {
                    Bootstrap.handle_exception(new ApplicationException("Unable to read file '{0}':{1} {2}".format_with(installLogFile, Environment.NewLine, ex.ToString()), ex));
                }

                this.Log().Debug(() => "Grabbing results files (.registry/.files) to include in report.");
                var registrySnapshot = string.Empty;
                var registrySnapshotFile = ".\\files\\{0}.{1}\\.registry".format_with(message.PackageId, message.PackageVersion);
                try
                {
                    if (_fileSystem.file_exists(registrySnapshotFile)) registrySnapshot = _fileSystem.read_file(registrySnapshotFile);
                }
                catch (Exception ex)
                {
                    Bootstrap.handle_exception(new ApplicationException("Unable to read file '{0}':{1} {2}".format_with(registrySnapshotFile, Environment.NewLine, ex.ToString()), ex));
                }

                var filesSnapshot = string.Empty;
                var filesSnapshotFile = ".\\files\\{0}.{1}\\.files".format_with(message.PackageId, message.PackageVersion);
                try
                {
                    if (_fileSystem.file_exists(filesSnapshotFile)) filesSnapshot = _fileSystem.read_file(filesSnapshotFile);
                }
                catch (Exception ex)
                {
                    Bootstrap.handle_exception(new ApplicationException("Unable to read file '{0}':{1} {2}".format_with(filesSnapshotFile, Environment.NewLine, ex.ToString()), ex));
                }

                var success = installResults.Success && installResults.ExitCode == 0;
                this.Log().Info(() => "Install was '{0}'.".format_with(success ? "successful" : "not successful"));

                if (detect_vagrant_errors(installResults.Logs, message.PackageId, message.PackageVersion)) return;

                var upgradeResults = new TestCommandOutputResult();

                var uninstallLog = string.Empty;
                var uninstallResults = new TestCommandOutputResult();
                if (success)
                {
                    this.Log().Info(() => "Now checking uninstall.");

                    var uninstallImage = string.Empty;

                    uninstallResults = _testService.run("choco.exe uninstall {0} --version {1} -dvy --execution-timeout={2}".format_with(message.PackageId, message.PackageVersion, _configuration.CommandExecutionTimeoutSeconds),
                        () =>
                        {
                            if (string.IsNullOrWhiteSpace(_vboxManageExe) || string.IsNullOrWhiteSpace(_configuration.VboxIdPath)) return;
                            if (!_fileSystem.file_exists(_configuration.VboxIdPath)) return;
                            var vmId = _fileSystem.read_file(_configuration.VboxIdPath);
                            if (string.IsNullOrWhiteSpace(vmId)) return;

                            var imageLocation = _fileSystem.combine_paths(imageDirectory, _imageFormat.format_with(
                                message.PackageId,
                                message.PackageVersion,
                                DateTime.Now.ToString(_dateTimeFormat),
                                "uninstall"
                            ));

                            try
                            {
                                CommandExecutor.execute_static(_vboxManageExe,
                                    "controlvm {" + vmId + "} screenshotpng " + imageLocation,
                                    30,
                                    _fileSystem.get_directory_name(Assembly.GetExecutingAssembly().Location),
                                    (o, e) =>
                                    {
                                        if (e == null || string.IsNullOrWhiteSpace(e.Data)) return;
                                        this.Log().Debug(() => " [VboxManage] {0}".format_with(e.Data));
                                    },
                                    (o, e) =>
                                    {
                                        if (e == null || string.IsNullOrWhiteSpace(e.Data)) return;
                                        this.Log().Warn(() => " [VboxManage][Error] {0}".format_with(e.Data));
                                    },
                                    null,
                                    updateProcessPath: false,
                                    allowUseWindow: false);

                                if (_fileSystem.file_exists(imageLocation))
                                {
                                    uninstallImage = _imageUploadService.upload_image(imageLocation);
                                }
                            }
                            catch (Exception ex)
                            {
                                this.Log().Warn("Image capture failed for {0}v{1}:{2} {3}".format_with(message.PackageId, message.PackageVersion, Environment.NewLine, ex.Message));
                            }
                        });
                    this.Log().Info(() => "Uninstall was '{0}'.".format_with(uninstallResults.ExitCode == 0 ? "successful" : "not successful"));
                    this.Log().Debug(() => "Grabbing actual log file to include in report.");

                    uninstallResults.ImageLink = uninstallImage;

                    var uninstallLogFile = ".\\choco_logs\\chocolatey.log";
                    try
                    {
                        if (_fileSystem.file_exists(uninstallLogFile))
                        {
                            uninstallLog = _fileSystem.read_file(uninstallLogFile);
                            _fileSystem.delete_file(uninstallLogFile);
                        }
                    }
                    catch (Exception ex)
                    {
                        Bootstrap.handle_exception(new ApplicationException("Unable to read file '{0}':{1} {2}".format_with(uninstallLogFile, Environment.NewLine, ex.ToString()), ex));
                    }

                    if (had_environment_errors(uninstallResults)) return;
                    if (detect_vagrant_errors(uninstallResults.Logs, message.PackageId, message.PackageVersion)) return;
                }

                foreach (var subDirectory in _fileSystem.get_directories(".\\files").or_empty_list_if_null())
                {
                    try
                    {
                        _fileSystem.delete_directory_if_exists(subDirectory, recursive: true);
                    }
                    catch (Exception ex)
                    {
                        Bootstrap.handle_exception(new ApplicationException("Unable to cleanup files directory (where .chocolatey files are put):{0} {1}".format_with(Environment.NewLine, ex.ToString()), ex));
                    }
                }

                var logs = new List<PackageTestLog>();

                var summary = new StringBuilder();
                summary.AppendFormat("{0} v{1} - {2} - Package Test Results", message.PackageId, message.PackageVersion, success ? "Passed" : "Failed");
                summary.AppendFormat("{0} * [{1}packages/{2}/{3}]({1}packages/{2}/{3})", Environment.NewLine, _configuration.PackagesUrl.ensure_trailing_slash(), message.PackageId, message.PackageVersion);
                summary.AppendFormat("{0} * Tested {1} +00:00", Environment.NewLine, DateTime.UtcNow.ToString("dd MMM yyyy HH:mm:ss"));
                summary.AppendFormat("{0} * Tested against {1} ({2})", Environment.NewLine, "win2012r2x64", "Windows Server 2012 R2 x64");
                summary.AppendFormat("{0} * Tested with the latest version of choco, possibly a beta version.", Environment.NewLine);
                summary.AppendFormat(
                    "{0} * Tested with {1} service v{2}{3}",
                    Environment.NewLine,
                    ApplicationParameters.Name,
                    ApplicationParameters.ProductVersion,
                    string.IsNullOrWhiteSpace(_configuration.InstanceName) ? string.Empty : " (Instance: {0})".format_with(_configuration.InstanceName)
                    );
                summary.AppendFormat(
                    "{0} * Install {1}.",
                    Environment.NewLine,
                    installResults.ExitCode == 0
                        ? "was successful"
                        : "failed. Note that the process may have hung, indicating a not completely silent install. This is usually seen when the last entry in the log is calling the install. This can also happen when a window pops up and needs to be closed to continue");
                if (!string.IsNullOrWhiteSpace(upgradeResults.Logs))
                    summary.AppendFormat(
                        "{0} * Upgrade {1}.",
                        Environment.NewLine,
                        upgradeResults.ExitCode == 0
                            ? "was successful"
                            : "failed. Note that the process may have hung, indicating a not completely silent install. This is usually seen when the last entry in the log is calling the install. This can also happen when a window pops up and needs to be closed to continue");
                if (!string.IsNullOrWhiteSpace(uninstallResults.Logs))
                    summary.AppendFormat(
                        "{0} * Uninstall {1}.",
                        Environment.NewLine,
                        uninstallResults.ExitCode == 0
                            ? "was successful"
                            : "failed (allowed). Note that the process may have hung, indicating a not completely silent uninstall. This is usually seen when the last entry in the log is calling the uninstall. This can also happen when a window pops up and needs to be closed to continue");

                logs.Add(new PackageTestLog("_Summary.md", summary.ToString()));
                if (!string.IsNullOrWhiteSpace(installResults.Logs)) logs.Add(new PackageTestLog("Install.txt", string.IsNullOrWhiteSpace(installLog) ? installResults.Logs : installLog));
                if (!string.IsNullOrWhiteSpace(installResults.ImageLink)) logs.Add(new PackageTestLog("InstallImage.md", @"
This is the image that was taken when the install test failed:

![{0} v{1} install failure]({2})
".format_with(message.PackageId, message.PackageVersion, installResults.ImageLink)));

                if (!string.IsNullOrWhiteSpace(registrySnapshot)) logs.Add(new PackageTestLog("1.RegistrySnapshot.xml", registrySnapshot));
                if (!string.IsNullOrWhiteSpace(filesSnapshot)) logs.Add(new PackageTestLog("FilesSnapshot.xml", filesSnapshot));
                if (!string.IsNullOrWhiteSpace(upgradeResults.Logs)) logs.Add(new PackageTestLog("Upgrade.txt", upgradeResults.Logs));
                if (!string.IsNullOrWhiteSpace(uninstallResults.Logs)) logs.Add(new PackageTestLog("Uninstall.txt", string.IsNullOrWhiteSpace(uninstallLog) ? uninstallResults.Logs : uninstallLog));
                if (!string.IsNullOrWhiteSpace(uninstallResults.ImageLink)) logs.Add(new PackageTestLog("UninstallImage.md", @"
This is the image that was taken when the uninstall test failed:

![{0} v{1} uninstall failure]({2})
".format_with(message.PackageId, message.PackageVersion, uninstallResults.ImageLink)));

                EventManager.publish(
                    new PackageTestResultMessage(
                        message.PackageId,
                        message.PackageVersion,
                        "Windows2012R2 x64",
                        "win2012r2x64",
                        DateTime.UtcNow,
                        logs,
                        success: success
                        ));
            }
            catch (Exception ex)
            {
                Bootstrap.handle_exception(ex);
            }
            //finally
            //{
            //    TransactionLock.release(VAGRANT_LOCK_NAME, lockTaken: true);
            //}
        }

        private bool detect_vagrant_errors(string log, string packageId, string packageVersion)
        {
            if (string.IsNullOrWhiteSpace(log)) return false;
            if (log.Contains("An action 'provision' was attempted") || log.Contains("VBoxManage.exe: error:"))
            {
                this.Log().Warn("Unable to use vagrant machine for testing {0} v{1}:{2} {3}".format_with(packageId, packageVersion, Environment.NewLine, log));
                _testService.destroy();
                Thread.Sleep(20000);
                return true;
            }

            return false;
        }
    }
}
