// Copyright 2011 - Present RealDimensions Software, LLC, the original 
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

using System.Collections.Generic;
using System.Reflection;
using System.Web.Mvc;

namespace NuGetGallery
{
    /// <summary>
    ///   Binder
    /// </summary>
    /// <remarks>
    ///   http://stackoverflow.com/q/26152374/18475
    ///   Based on ideas from http://stackoverflow.com/q/9417888/18475 and http://stackoverflow.com/a/2340598/18475
    ///   https://msdn.microsoft.com/en-us/magazine/hh781022.aspx
    ///   http://blog.baltrinic.com/software-development/dotnet/better-array-model-binding-in-asp-net-mvc
    ///   https://lostechies.com/jimmybogard/2011/07/07/intelligent-model-binding-with-model-binder-providers/
    /// </remarks>
    public class BaseModelBinder<TModel> : IModelBinder where TModel : class, new()
    {
        public virtual object BindModel(ControllerContext controllerContext, ModelBindingContext bindingContext)
        {
            TModel model = (TModel)bindingContext.Model ?? new TModel();
            ICollection<string> propertyNames = bindingContext.PropertyMetadata.Keys;
            foreach (var propertyName in propertyNames)
            {
                var property = model.GetType().GetProperty(propertyName, BindingFlags.IgnoreCase | BindingFlags.Instance | BindingFlags.Public);
                if (property == null) continue;

                var value = GetValue(bindingContext, propertyName);
                property.SetValue(model, value, null);
            }

            return model;
        }

        private string GetValue(ModelBindingContext context, string name)
        {
            name = (context.ModelName == "" ? "" : context.ModelName + ".") + name;

            ValueProviderResult result = context.ValueProvider.GetValue(name);

            if (result == null) return null;

            return result.AttemptedValue;
        }
    }
}
