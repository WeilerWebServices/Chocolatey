// Copyright 2017 - 2020 Chocolatey Software
// Copyright 2011 - 2017RealDimensions Software, LLC, the original 
// authors/contributors from ChocolateyGallery
// at https://github.com/chocolatey/chocolatey.org,
// and the authors/contributors of NuGetGallery 
// at https://github.com/NuGet/NuGetGallery
//  
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// 
//   http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using System;
using System.Diagnostics;

namespace NuGetGallery
{
    public class MethodExtensionWrappers
    {
        public static T WrapExecutionTracingTime<T>(Func<T> func, string message)
        {
            T result = default(T);
            var startTime = DateTime.UtcNow.Ticks;
            if (func != null)
            {
                result = func.Invoke();
            }

            var totalTime = (DateTime.UtcNow.Ticks - startTime);
            if (totalTime < TimeSpan.TicksPerSecond)
            {
                Trace.WriteLine(message + " (" + (totalTime / TimeSpan.TicksPerMillisecond) + " milliseconds)");
            }
            else
            {
                Trace.WriteLine(message + " (" + (totalTime / TimeSpan.TicksPerSecond) + " seconds)");
            }

            return result;
        }


        public static T WrapExecutionTracingTime<T>(Func<T> func, Func<string> message)
        {
            T result = default(T);
            var startTime = DateTime.UtcNow.Ticks;
            if (func != null)
            {
                result = func.Invoke();
            }
            var totalTime = (DateTime.UtcNow.Ticks - startTime);
           
            if (message != null)
            {
                if (totalTime < TimeSpan.TicksPerSecond)
                {
                    Trace.WriteLine(message() + " (" + (totalTime / TimeSpan.TicksPerMillisecond) + " milliseconds)");
                }
                else
                {
                    Trace.WriteLine(message() + " (" + (totalTime / TimeSpan.TicksPerSecond) + " seconds)");
                }
            }

            return result;
        }

        public static void WrapExecutionTracingTime(Action action, string message)
        {
            var startTime = DateTime.UtcNow.Ticks;
            if (action != null)
            {
                action.Invoke();
            }

            var totalTime = (DateTime.UtcNow.Ticks - startTime);
            if (totalTime < TimeSpan.TicksPerSecond)
            {
                Trace.WriteLine(message + " (" + (totalTime / TimeSpan.TicksPerMillisecond) + " milliseconds)");
            }
            else
            {
                Trace.WriteLine(message + " (" + (totalTime / TimeSpan.TicksPerSecond) + " seconds)");
            }
        }

        public static void WrapExecutionTracingTime(Action action, Func<string> message)
        {
            var startTime = DateTime.UtcNow.Ticks;
            if (action != null)
            {
                action.Invoke();
            }
            var totalTime = (DateTime.UtcNow.Ticks - startTime);
            
            if (message != null)
            {
                if (totalTime < TimeSpan.TicksPerSecond)
                {
                    Trace.WriteLine(message() + " (" + (totalTime / TimeSpan.TicksPerMillisecond) + " milliseconds)");
                }
                else
                {
                    Trace.WriteLine(message() + " (" + (totalTime / TimeSpan.TicksPerSecond) + " seconds)");
                }
            }
        }
    }
}
