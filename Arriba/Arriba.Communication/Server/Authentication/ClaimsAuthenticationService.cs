// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Composition;
using System.Diagnostics;
using System.Security.Claims;
using System.Security.Principal;

namespace Arriba.Server.Authentication
{
    /// <summary>
    /// Windows authentication utilities. 
    /// </summary>
    [Export(typeof(ClaimsAuthenticationService)), Shared]
    internal class ClaimsAuthenticationService : IDisposable
    {
        private readonly RuntimeCache _cache = new RuntimeCache("Arriba.ClaimsAuthentication");
        private readonly TimeSpan _defaultTimeToLive = TimeSpan.FromMinutes(15);

        /// <summary>
        /// Determines whether the specified user is within the specified security group. 
        /// </summary>
        /// <param name="principal">User principal to check.</param>
        /// <param name="roleName">Role to validate.</param>
        /// <returns>True if the user is in the specified role, otherwise false.</returns>
        public bool IsUserInGroup(IPrincipal principal, string roleName)
        {
            if (principal == null)
            {
                throw new ArgumentNullException("principal");
            }
            else if (roleName == null)
            {
                throw new ArgumentNullException("roleName");
            }
            else if (roleName.Length == 0)
            {
                throw new ArgumentException("Role name should not be empty", "roleName");
            }

            ClaimsPrincipal cPrincipal = principal as ClaimsPrincipal;

            if (cPrincipal.Identity == null || String.IsNullOrEmpty(cPrincipal.Identity.Name) || !principal.Identity.IsAuthenticated)
            {
                return false;
            }

            // Cachekey should be in the form of UserInGroup:{Identity}:{Role}
            string cacheKey = String.Concat("UserInGroup:", principal.Identity.Name, ":", roleName);

            Debug.Assert(cacheKey.Contains(principal.Identity.Name));
            Debug.Assert(cacheKey.Contains(roleName));

            return _cache.GetOrAdd<bool>(
                cacheKey,
                () => principal.IsInRole(roleName),
                timeToLive: _defaultTimeToLive);
        }

        public void Dispose()
        {
            _cache.Dispose();
        }
    }
}

