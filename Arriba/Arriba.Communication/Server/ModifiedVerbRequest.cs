// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace Arriba.Communication
{
    /// <summary>
    /// Modifies the request verb for a request.
    /// </summary>
    internal class ModifiedVerbRequest : IRequest
    {
        private RequestVerb _verb;
        private IRequest _request;

        public ModifiedVerbRequest(IRequest request, RequestVerb verb)
        {
            _request = request;
            _verb = verb;
        }

        public RequestVerb Method
        {
            get
            {
                return _verb;
            }
        }

        public string Resource
        {
            get
            {
                return _request.Resource;
            }
        }

        public System.Security.Principal.IPrincipal User
        {
            get
            {
                return _request.User;
            }
        }

        public bool HasBody
        {
            get
            {
                return _request.HasBody;
            }
        }

        public Task<T> ReadBodyAsync<T>()
        {
            return _request.ReadBodyAsync<T>();
        }


        public IValueBag ResourceParameters
        {
            get
            {
                return _request.ResourceParameters;
            }
        }

        public IValueBag Headers
        {
            get
            {
                return _request.Headers;
            }
        }

        public Stream InputStream
        {
            get { return _request.InputStream; }
        }


        public IEnumerable<string> AcceptedResponseTypes
        {
            get { return _request.AcceptedResponseTypes; }
        }


        public string Origin
        {
            get { return _request.Origin; }
        }
    }
}
