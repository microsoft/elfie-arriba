// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Arriba.Communication;

namespace Arriba.Server
{
    public class ArribaResponse : Response<ArribaResponseEnvelope>
    {
        private ArribaResponseEnvelope _envelope = null;

        private ArribaResponse(ResponseStatus status, object body)
            : base(status)
        {
            _envelope = new ArribaResponseEnvelope(success: status == ResponseStatus.Ok, content: body);
        }

        protected override object GetResponseBody()
        {
            return _envelope;
        }

        internal static ArribaResponse Ok(object body)
        {
            return new ArribaResponse(ResponseStatus.Ok, body);
        }

        internal static ArribaResponse Created(object body)
        {
            return new ArribaResponse(ResponseStatus.Created, body);
        }

        internal static ArribaResponse Forbidden(object body)
        {
            return new ArribaResponse(ResponseStatus.Forbidden, body);
        }

        internal static ArribaResponse BadRequest(string format, params object[] args)
        {
            return new ArribaResponse(ResponseStatus.Error, string.Format(format, args));
        }

        // Replace Response.Error, Response.NotFound with ArribaResponseEnvelope-returning-versions
        internal static new ArribaResponse Error(object body)
        {
            return new ArribaResponse(ResponseStatus.Error, body);
        }

        internal static new ArribaResponse NotFound(object body)
        {
            return new ArribaResponse(ResponseStatus.NotFound, body);
        }
    }
}
