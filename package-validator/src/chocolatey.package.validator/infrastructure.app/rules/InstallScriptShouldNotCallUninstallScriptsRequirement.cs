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

namespace chocolatey.package.validator.infrastructure.app.rules
{
    using System.IO;
    using infrastructure.rules;
    using NuGet;
    using utility;

    public class InstallScriptShouldNotCallUninstallScriptsRequirement : BasePackageRule
    {
        public override string ValidationFailureMessage
        {
            get
            {
                return @"The install script should not directly call the uninstall script.  Your script does this, and it will need to be changed. [More...](https://github.com/chocolatey/package-validator/wiki/InstallScriptShouldntCallUninstallScript)";
            }
        }

        public override PackageValidationOutput is_valid(IPackage package)
        {
            var valid = true;

            var files = Utility.get_chocolatey_automation_scripts(package);
            foreach (var file in files.or_empty_list_if_null())
            {
                string extension = Path.GetExtension(file.Key.Path).to_lower();
                if (extension != "chocolateyinstall.ps1") continue;

                var contents = file.Value.to_lower();

                if (contents.Contains("chocolateyuninstall.ps1")) valid = false;
            }

            return valid;
        }
    }
}
