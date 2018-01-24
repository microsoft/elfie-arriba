// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Elfie.Serialization;
using System;
using System.Text;

namespace Xsv.Sanitize
{
    /// <summary>
    ///  PhraseMapper maps hashes into ThreeWordPhrases of common, short, English Words.
    /// </summary>
    public class PhraseMapper : ISanitizeMapper
    {
        private string[] TopWords { get; set; }

        public PhraseMapper()
        {
            this.TopWords = Resource.ReadAllStreamLines(@"Xsv.Sanitize.Data.TopWords.txt");
        }

        public string Generate(uint hash)
        {
            StringBuilder result = new StringBuilder();

            uint hashRemaining = hash;
            while (hashRemaining > 0)
            {
                int index = Hashing.Extract(ref hashRemaining, this.TopWords.Length);
                string word = this.TopWords[index];
                result.Append(Char.ToUpper(word[0]));
                result.Append(word.Substring(1));
            }

            return result.ToString();
        }
    }
}
