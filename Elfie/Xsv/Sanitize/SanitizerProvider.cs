// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;

namespace Xsv.Sanitize
{
    public interface ISanitizerProvider
    {
        /// <summary>
        ///  Get the ISanitizeMapper to transform to a named type of thing (PersonName, IP).
        /// </summary>
        /// <param name="name">Name for which to get mapper</param>
        /// <returns>Mapper for name</returns>
        ISanitizeMapper Mapper(string name);
    }

    public class SanitizerProvider : ISanitizerProvider
    {
        private static Dictionary<string, ISanitizeMapper> Mappers { get; set; }

        static SanitizerProvider()
        {
            LoadMappers();
        }

        private static void LoadMappers()
        {
            Mappers = new Dictionary<string, ISanitizeMapper>(StringComparer.OrdinalIgnoreCase);

            // Register default mappers
            Mappers[string.Empty] = Mappers["Phrase"] = new PhraseMapper();
            Mappers["Alias"] = new AliasMapper();
            Mappers["IP"] = new IpMapper();
            Mappers["PersonName"] = new PersonNameMapper();
            Mappers["ComputerName"] = new ComputerNameMapper();
            Mappers["Int"] = new IntMapper();
            Mappers["Guid"] = new GuidMapper();

            // Register ISanitizeMappers from app.config
            AppConfigExtensibility.AddExtensionsOf<ISanitizeMapper>(Mappers);
        }

        public ISanitizeMapper Mapper(string name)
        {
            ISanitizeMapper result;
            if (!Mappers.TryGetValue(name, out result)) throw new UsageException($"SanitizerProvider doesn't have a provider for '{name}'.");
            return result;
        }

        public ICollection<string> MapperTypes()
        {
            return Mappers.Keys;
        }
    }
}
