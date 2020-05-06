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
    using NuGet;
    using infrastructure.rules;

    public class IconUrlUsePngOrSvgSuggestion : BasePackageRule
    {
        public override string ValidationFailureMessage { get { return 
@"As per the [packaging guidelines](https://github.com/chocolatey/choco/wiki/CreatePackages#package-icon-guidelines) icons should be either a png or svg file. [More...](https://github.com/chocolatey/package-validator/wiki/UsePngOrSvgForPackageIcons)";
            }
        }

        public override PackageValidationOutput is_valid(IPackage package)
        {
            if (string.IsNullOrWhiteSpace(package.IconUrl.to_string())) return true;

            return package.IconUrl.to_string().EndsWith(".png") || package.IconUrl.to_string().EndsWith(".svg");
        }
    }
}
