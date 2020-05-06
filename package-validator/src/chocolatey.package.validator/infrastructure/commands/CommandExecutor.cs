﻿// Copyright © 2015 - Present RealDimensions Software, LLC
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

namespace chocolatey.package.validator.infrastructure.commands
{
    using System;
    using System.Diagnostics;
    using System.Reflection;
    using app;
    using filesystem;

    public sealed class CommandExecutor : ICommandExecutor
    {
        public CommandExecutor(IFileSystem fileSystem)
        {
            _fileSystemInitializer = new Lazy<IFileSystem>(() => fileSystem);
        }

        private static Lazy<IFileSystem> _fileSystemInitializer = new Lazy<IFileSystem>(() => new DotNetFileSystem());

        private static IFileSystem file_system { get { return _fileSystemInitializer.Value; } }

        public int execute(string process, string arguments, int waitForExitInSeconds)
        {
            return execute(
                process,
                arguments,
                waitForExitInSeconds,
                file_system.get_directory_name(file_system.get_current_assembly_path()));
        }

        public int execute(
            string process,
            string arguments,
            int waitForExitInSeconds,
            Action<object, DataReceivedEventArgs> stdOutAction,
            Action<object, DataReceivedEventArgs> stdErrAction,
            bool updateProcessPath = true
            )
        {
            return execute(
                process,
                arguments,
                waitForExitInSeconds,
                file_system.get_directory_name(file_system.get_current_assembly_path()),
                stdOutAction,
                stdErrAction,
                updateProcessPath,
                allowUseWindow: false
                );
        }

        public int execute(string process, string arguments, int waitForExitInSeconds, string workingDirectory)
        {
            return execute(
                process,
                arguments,
                waitForExitInSeconds,
                workingDirectory,
                null,
                null,
                updateProcessPath: true,
                allowUseWindow: false);
        }

        public int execute(
            string process,
            string arguments,
            int waitForExitInSeconds,
            string workingDirectory,
            Action<object, DataReceivedEventArgs> stdOutAction,
            Action<object, DataReceivedEventArgs> stdErrAction,
            bool updateProcessPath,
            bool allowUseWindow
            )
        {
            return execute_static(
                process,
                arguments,
                waitForExitInSeconds,
                file_system.get_directory_name(Assembly.GetExecutingAssembly().Location),
                stdOutAction,
                stdErrAction,
                updateProcessPath,
                allowUseWindow
                );
        }

        public static int execute_static(
            string process,
            string arguments,
            int waitForExitInSeconds,
            string workingDirectory,
            Action<object, DataReceivedEventArgs> stdOutAction,
            Action<object, DataReceivedEventArgs> stdErrAction,
            bool updateProcessPath,
            bool allowUseWindow
            )
        {
            int exitCode = -1;
            if (updateProcessPath) process = file_system.get_full_path(process);

            ApplicationParameters.Name.Log().Debug(() => "Calling command ['\"{0}\" {1}']".format_with(process, arguments));

            var psi = new ProcessStartInfo(process, arguments)
            {
                UseShellExecute = false,
                WorkingDirectory = workingDirectory,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = !allowUseWindow,
                WindowStyle = ProcessWindowStyle.Minimized,
            };

            using (var p = new Process())
            {
                p.StartInfo = psi;
                if (stdOutAction == null) p.OutputDataReceived += log_output;
                else p.OutputDataReceived += (s, e) => stdOutAction(s, e);
                if (stdErrAction == null) p.ErrorDataReceived += log_error;
                else p.ErrorDataReceived += (s, e) => stdErrAction(s, e);

                p.EnableRaisingEvents = true;
                p.Start();
                p.BeginErrorReadLine();
                p.BeginOutputReadLine();

                if (waitForExitInSeconds > 0)
                {
                    var exited = p.WaitForExit((int)TimeSpan.FromSeconds(waitForExitInSeconds).TotalMilliseconds);
                    if (exited)
                    {
                        exitCode = p.ExitCode;
                    }
                    else
                    {
                        ApplicationParameters.Name.Log().Warn(() => "Killing process ['\"{0}\" {1}']".format_with(process, arguments));
                        p.Kill();
                    }
                }
            }

            ApplicationParameters.Name.Log().Debug(
                () => "Command ['\"{0}\" {1}'] exited with '{2}'".format_with(process, arguments, exitCode));

            return exitCode;
        }

        public ProcessOutput execute(
            string process,
            string arguments,
            int waitForExitInSeconds,
            string workingDirectory,
            bool updateProcessPath,
            bool allowUseWindow
            )
        {
            return execute_static(
                process,
                arguments,
                waitForExitInSeconds,
                file_system.get_directory_name(Assembly.GetExecutingAssembly().Location),
                updateProcessPath,
                allowUseWindow
                );
        }

        public static ProcessOutput execute_static(
            string process,
            string arguments,
            int waitForExitInSeconds,
            string workingDirectory,
            bool updateProcessPath,
            bool allowUseWindow
            )
        {
            if (updateProcessPath) process = file_system.get_full_path(process);

            ApplicationParameters.Name.Log().Debug(() => "Calling command ['\"{0}\" {1}']".format_with(process, arguments));

            var psi = new ProcessStartInfo(process, arguments)
            {
                UseShellExecute = false,
                WorkingDirectory = workingDirectory,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = !allowUseWindow,
                WindowStyle = ProcessWindowStyle.Minimized,
            };

            var output = new ProcessOutput
            {
                ExitCode = -1
            };

            using (var p = new Process())
            {
                p.StartInfo = psi;
                p.Start();

                if (waitForExitInSeconds > 0)
                {
                    var exited = p.WaitForExit((int)TimeSpan.FromSeconds(waitForExitInSeconds).TotalMilliseconds);
                    if (exited)
                    {
                        output.ExitCode = p.ExitCode;
                    } 
                    else
                    {
                        ApplicationParameters.Name.Log().Warn(() => "Killing process ['\"{0}\" {1}']".format_with(process, arguments));
                        p.Kill();
                    }
                }

                output.StandardOut = p.StandardOutput.ReadToEnd();
                output.StandardError = p.StandardError.ReadToEnd();
            }

            ApplicationParameters.Name.Log()
                .Debug(() => "Command ['\"{0}\" {1}'] exited with '{2}'".format_with(process, arguments, output.ExitCode));

            return output;
        }

        private static void log_output(object sender, DataReceivedEventArgs e)
        {
            if (e != null) ApplicationParameters.Name.Log().Info(e.Data);
        }

        private static void log_error(object sender, DataReceivedEventArgs e)
        {
            if (e != null) ApplicationParameters.Name.Log().Error(e.Data);
        }
    }
}
