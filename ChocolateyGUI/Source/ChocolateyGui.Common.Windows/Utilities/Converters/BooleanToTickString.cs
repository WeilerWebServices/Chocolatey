﻿// --------------------------------------------------------------------------------------------------------------------
// <copyright company="Chocolatey" file="BooleanToTickString.cs">
//   Copyright 2017 - Present Chocolatey Software, LLC
//   Copyright 2014 - 2017 Rob Reynolds, the maintainers of Chocolatey, and RealDimensions Software, LLC
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace ChocolateyGui.Common.Windows.Utilities.Converters
{
    public class BooleanToTickString : DependencyObject, IValueConverter
    {
        private const string Tick = "✓";

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return (bool)value ? Tick : string.Empty;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}