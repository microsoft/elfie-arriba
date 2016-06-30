// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Linq;

namespace Arriba.Communication
{
    public static class RequestExtensions
    {
        /// <summary>
        /// Validates if the accept type of the request contains any of the specified mime types.
        /// </summary>
        /// <param name="request">Request.</param>
        /// <param name="mimeTypes">Mime types to validate.</param>
        /// <returns><c>true</c> if the request accepts any of the specified types, otherwise <c>false</c>.</returns>
        public static bool AcceptedResponseTypesContains(this IRequest request, params string[] mimeTypes)
        {
            if (mimeTypes.Length == 0)
            {
                return request.AcceptedResponseTypes.Any();
            }

            foreach (var mimeType in mimeTypes)
            {
                if (request.AcceptedResponseTypes.Contains(mimeType, StringComparer.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
