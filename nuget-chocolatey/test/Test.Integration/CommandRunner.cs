using System;
using System.Diagnostics;
using System.IO;

namespace NuGet.Test.Integration
{
    public class CommandRunner
    {
        public static Tuple<int, string, string> Run(string process, string workingDirectory, string arguments, bool waitForExit, int timeOutInMilliseconds = 60000,
           Action<StreamWriter> inputAction = null)
        {
            string result = String.Empty;
            string error = String.Empty;

            ProcessStartInfo psi = new ProcessStartInfo(Path.GetFullPath(process), arguments)
            {
                WorkingDirectory = Path.GetFullPath(workingDirectory),
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                RedirectStandardInput = inputAction != null
            };

            StreamReader standardOutput;
            StreamReader errorOutput;
            int exitCode = 1;

            using (Process p = new Process())
            {
                p.StartInfo = psi;
                p.Start();
                standardOutput = p.StandardOutput;
                errorOutput = p.StandardError;

                if (inputAction != null)
                {
                    inputAction(p.StandardInput);
                }
                
                if (waitForExit)
                {
                    bool processExited = p.WaitForExit(timeOutInMilliseconds);
                    if (!processExited)
                    {
                        p.Kill();
                    }
                }

                result = standardOutput.ReadToEnd();
                error = errorOutput.ReadToEnd();
                
                if (p.HasExited)
                {
                    exitCode = p.ExitCode;
                }
            }           

            Console.WriteLine(result);
            Console.WriteLine(errorOutput);

            return Tuple.Create(exitCode, result, error);
        }
    }
}
