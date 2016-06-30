// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Arriba.Server.Hosting
{
    using Arriba.Model;
    using System.Composition;

    /// <summary>
    /// Represents a singleton database export
    /// </summary>
    [Export, Shared]
    public class DatabaseFactory
    {
        private static SecureDatabase s_database;

        public DatabaseFactory()
        {
            if (s_database == null)
            {
                s_database = new SecureDatabase();
            }
        }

        public SecureDatabase GetDatabase()
        {
            return s_database;
        }
    }
}
