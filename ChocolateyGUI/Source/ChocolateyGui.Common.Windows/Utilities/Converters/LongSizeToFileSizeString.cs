﻿// --------------------------------------------------------------------------------------------------------------------
// <copyright company="Chocolatey" file="LongSizeToFileSizeString.cs">
//   Copyright 2017 - Present Chocolatey Software, LLC
//   Copyright 2014 - 2017 Rob Reynolds, the maintainers of Chocolatey, and RealDimensions Software, LLC
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

using System;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Text;
using System.Windows.Data;

namespace ChocolateyGui.Common.Windows.Utilities.Converters
{
    public sealed class LongSizeToFileSizeString : IValueConverter
    {
        /// <summary>
        ///     Converts a numeric value into a string that represents the number expressed as a size value in bytes, kilobytes,
        ///     megabytes, or gigabytes, depending on the size.
        /// </summary>
        /// <param name="fileSize">The numeric value to be converted.</param>
        /// <returns>the converted string</returns>
        [SuppressMessage(
            "Microsoft.Naming",
            "CA1704:IdentifiersShouldBeSpelledCorrectly",
            MessageId = "Str",
            Justification = "Name matches underlying PInvoke")]
        public static string StrFormatByteSize(long fileSize)
        {
            var sb = new StringBuilder(16);
            NativeMethods.StrFormatByteSize(fileSize, sb, sb.Capacity);
            return sb.ToString();
        }

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (!(value is long))
            {
                return string.Empty;
            }

            return StrFormatByteSize((long)value);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}