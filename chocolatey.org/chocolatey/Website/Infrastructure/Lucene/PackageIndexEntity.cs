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
using System.Globalization;
using System.Linq;
using Lucene.Net.Documents;

namespace NuGetGallery
{
    public class PackageIndexEntity
    {
        internal static readonly char[] IdSeparators = new[] { '.', '-' };

        public Package Package { get; set; }
        
        public PackageIndexEntity()
        {
        }

        public PackageIndexEntity(Package package)
        {
            Package = package;
        }

        public Document ToDocument()
        {
            var document = new Document();

            // Note: Used to identify index records for updates
            document.Add(new Field("PackageRegistrationKey", Package.PackageRegistrationKey.ToString(CultureInfo.InvariantCulture), Field.Store.YES, Field.Index.NOT_ANALYZED));
            document.Add(new Field("Key", Package.Key.ToString(CultureInfo.InvariantCulture), Field.Store.YES, Field.Index.NOT_ANALYZED));

            //id fields
            document.Add(new Field("Id-Original", Package.PackageRegistration.Id, Field.Store.YES, Field.Index.NO));

            var field = new Field("Id-Exact", Package.PackageRegistration.Id.ToLowerInvariant(), Field.Store.YES, Field.Index.NOT_ANALYZED);
            field.Boost = 4.5f;
            document.Add(field);
            
            // We store the Id/Title field in multiple ways, so that it's possible to match using multiple
            // styles of search
            // Note: no matter which way we store it, it will also be processed by the Analyzer later.

            // Style 1: As-Is Id, no tokenizing (so you can search using dot or dash-joined terms)
            // Boost this one
            field = new Field("Id", Package.PackageRegistration.Id.ToLowerInvariant(), Field.Store.NO, Field.Index.ANALYZED);
            document.Add(field);

            // Style 2: dot+dash tokenized (so you can search using undotted terms)
            field = new Field("Id", SplitId(Package.PackageRegistration.Id.ToLowerInvariant()), Field.Store.NO, Field.Index.ANALYZED);
            field.Boost = 0.8f;
            document.Add(field);

            // Style 3: camel-case tokenized (so you can search using parts of the camelCasedWord). 
            // De-boosted since matches are less likely to be meaningful
            field = new Field("Id", CamelSplitId(Package.PackageRegistration.Id.ToLowerInvariant()), Field.Store.NO, Field.Index.ANALYZED);
            field.Boost = 0.25f;
            document.Add(field);

            document.Add(new Field("Version", Package.Version.to_string().ToLowerInvariant(), Field.Store.YES, Field.Index.NOT_ANALYZED));

            // title fields
            // If an element does not have a Title, fall back to Id, same as the website.
            var workingTitle = string.IsNullOrEmpty(Package.Title)
                                   ? Package.PackageRegistration.Id
                                   : Package.Title;

            field = new Field("Title-Exact", workingTitle.ToLowerInvariant(), Field.Store.YES, Field.Index.NOT_ANALYZED);
            field.Boost = 4.0f;
            document.Add(field);

            // As-Is (stored for search results)
            field = new Field("Title", workingTitle, Field.Store.YES, Field.Index.ANALYZED);
            field.Boost = 0.9f;
            document.Add(field);

            // no need to store dot+dash tokenized - we'll handle this in the analyzer
            field = new Field("Title", SplitId(workingTitle), Field.Store.NO, Field.Index.ANALYZED);
            field.Boost = 0.8f;
            document.Add(field);

            // camel-case tokenized
            field = new Field("Title", CamelSplitId(workingTitle), Field.Store.NO, Field.Index.ANALYZED);
            field.Boost = 0.5f;
            document.Add(field);

            document.Add(new Field("Summary", Package.Summary.to_string(), Field.Store.YES, Field.Index.NO));

            // Store description so we can show them in search results
            field = new Field("Description", Package.Description, Field.Store.YES, Field.Index.ANALYZED);
            field.Boost = 0.1f;
            document.Add(field);

            // Store tags so we can show them in search results
            field = new Field("Tags", Package.Tags.to_string(), Field.Store.YES, Field.Index.ANALYZED);
            field.Boost = 0.8f;
            document.Add(field);

            document.Add(new Field("Authors", Package.FlattenedAuthors.to_string(), Field.Store.YES, Field.Index.ANALYZED));
            document.Add(new Field("Copyright", Package.Copyright.to_string(), Field.Store.YES, Field.Index.NO));
            document.Add(new Field("Created", Package.Created.ToString(CultureInfo.InvariantCulture), Field.Store.YES, Field.Index.NO));
            document.Add(new Field("FlattenedDependencies", Package.FlattenedDependencies.to_string(), Field.Store.YES, Field.Index.NO));
            // sorting
            document.Add(new Field("DownloadCount", Package.PackageRegistration.DownloadCount.ToString(CultureInfo.InvariantCulture), Field.Store.YES, Field.Index.NOT_ANALYZED));
            document.Add(new Field("VersionDownloadCount", Package.DownloadCount.ToString(CultureInfo.InvariantCulture), Field.Store.YES, Field.Index.NO));

            // Fields for storing data to avoid hitting SQL while doing searches
            document.Add(new Field("IconUrl", Package.IconUrl.to_string(), Field.Store.YES, Field.Index.NO));

            // Fields meant for filtering, also storing data to avoid hitting SQL while doing searches
            document.Add(new Field("IsLatest", Package.IsLatest.to_string(), Field.Store.YES, Field.Index.NOT_ANALYZED));
            document.Add(new Field("IsLatestStable", Package.IsLatestStable.to_string(), Field.Store.YES, Field.Index.NOT_ANALYZED));
            document.Add(new Field("IsPrerelease", Package.IsPrerelease.to_string(), Field.Store.YES, Field.Index.NOT_ANALYZED));
            document.Add(new Field("Listed", Package.Listed.to_string(), Field.Store.YES, Field.Index.NOT_ANALYZED));
            document.Add(new Field("InIndex", "True", Field.Store.YES, Field.Index.NOT_ANALYZED));

            document.Add(new Field("Language", Package.Language.to_string(), Field.Store.YES, Field.Index.NO));
            document.Add(new Field("LastUpdated", Package.LastUpdated.ToString(CultureInfo.InvariantCulture), Field.Store.YES, Field.Index.NO));
            document.Add(new Field("Published", Package.Published.ToString(CultureInfo.InvariantCulture), Field.Store.YES, Field.Index.NO));
            // Fields meant for filtering, sorting
            document.Add(new Field("PublishedDate", Package.Published.Ticks.ToString(CultureInfo.InvariantCulture), Field.Store.NO, Field.Index.NOT_ANALYZED));
            document.Add(new Field("LicenseUrl", Package.LicenseUrl.to_string(), Field.Store.YES, Field.Index.NO));
            document.Add(new Field("RequiresLicenseAcceptance", Package.RequiresLicenseAcceptance.to_string(), Field.Store.YES, Field.Index.NO));
            document.Add(new Field("Hash", Package.Hash.ToStringSafe(), Field.Store.YES, Field.Index.NO));
            document.Add(new Field("HashAlgorithm", Package.HashAlgorithm.to_string(), Field.Store.YES, Field.Index.NO));
            document.Add(new Field("PackageFileSize", Package.PackageFileSize.ToString(CultureInfo.InvariantCulture), Field.Store.YES, Field.Index.NO));
            document.Add(new Field("ProjectUrl", Package.ProjectUrl.to_string(), Field.Store.YES, Field.Index.NO));
            document.Add(new Field("ReleaseNotes", Package.ReleaseNotes.to_string(), Field.Store.YES, Field.Index.NO));

            // nuspec enhancements
            document.Add(new Field("ProjectSourceUrl", Package.ProjectSourceUrl.to_string(), Field.Store.YES, Field.Index.NO));
            document.Add(new Field("PackageSourceUrl", Package.PackageSourceUrl.to_string(), Field.Store.YES, Field.Index.NO));
            document.Add(new Field("DocsUrl", Package.DocsUrl.to_string(), Field.Store.YES, Field.Index.NO));
            document.Add(new Field("MailingListUrl", Package.MailingListUrl.to_string(), Field.Store.YES, Field.Index.NO));
            document.Add(new Field("BugTrackerUrl", Package.BugTrackerUrl.to_string(), Field.Store.YES, Field.Index.NO));
            document.Add(new Field("PackageStatus", Package.Status.to_string(), Field.Store.YES, Field.Index.NO));
            document.Add(new Field("PackageSubmittedStatus", Package.SubmittedStatus.to_string(), Field.Store.YES, Field.Index.NO));

            document.Add(new Field("PackageTestResultUrl", Package.PackageTestResultUrl.to_string(), Field.Store.YES, Field.Index.NO));
            document.Add(new Field("PackageTestResultStatus", Package.PackageTestResultStatus.to_string(), Field.Store.YES, Field.Index.NO));
            if (Package.PackageTestResultDate.HasValue) document.Add(new Field("PackageTestResultStatusDate", Package.PackageTestResultDate.GetValueOrDefault().ToString(CultureInfo.InvariantCulture), Field.Store.YES, Field.Index.NO));
            document.Add(new Field("PackageValidationResultStatus", Package.PackageValidationResultStatus.to_string(), Field.Store.YES, Field.Index.NO));
            if (Package.PackageValidationResultDate.HasValue) document.Add(new Field("PackageValidationResultDate", Package.PackageValidationResultDate.GetValueOrDefault().ToString(CultureInfo.InvariantCulture), Field.Store.YES, Field.Index.NO));
            if (Package.PackageCleanupResultDate.HasValue) document.Add(new Field("PackageCleanupResultDate", Package.PackageCleanupResultDate.GetValueOrDefault().ToString(CultureInfo.InvariantCulture), Field.Store.YES, Field.Index.NO));
            if (Package.ReviewedDate.HasValue) document.Add(new Field("PackageReviewedDate", Package.ReviewedDate.GetValueOrDefault().ToString(CultureInfo.InvariantCulture), Field.Store.YES, Field.Index.NO));
            if (Package.ApprovedDate.HasValue) document.Add(new Field("PackageApprovedDate", Package.ApprovedDate.GetValueOrDefault().ToString(CultureInfo.InvariantCulture), Field.Store.YES, Field.Index.NO));
            var reviewedByUserName = string.Empty;
            if (Package.ReviewedBy != null)
            {
                reviewedByUserName = Package.ReviewedBy.Username;  
            }
            document.Add(new Field("PackageReviewer", reviewedByUserName, Field.Store.YES, Field.Index.NO));

            document.Add(new Field("DownloadCacheStatus", Package.DownloadCacheStatus.to_string(), Field.Store.YES, Field.Index.NO));
            if (Package.DownloadCacheDate.HasValue) document.Add(new Field("DownloadCacheDate", Package.DownloadCacheDate.GetValueOrDefault().ToString(CultureInfo.InvariantCulture), Field.Store.YES, Field.Index.NO));
            document.Add(new Field("DownloadCache", Package.DownloadCache.to_string(), Field.Store.YES, Field.Index.NO));
            document.Add(new Field("PackageScanStatus", Package.PackageScanStatus.to_string(), Field.Store.YES, Field.Index.NO));
            if (Package.PackageScanResultDate.HasValue) document.Add(new Field("PackageScanResultDate", Package.PackageScanResultDate.GetValueOrDefault().ToString(CultureInfo.InvariantCulture), Field.Store.YES, Field.Index.NO));

            if (Package.PackageRegistration.Owners.AnySafe())
            {
                string flattenedOwners = String.Join(";", Package.PackageRegistration.Owners.Select(o => o.Username));
                document.Add(new Field("Owners", flattenedOwners, Field.Store.NO, Field.Index.ANALYZED));
                document.Add(new Field("FlattenedOwners", flattenedOwners, Field.Store.YES, Field.Index.NO));
            }

            if (Package.SupportedFrameworks.AnySafe())
            {
                string joinedFrameworks = string.Join(";", Package.SupportedFrameworks.Select(f => f.FrameworkName));
                document.Add(new Field("JoinedSupportedFrameworks", joinedFrameworks, Field.Store.YES, Field.Index.NO));
            }

            string displayName = String.IsNullOrEmpty(Package.Title) ? Package.PackageRegistration.Id : Package.Title;
            document.Add(new Field("DisplayName", displayName.ToLower(CultureInfo.CurrentCulture), Field.Store.NO, Field.Index.NOT_ANALYZED));
          
            return document;
        }

        // Split up the id by - and . then join it back into one string (tokens in the same order).
        internal static string SplitId(string term)
        {
            var split = term.Split(IdSeparators, StringSplitOptions.RemoveEmptyEntries);
            return split.Any() ? string.Join(" ", split) : "";
        }

        internal static string CamelSplitId(string term)
        {
            var split = term.Split(IdSeparators, StringSplitOptions.RemoveEmptyEntries);
            var tokenized = split.SelectMany(CamelCaseTokenize);
            return tokenized.Any() ? string.Join(" ", tokenized) : "";
        }

        private static IEnumerable<string> CamelCaseTokenize(string term)
        {
            const int minTokenLength = 3;
            if (term.Length < minTokenLength)
            {
                yield break;
            }

            int tokenEnd = term.Length;
            for (int i = term.Length - 1; i > 0; i--)
            {
                // If the remainder is fewer than 2 chars or we have a token that is at least 2 chars long, tokenize it.
                if (i < minTokenLength || (Char.IsUpper(term[i]) && (tokenEnd - i >= minTokenLength)))
                {
                    if (i < minTokenLength)
                    {
                        // If the remainder is smaller than 2 chars, just return the entire string
                        i = 0;
                    }

                    yield return term.Substring(i, tokenEnd - i);
                    tokenEnd = i;
                }
            }

            // Finally return the term in entirety
            yield return term;
        }
    }
}
