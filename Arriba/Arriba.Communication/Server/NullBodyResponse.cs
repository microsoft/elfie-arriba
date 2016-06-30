// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Arriba.Communication
{
    /// <summary>
    /// Modifies a response to have no response body.
    /// </summary>
    internal class NullBodyResponse : IResponse
    {
        private IResponse _response;

        public NullBodyResponse(IResponse response)
        {
            _response = response;
        }

        public ResponseStatus Status
        {
            get
            {
                return _response.Status;
            }
        }

        public object ResponseBody
        {
            get
            {
                return null;
            }
        }

        public bool Handled
        {
            get
            {
                return true;
            }
        }

        public IWritableValueBag Headers
        {
            get
            {
                return _response.Headers;
            }
        }

        public void AddHeader(string key, string value)
        {
            _response.Headers.Add(key, value);
        }

        public void Dispose()
        { }
    }
}
