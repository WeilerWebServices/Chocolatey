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

using System;
using System.Collections.Generic;
using System.Configuration;
using System.Web;

namespace NuGetGallery
{
    public class Configuration : IConfiguration
    {
        private static readonly Dictionary<string, Lazy<object>> _configThunks = new Dictionary<string, Lazy<object>>();
        private readonly Lazy<string> _httpSiteRootThunk;
        private readonly Lazy<string> _httpsSiteRootThunk;

        public Configuration()
        {
            _httpSiteRootThunk = new Lazy<string>(GetHttpSiteRoot);
            _httpsSiteRootThunk = new Lazy<string>(GetHttpsSiteRoot);
        }

        public static string ReadAppSettings(string key)
        {
            return ReadAppSettings(key, value => value);
        }

        public static T ReadAppSettings<T>(string key, Func<string, T> valueThunk)
        {
            if (!_configThunks.ContainsKey(key))
            {
                _configThunks.Add(
                    key, new Lazy<object>(
                             () =>
                             {
                                 var value = ConfigurationManager.AppSettings[string.Format("Gallery:{0}", key)];
                                 if (string.IsNullOrWhiteSpace(value)) value = null;
                                 return valueThunk(value);
                             }));
            }

            return (T)_configThunks[key].Value;
        }

        public string AzureStorageAccessKey { get { return ReadAppSettings("AzureStorageAccessKey"); } }

        public string AzureStorageAccountName { get { return ReadAppSettings("AzureStorageAccountName"); } }

        public string AzureStorageBlobUrl { get { return ReadAppSettings("AzureStorageBlobUrl"); } }

        public string FileStorageDirectory { get { return ReadAppSettings("FileStorageDirectory", value => value ?? HttpContext.Current.Server.MapPath("~/App_Data/Files")); } }

        public PackageStoreType PackageStoreType
        {
            get { return ReadAppSettings("PackageStoreType", value => (PackageStoreType)Enum.Parse(typeof(PackageStoreType), value ?? PackageStoreType.NotSpecified.ToString())); }
        }

        public PackageStatisticsStoreType PackageStatisticsStoreType
        {
            get { return ReadAppSettings("PackageStatisticsStoreType", value => (PackageStatisticsStoreType)Enum.Parse(typeof(PackageStatisticsStoreType), value ?? PackageStatisticsStoreType.NotSpecified.ToString())); }
        }

        public string AzureCdnHost { get { return ReadAppSettings("AzureCdnHost"); } }

        public string GalleryOwnerName { get { return ReadAppSettings("GalleryOwnerName", (value) => value ?? string.Empty); } }

        public string GalleryOwnerEmail { get { return ReadAppSettings("GalleryOwnerEmail", (value) => value ?? string.Empty); } }

        public bool ConfirmEmailAddresses { get { return ReadAppSettings("ConfirmEmailAddresses", (value) => bool.Parse(value ?? bool.TrueString)); } }

        public string S3Bucket { get { return ReadAppSettings("S3Bucket", (value) => value ?? string.Empty); } }

        public string PackagesUrl { get { return ReadAppSettings("PackagesUrl", (value) => value ?? string.Empty); } }

        public string S3AccessKey { get { return ReadAppSettings("S3AccessKey", (value) => value ?? string.Empty); } }

        public string S3SecretKey { get { return ReadAppSettings("S3SecretKey", (value) => value ?? string.Empty); } }

        public string SqsServiceUrl { get { return ReadAppSettings("SqsServiceUrl", (value) => value ?? string.Empty); } }

        public bool UseSmtp { get { return ReadAppSettings("UseSmtp", (value) => bool.Parse(value ?? bool.FalseString)); } }
        
        public string SmtpHost { get { return ReadAppSettings("SmtpHost", (value) => value ?? string.Empty); }  }

        public string SmtpUsername { get { return ReadAppSettings("SmtpUsername", (value) => value ?? string.Empty); } }
        
        public string SmtpPassword { get { return ReadAppSettings("SmtpPassword", (value) => value ?? string.Empty); } }

        public int? SmtpPort { get { return ReadAppSettings("SmtpPort", (value) => int.Parse(value)); } }

        public bool SmtpEnableSsl { get { return ReadAppSettings("SmtpEnableSsl", (value) => bool.Parse(value ?? bool.TrueString)); } }

        public string ModerationEmail { get { return ReadAppSettings("ModerationEmail", (value) => value ?? string.Empty); } }

        public bool UseCaching { get { return ReadAppSettings("UseCaching", (value) => bool.Parse(value ?? bool.TrueString)); } }

        public bool HostImages { get { return ReadAppSettings("HostImages", (value) => bool.Parse(value ?? bool.TrueString)); } }

        public int PackageOperationsUserKey { get { return ReadAppSettings("PackageOperationsUserKey", (value) => int.Parse(value)); } }

        public string ScanResultsKey { get { return ReadAppSettings("ScanResultsKey", (value) => value ?? string.Empty); } }

        public bool IndexContainsAllVersions { get { return ReadAppSettings("IndexContainsAllVersions", (value) => bool.Parse(value ?? bool.TrueString)); } }

        public bool UseBackgroundJobsDatabaseUser { get { return ReadAppSettings("UseBackgroundJobsDatabaseUser", (value) => bool.Parse(value ?? bool.TrueString)); } }

        public string BackgroundJobsDatabaseUserId { get { return ReadAppSettings("BackgroundJobsDatabaseUserId", (value) => value ?? string.Empty); } }

        public string BackgroundJobsDatabaseUserPassword { get { return ReadAppSettings("BackgroundJobsDatabaseUserPassword", (value) => value ?? string.Empty); } }

        protected virtual string GetConfiguredSiteRoot()
        {
            return ReadAppSettings("SiteRoot");
        }

        protected virtual HttpRequestBase GetCurrentRequest()
        {
            return new HttpRequestWrapper(HttpContext.Current.Request);
        }

        public string GetSiteRoot(bool useHttps)
        {
            return useHttps ? _httpsSiteRootThunk.Value : _httpSiteRootThunk.Value;
        }

        private string GetHttpSiteRoot()
        {
            var request = GetCurrentRequest();
            string siteRoot;

            if (request.IsLocal) siteRoot = request.Url.GetLeftPart(UriPartial.Authority) + '/';
            else siteRoot = GetConfiguredSiteRoot();

            if (!siteRoot.StartsWith("http://", StringComparison.OrdinalIgnoreCase) && !siteRoot.StartsWith("https://", StringComparison.OrdinalIgnoreCase)) throw new InvalidOperationException("The configured site root must start with either http:// or https://.");

            if (siteRoot.StartsWith("https://", StringComparison.OrdinalIgnoreCase)) siteRoot = "http://" + siteRoot.Substring(8);

            return siteRoot;
        }

        private string GetHttpsSiteRoot()
        {
            var siteRoot = _httpSiteRootThunk.Value;

            if (!siteRoot.StartsWith("http://", StringComparison.OrdinalIgnoreCase)) throw new InvalidOperationException("The configured HTTP site root must start with http://.");

            return "https://" + siteRoot.Substring(7);
        }
    }
}
