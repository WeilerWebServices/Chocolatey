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
using System.IO;
using Lucene.Net.Analysis;
using Lucene.Net.Analysis.Standard;

namespace NuGetGallery
{
    public class PerFieldAnalyzer : PerFieldAnalyzerWrapper
    {
        public PerFieldAnalyzer()
            : base(new StandardAnalyzer(LuceneCommon.LuceneVersion), CreateFieldAnalyzers())
        {
        }

        private static IDictionary<string, Analyzer> CreateFieldAnalyzers()
        {
            return new Dictionary<string, Analyzer>(StringComparer.OrdinalIgnoreCase)
            {
                { "Id", new StandardAnalyzer(LuceneCommon.LuceneVersion, new HashSet<string>()) },
                { "Id-Exact", new IdExactAnalyzer() },
                { "Title", new TitleAnalyzer() },
                { "Description", new DescriptionAnalyzer() },
                { "Tags", new DescriptionAnalyzer() },
            };
        }

        //  similar to a StandardAnalyzer except this allows special characters (like C++)
        //  note the base tokenization is now just whitespace in this case

        private class TitleAnalyzer : Analyzer
        {
            private static readonly WhitespaceAnalyzer whitespaceAnalyzer = new WhitespaceAnalyzer();

            public override TokenStream TokenStream(string fieldName, TextReader reader)
            {
                // Split the title based on IdSeparators, then run it through the innerAnalyzer
                string title = reader.ReadToEnd();
                string partiallyTokenized = String.Join(" ", title.Split(PackageIndexEntity.IdSeparators, StringSplitOptions.RemoveEmptyEntries));
                TokenStream result = whitespaceAnalyzer.TokenStream(fieldName, new StringReader(partiallyTokenized));
                result = new LowerCaseFilter(result);
                return result;
            }
        }   
        
        //  similar to a StandardAnalyzer except this allows hyphens (-)
        //  note the base tokenization is now just whitespace in this case

        private class IdExactAnalyzer : Analyzer
        {
            private static readonly WhitespaceAnalyzer whitespaceAnalyzer = new WhitespaceAnalyzer();

            public override TokenStream TokenStream(string fieldName, TextReader reader)
            {
                var tokenizer = new WhitespaceTokenizer(reader);

                return new LowerCaseFilter(tokenizer);
            }
        }

        //  similar to our TitleAnalyzer except we want to ignore stop words in the description
        private class DescriptionAnalyzer : Analyzer
        {
            private static readonly ISet<string> stopWords = new HashSet<string>
            {
                "a",
                "an",
                "and",
                "are",
                "as",
                "at",
                "be",
                "but",
                "by",
                "for",
                "if",
                "in",
                "into",
                "is",
                "it",
                "no",
                "not",
                "of",
                "on",
                "or",
                "such",
                "that",
                "the",
                "their",
                "then",
                "there",
                "these",
                "they",
                "this",
                "to",
                "was",
                "will",
                "with"
            };

            private static readonly WhitespaceAnalyzer whitespaceAnalyzer = new WhitespaceAnalyzer();

            public override TokenStream TokenStream(string fieldName, TextReader reader)
            {
                TokenStream result = whitespaceAnalyzer.TokenStream(fieldName, reader);
                result = new LowerCaseFilter(result);
                result = new StopFilter(true, result, stopWords);
                return result;
            }
        }
    }
}
