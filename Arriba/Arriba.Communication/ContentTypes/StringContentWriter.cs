// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace Arriba.Communication.ContentTypes
{
    /// <summary>
    /// String content writer. 
    /// </summary>
    public sealed class StringContentWriter : IContentWriter
    {
        string IContentWriter.ContentType
        {
            get
            {
                return "text/plain";
            }
        }

        bool IContentWriter.CanWrite(Type t)
        {
            // We call ToString on everything, always true; 
            return true;
        }

        async Task IContentWriter.WriteAsync(IRequest request, Stream output, object content)
        {
            if (content == null)
            {
                return;
            }

            using (StreamWriter writer = new StreamWriter(output))
            {
                IEnumerable enumberable = content as IEnumerable;
                if (enumberable != null)
                {
                    foreach (var item in enumberable)
                    {
                        await writer.WriteLineAsync(item.ToString());
                    }
                }
                else
                {
                    await writer.WriteAsync(content.ToString());
                }
            }
        }
    }
}
