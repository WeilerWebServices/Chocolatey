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

namespace chocolatey.package.verifier.infrastructure.commands
{
    using System;
    using System.Diagnostics;
    using System.IO;
    using System.Management;
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
            return execute(
                process,
                arguments,
                waitForExitInSeconds,
                file_system.get_directory_name(Assembly.GetExecutingAssembly().Location),
                stdOutAction,
                stdErrAction,
                null,
                updateProcessPath,
                allowUseWindow
                );
        }

        public int execute(
            string process,
            string arguments,
            int waitForExitInSeconds,
            string workingDirectory,
            Action<object, DataReceivedEventArgs> stdOutAction,
            Action<object, DataReceivedEventArgs> stdErrAction,
            Action timeoutAction,
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
                timeoutAction,
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
            Action timeoutAction,
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
                        if (timeoutAction != null) timeoutAction.Invoke();
                        ApplicationParameters.Name.Log().Warn(() => "Killing process ['\"{0}\" {1}']".format_with(process, arguments));
                        kill_process_and_children(p.Id);
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
                        kill_process_and_children(p.Id);
                    }
                }

                output.StandardOut = p.StandardOutput.ReadToEnd();
                output.StandardError = p.StandardError.ReadToEnd();
            }

            ApplicationParameters.Name.Log()
                .Debug(() => "Command ['\"{0}\" {1}'] exited with '{2}'".format_with(process, arguments, output.ExitCode));

            return output;
        }

        /// <summary>
        /// Kill a process, and all of its children, grandchildren, etc.
        /// </summary>
        /// <param name="pid">Process ID.</param>
        /// <remarks>From http://stackoverflow.com/a/10402906/18475 </remarks>
        public static void kill_process_and_children(int pid)
        {
            ManagementObjectSearcher searcher = new ManagementObjectSearcher
              ("Select * From Win32_Process Where ParentProcessID=" + pid);
            ManagementObjectCollection moc = searcher.Get();
            foreach (ManagementObject mo in moc)
            {
                kill_process_and_children(Convert.ToInt32(mo["ProcessID"]));
            }
            try
            {
                Process proc = Process.GetProcessById(pid);
                proc.Kill();
            }
            catch (ArgumentException)
            {
                // Process already exited.
            }
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
