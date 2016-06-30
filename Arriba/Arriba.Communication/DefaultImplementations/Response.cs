// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Specialized;

namespace Arriba.Communication
{
    /// <summary>
    /// Default implementation of IResponse
    /// </summary>
    public class Response : IResponse
    {
        private Lazy<IWritableValueBag> _headersLazy = new Lazy<IWritableValueBag>(() => new NameValueCollectionValueBag(new NameValueCollection()));

        private static IResponse s_notHandledSingleton = new Response(ResponseStatus.NotHandled, null);
        private object _responseBody;

        public Response(ResponseStatus status)
        {
            this.Status = status;
        }

        public Response(ResponseStatus status, object body)
            : this(status)
        {
            _responseBody = body;
        }

        public ResponseStatus Status
        {
            get;
            private set;
        }

        public virtual object ResponseBody
        {
            get
            {
                return this.GetResponseBody();
            }
        }

        protected virtual object GetResponseBody()
        {
            return _responseBody;
        }

        public IWritableValueBag Headers
        {
            get
            {
                return _headersLazy.Value;
            }
        }

        public static IResponse NotHandled
        {
            get
            {
                return s_notHandledSingleton;
            }
        }

        public void AddHeader(string key, string value)
        {
            _headersLazy.Value.Add(key, value);
        }

        internal static IResponse Error(object body)
        {
            return new Response(ResponseStatus.Error, body);
        }

        internal static IResponse NotFound(object body)
        {
            return new Response(ResponseStatus.NotFound, body);
        }

        internal static IResponse NotFound()
        {
            return new Response(ResponseStatus.NotFound, null);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        { }
    }
}
