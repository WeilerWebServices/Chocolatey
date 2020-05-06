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

namespace chocolatey.package.verifier.infrastructure.logging
{
    using System;

    public class ConsoleLog : ILog, ILog<ConsoleLog>
    {
        public void InitializeFor(string loggerName)
        {
        }

        public void Debug(string message, params object[] formatting)
        {
            Console.WriteLine("[DEBUG] " + string.Format(message, formatting));
        }

        public void Debug(Func<string> message)
        {
            Console.WriteLine("[DEBUG] " + message());
        }

        public void Info(string message, params object[] formatting)
        {
            Console.WriteLine(message, formatting);
        }

        public void Info(Func<string> message)
        {
            Console.WriteLine(message());
        }

        public void Warn(string message, params object[] formatting)
        {
            Console.WriteLine("[WARN] " + string.Format(message, formatting));
        }

        public void Warn(Func<string> message)
        {
            Console.WriteLine("[WARN] " + message());
        }

        public void Error(string message, params object[] formatting)
        {
            Console.WriteLine("[ERROR] " + string.Format(message, formatting));
        }

        public void Error(Func<string> message)
        {
            Console.WriteLine("[ERROR] " + message());
        }

        public void Fatal(string message, params object[] formatting)
        {
            Console.WriteLine("[FATAL] " + string.Format(message, formatting));
        }

        public void Fatal(Func<string> message)
        {
            Console.WriteLine("[FATAL] " + message());
        }
    }
}
