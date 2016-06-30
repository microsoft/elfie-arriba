// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace Arriba.Communication
{
    /// <summary>
    /// Default implementation of IResponse<typeparam name="T"></typeparam>
    /// </summary>
    public class Response<T> : Response, IResponse<T>
    {
        private T _responseBody;

        public Response(ResponseStatus status)
            : base(status)
        {
        }

        public Response(ResponseStatus status, T body)
            : base(status)
        {
            _responseBody = body;
        }

        protected override object GetResponseBody()
        {
            return _responseBody;
        }

        public new T ResponseBody
        {
            get
            {
                return (T)this.GetResponseBody();
            }
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            if (!disposing) return;

            if (_responseBody != null)
            {
                if (_responseBody is IDisposable)
                {
                    ((IDisposable)_responseBody).Dispose();
                    _responseBody = default(T);
                }
            }
        }
    }
}
