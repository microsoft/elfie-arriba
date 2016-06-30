// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Arriba.Communication.Application
{
    public abstract class StaticFileApplication : RoutedApplication<IResponse>
    {
        private Dictionary<string, string> _contentTypeCache = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        private string _basePath;
        private string _baseRoute;
        private string _defaultFile;

        public override string Name
        {
            get
            {
                return String.Format("Static serving \"{0}\" for \"{1}\"", _baseRoute, _basePath);
            }
        }

        public StaticFileApplication(string route, string path, string defaultFile)
        {
            if (String.IsNullOrEmpty(route) || !route.StartsWith("/") || !route.EndsWith("/"))
            {
                throw new ArgumentException(String.Format("Route format \"{0}\" is invalid, it must start and end with a \"/\"", route), "route");
            }

            _baseRoute = route;
            _basePath = path;
            _defaultFile = defaultFile;
            this.Get(route + "*:path", this.ServeFile);
        }

        private IResponse ServeFile(IRequestContext request, Route routeData)
        {
            string subPath = routeData.GetPart("path") ?? String.Empty;
            string localPath = Path.Combine(_basePath, subPath.Replace("/", "\\"));

            // If we are being asked to serve a directory, look for the default file (e.g. index.html) 
            if (Directory.Exists(localPath))
            {
                localPath = Path.Combine(localPath, _defaultFile);
            }

            if (!File.Exists(localPath))
            {
                return Response.NotFound("File not found");
            }

            // Note: The channel will dispose of the response, so the underlying stream can be close.d 
            return new StreamResponse(GetContentTypeFromFile(localPath), File.OpenRead(localPath));
        }

        /// <summary>
        /// Gets the content type (mime type) for the specified file path. 
        /// </summary>
        /// <param name="localPath">File path to get content type for.</param>
        /// <returns>Mime type</returns>
        private string GetContentTypeFromFile(string localPath)
        {
            string contentType;
            string fileExtension = Path.GetExtension(localPath).ToLower();

            if (_contentTypeCache.TryGetValue(fileExtension, out contentType))
            {
                return contentType;
            }

            contentType = "application/octet-stream"; // Default if registry lookup fails. 

            using (Microsoft.Win32.RegistryKey regKey = Microsoft.Win32.Registry.ClassesRoot.OpenSubKey(fileExtension))
            {
                if (regKey != null && regKey.GetValue("Content Type") != null)
                {
                    contentType = regKey.GetValue("Content Type").ToString();
                    _contentTypeCache.Add(fileExtension, contentType);
                }
            }

            return contentType;
        }
    }
}
