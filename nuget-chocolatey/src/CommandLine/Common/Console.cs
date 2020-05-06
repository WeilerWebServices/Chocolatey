﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security;
using NuGet;

namespace NuGet.Common
{
    public class Console : IConsole
    {
        public Console()
        {
            // setup CancelKeyPress handler so that the console colors are
            // restored to their original values when nuget.exe is interrupted
            // by Ctrl-C.
            var originalForegroundColor = System.Console.ForegroundColor;
            var originalBackgroundColor = System.Console.BackgroundColor;
            System.Console.CancelKeyPress += (sender, e) =>
            {
                System.Console.ForegroundColor = originalForegroundColor;
                System.Console.BackgroundColor = originalBackgroundColor;
            };
        }

        public int CursorLeft
        {
            get
            {
                try
                {
                    return System.Console.CursorLeft;
                }
                catch (IOException)
                {
                    return 0;
                }
            }
            set
            {
                System.Console.CursorLeft = value;
            }
        }

        public int WindowWidth
        {
            get
            {
                try
                {
                    return System.Console.WindowWidth;
                }
                catch (IOException)
                {
                    // probably means redirected to file
                    return int.MaxValue;
                }
            }
            set
            {
                System.Console.WindowWidth = value;
            }
        }

        public Verbosity Verbosity
        {
            get; set; 
        }

        public bool IsNonInteractive
        {
            get; set;
        }

        private TextWriter Out
        {
            get { return Verbosity == Verbosity.Quiet ? TextWriter.Null : System.Console.Out; }
        }

        public void Write(object value)
        {
            Out.Write(value);
        }

        public void Write(string value)
        {
            Out.Write(value);
        }

        public void Write(string format, params object[] args)
        {
            if (args == null || !args.Any())
            {
                // Don't try to format strings that do not have arguments. We end up throwing if the original string was not meant to be a format token 
                // and contained braces (for instance html)
                Out.Write(format);
            }
            else
            {
                Out.Write(format, args);
            }
        }

        public void WriteLine()
        {
            Out.WriteLine();
        }

        public void WriteLine(object value)
        {
            Out.WriteLine(value);
        }

        public void WriteLine(string value)
        {
            Out.WriteLine(value);
        }

        public void WriteLine(string format, params object[] args)
        {
            Out.WriteLine(format, args);
        }

        public void WriteError(object value)
        {
            WriteError(value.ToString());
        }

        public void WriteError(string value)
        {
            WriteError(value, new object[0]);
        }

        public void WriteError(string format, params object[] args)
        {
            WriteColor(System.Console.Error, ConsoleColor.Red, format, args);
        }

        public void WriteWarning(string value)
        {
            WriteWarning(prependWarningText: true, value: value, args: new object[0]);
        }

        public void WriteWarning(bool prependWarningText, string value)
        {
            WriteWarning(prependWarningText, value, new object[0]);
        }

        public void WriteWarning(string value, params object[] args)
        {
            WriteWarning(prependWarningText: true, value: value, args: args);
        }

        public void WriteWarning(bool prependWarningText, string value, params object[] args)
        {
            string message = prependWarningText
                                 ? String.Format(CultureInfo.CurrentCulture, LocalizedResourceManager.GetString("CommandLine_Warning"), value)
                                 : value;

            WriteColor(Out, ConsoleColor.Yellow, message, args);
        }

        public void WriteLine(ConsoleColor color, string value, params object[] args)
        {
            WriteColor(Out, color, value, args);
        }

        private static void WriteColor(TextWriter writer, ConsoleColor color, string value, params object[] args)
        {
            var currentColor = System.Console.ForegroundColor;
            try
            {
                currentColor = System.Console.ForegroundColor;
                System.Console.ForegroundColor = color;
                if (args == null || !args.Any())
                {
                    // If it doesn't look like something that needs to be formatted, don't format it.
                    writer.WriteLine(value);
                }
                else
                {
                    writer.WriteLine(value, args);
                }
            }
            finally
            {
                System.Console.ForegroundColor = currentColor;
            }
        }

        public void PrintJustified(int startIndex, string text)
        {
            PrintJustified(startIndex, text, WindowWidth);
        }

        public void PrintJustified(int startIndex, string text, int maxWidth)
        {
            if (maxWidth > startIndex)
            {
                maxWidth = maxWidth - startIndex - 1;
            }

            while (text.Length > 0)
            {
                // Trim whitespace at the beginning
                text = text.TrimStart();
                // Calculate the number of chars to print based on the width of the System.Console
                int length = Math.Min(text.Length, maxWidth);

                string content;

                // Text we can print without overflowing the System.Console, excluding new line characters.
                int newLineIndex = text.IndexOf(Environment.NewLine, 0, length, StringComparison.OrdinalIgnoreCase);
                if (newLineIndex > -1)
                {
                    content = text.Substring(0, newLineIndex);
                }
                else
                {
                    content = text.Substring(0, length);
                }

                int leftPadding = startIndex + content.Length - CursorLeft;
                // Print it with the correct padding
                Out.WriteLine((leftPadding > 0) ? content.PadLeft(leftPadding) : content);
                // Get the next substring to be printed
                text = text.Substring(content.Length);
            }
        }

        public bool Confirm(string description)
        {
            if (IsNonInteractive)
            {
                return true;
            }

            var currentColor = ConsoleColor.Gray;
            try
            {
                currentColor = System.Console.ForegroundColor;
                System.Console.ForegroundColor = ConsoleColor.Yellow;
                System.Console.Write(String.Format(CultureInfo.CurrentCulture, LocalizedResourceManager.GetString("ConsoleConfirmMessage"), description));
                var result = System.Console.ReadLine();
                return result.StartsWith(LocalizedResourceManager.GetString("ConsoleConfirmMessageAccept"), StringComparison.OrdinalIgnoreCase);
            }
            finally
            {
                System.Console.ForegroundColor = currentColor;
            }
        }

        public ConsoleKeyInfo ReadKey()
        {
            EnsureInteractive();
            return System.Console.ReadKey(intercept: true);
        }

        public string ReadLine()
        {
            EnsureInteractive();
            return System.Console.ReadLine();
        }

        public void ReadSecureString(SecureString secureString)
        {
            EnsureInteractive();
            try
            {
                ReadSecureStringFromConsole(secureString);
            }
            catch (InvalidOperationException)
            {
                // This can happen when you redirect nuget.exe input, either from the shell with "<" or 
                // from code with ProcessStartInfo. 
                // In this case, just read data from Console.ReadLine()
                foreach (var c in ReadLine())
                {
                    secureString.AppendChar(c);
                }
            }
            secureString.MakeReadOnly();
        }

        private static void ReadSecureStringFromConsole(SecureString secureString)
        {
            ConsoleKeyInfo keyInfo;
            while ((keyInfo = System.Console.ReadKey(intercept: true)).Key != ConsoleKey.Enter)
            {
                if (keyInfo.Key == ConsoleKey.Backspace)
                {
                    if (secureString.Length < 1)
                    {
                        continue;
                    }
                    System.Console.SetCursorPosition(System.Console.CursorLeft - 1, System.Console.CursorTop);
                    System.Console.Write(' ');
                    System.Console.SetCursorPosition(System.Console.CursorLeft - 1, System.Console.CursorTop);
                    secureString.RemoveAt(secureString.Length - 1);
                }
                else
                {
                    secureString.AppendChar(keyInfo.KeyChar);
                    System.Console.Write('*');
                }
            }
            System.Console.WriteLine();
        }

        private void EnsureInteractive()
        {
            if (IsNonInteractive)
            {
                throw new InvalidOperationException(LocalizedResourceManager.GetString("Error_CannotPromptForInput"));
            }
        }

        public void Log(MessageLevel level, string message, params object[] args)
        {
            switch (level)
            {
                case MessageLevel.Verbose:
                case MessageLevel.Info:
                    WriteLine(message, args);
                    break; 
                case MessageLevel.Warning:
                    WriteWarning(message, args);
                    break;
                case MessageLevel.Debug:
                    WriteColor(Out, ConsoleColor.Gray, message, args);
                    break;
                case MessageLevel.Error:
                case MessageLevel.Fatal:
                    WriteError(message, args);
                    break; 
            }
        }

        public FileConflictResolution ResolveFileConflict(string message)
        {
            // make the question stand out from previous text
            WriteLine();

            WriteLine(ConsoleColor.Yellow, "File Conflict.");
            WriteLine(message);

            // Yes - Yes To All - No - No To All
            var acceptedAnswers = new List<string> { "Y", "A", "N", "L" };
            var choices = new[]
            {
                FileConflictResolution.Overwrite,
                FileConflictResolution.OverwriteAll,
                FileConflictResolution.Ignore,
                FileConflictResolution.IgnoreAll
            };

            while (true)
            {
                Write(LocalizedResourceManager.GetString("FileConflictChoiceText"));
                string answer = ReadLine();                
                if (!String.IsNullOrEmpty(answer)) 
                {
                    int index = acceptedAnswers.FindIndex(a => a.Equals(answer, StringComparison.OrdinalIgnoreCase));
                    if (index > -1)
                    {
                        return choices[index];
                    }
                }
            }
        }
    }
}