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

namespace chocolatey.package.validator.tests
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using Moq;
    using validator.infrastructure.logging;

    // ReSharper disable InconsistentNaming

    public enum LogLevel
    {
        Debug,
        Info,
        Warn,
        Error,
        Fatal
    }

    public class MockLogger : Mock<ILog>, ILog, ILog<MockLogger>
    {
        public void reset()
        {
            Messages.Clear();
            //this.ResetCalls();
            LogMessagesToConsole = false;
        }

        public bool LogMessagesToConsole { get; set; }

        private readonly Lazy<ConcurrentDictionary<string, IList<string>>> _messages =
            new Lazy<ConcurrentDictionary<string, IList<string>>>();

        public ConcurrentDictionary<string, IList<string>> Messages { get { return _messages.Value; } }

        public IList<string> MessagesFor(LogLevel logLevel)
        {
            return _messages.Value.GetOrAdd(logLevel.ToString(), new List<string>());
        }

        public void InitializeFor(string loggerName)
        {
        }

        public void LogMessage(LogLevel logLevel, string message)
        {
            var list = _messages.Value.GetOrAdd(logLevel.ToString(), new List<string>());
            list.Add(message);
            if (LogMessagesToConsole) Console.WriteLine("[{0}] {1}".format_with(logLevel.to_string(), message));
        }

        public void Debug(string message, params object[] formatting)
        {
            Object.Debug(message.format_with(formatting));
            LogMessage(LogLevel.Debug, message.format_with(formatting));
        }

        public void Debug(Func<string> message)
        {
            Object.Debug(message());
            LogMessage(LogLevel.Debug, message());
        }

        public void Info(string message, params object[] formatting)
        {
            Object.Info(message.format_with(formatting));
            LogMessage(LogLevel.Info, message.format_with(formatting));
        }

        public void Info(Func<string> message)
        {
            Object.Info(message());
            LogMessage(LogLevel.Info, message());
        }

        public void Warn(string message, params object[] formatting)
        {
            Object.Warn(message.format_with(formatting));
            LogMessage(LogLevel.Warn, message.format_with(formatting));
        }

        public void Warn(Func<string> message)
        {
            Object.Warn(message());
            LogMessage(LogLevel.Warn, message());
        }

        public void Error(string message, params object[] formatting)
        {
            Object.Error(message.format_with(formatting));
            LogMessage(LogLevel.Error, message.format_with(formatting));
        }

        public void Error(Func<string> message)
        {
            Object.Error(message());
            LogMessage(LogLevel.Error, message());
        }

        public void Fatal(string message, params object[] formatting)
        {
            Object.Fatal(message.format_with(formatting));
            LogMessage(LogLevel.Fatal, message.format_with(formatting));
        }

        public void Fatal(Func<string> message)
        {
            Object.Fatal(message());
            LogMessage(LogLevel.Fatal, message());
        }
    }

    // ReSharper restore InconsistentNaming
}
