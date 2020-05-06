﻿// --------------------------------------------------------------------------------------------------------------------
// <copyright company="Chocolatey" file="IConfigService.cs">
//   Copyright 2017 - Present Chocolatey Software, LLC
//   Copyright 2014 - 2017 Rob Reynolds, the maintainers of Chocolatey, and RealDimensions Software, LLC
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using ChocolateyGui.Common.Models;

namespace ChocolateyGui.Common.Services
{
    public interface IConfigService
    {
        AppConfiguration GetAppConfiguration();

        void UpdateSettings(AppConfiguration settings);

        IEnumerable<ChocolateyGuiFeature> GetFeatures();

        void ListFeatures(ChocolateyGuiConfiguration configuration);

        IEnumerable<ChocolateyGuiSetting> GetSettings();

        void ListSettings(ChocolateyGuiConfiguration configuration);

        void EnableFeature(ChocolateyGuiConfiguration configuration);

        void DisableFeature(ChocolateyGuiConfiguration configuration);

        void GetConfigValue(ChocolateyGuiConfiguration configuration);

        void SetConfigValue(ChocolateyGuiConfiguration configuration);

        void UnsetConfigValue(ChocolateyGuiConfiguration configuration);
    }
}