// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

using Arriba.Monitoring;
using System.Collections.Specialized;
using System.Web;

namespace Arriba.Communication.Application
{
    /// <summary>
    /// Routed application that map handlers to mapped route paths. 
    /// </summary>
    public abstract class RoutedApplication<TResponse> : Arriba.Communication.Application.IRoutedApplication where TResponse : IResponse
    {
        private List<TypedRouteHandler> _routes = new List<TypedRouteHandler>();

        protected delegate Task<TResponse> RouteHandlerAsync(IRequestContext ctx, Route route);
        protected delegate TResponse RouteHandler(IRequestContext ctx, Route route);
        protected delegate TResponse RouteAfterHandler(IRequestContext ctx, TResponse response);

        protected void Get(string route, params RouteHandler[] handler)
        {
            this.ForVerb(RequestVerb.Get, route, handler);
        }

        protected void Get(RouteSpecification route, params RouteHandler[] handler)
        {
            this.ForVerb(RequestVerb.Get, route, handler);
        }

        protected void GetAsync(string route, params RouteHandlerAsync[] handler)
        {
            this.ForVerbAsync(RequestVerb.Get, route, handler);
        }

        protected void GetAsync(RouteSpecification route, params RouteHandlerAsync[] handler)
        {
            this.ForVerbAsync(RequestVerb.Get, route, handler);
        }

        protected void Put(string route, params RouteHandler[] handler)
        {
            this.ForVerb(RequestVerb.Put, route, handler);
        }

        protected void Put(RouteSpecification route, params RouteHandler[] handler)
        {
            this.ForVerb(RequestVerb.Put, route, handler);
        }

        protected void PutAsync(string route, params RouteHandlerAsync[] handler)
        {
            this.ForVerbAsync(RequestVerb.Put, route, handler);
        }

        protected void PutAsync(RouteSpecification route, params RouteHandlerAsync[] handler)
        {
            this.ForVerbAsync(RequestVerb.Put, route, handler);
        }

        protected void Post(string route, params RouteHandler[] handler)
        {
            this.ForVerb(RequestVerb.Post, route, handler);
        }

        protected void Post(RouteSpecification route, params RouteHandler[] handler)
        {
            this.ForVerb(RequestVerb.Post, route, handler);
        }

        protected void PostAsync(string route, params RouteHandlerAsync[] handler)
        {
            this.ForVerbAsync(RequestVerb.Post, route, handler);
        }

        protected void PostAsync(RouteSpecification route, params RouteHandlerAsync[] handler)
        {
            this.ForVerbAsync(RequestVerb.Post, route, handler);
        }

        protected void Delete(string route, params RouteHandler[] handler)
        {
            this.ForVerb(RequestVerb.Delete, route, handler);
        }

        protected void Delete(RouteSpecification route, params RouteHandler[] handler)
        {
            this.ForVerb(RequestVerb.Delete, route, handler);
        }

        protected void DeleteAsync(string route, params RouteHandlerAsync[] handler)
        {
            this.ForVerbAsync(RequestVerb.Delete, route, handler);
        }

        protected void DeleteAsync(RouteSpecification route, params RouteHandlerAsync[] handler)
        {
            this.ForVerbAsync(RequestVerb.Delete, route, handler);
        }

        protected void Patch(string route, params RouteHandler[] handler)
        {
            this.ForVerb(RequestVerb.Patch, route, handler);
        }

        protected void Patch(RouteSpecification route, params RouteHandler[] handler)
        {
            this.ForVerb(RequestVerb.Patch, route, handler);
        }

        protected void PatchAsync(string route, params RouteHandlerAsync[] handler)
        {
            this.ForVerbAsync(RequestVerb.Patch, route, handler);
        }

        protected void PatchAsync(RouteSpecification route, params RouteHandlerAsync[] handler)
        {
            this.ForVerbAsync(RequestVerb.Patch, route, handler);
        }

        protected void ForVerbAsync(RequestVerb requestVerb, string route, RouteHandlerAsync[] handler)
        {
            this.ForVerbAsync(requestVerb, new RouteSpecification(route), handler);
        }

        protected void ForVerb(RequestVerb requestVerb, string route, RouteHandler[] handler)
        {
            this.ForVerb(requestVerb, new RouteSpecification(route), handler);
        }

        protected void ForVerbAsync(RequestVerb verb, RouteSpecification route, IEnumerable<RouteHandlerAsync> handler)
        {
            if (verb == RequestVerb.Options || verb == RequestVerb.Head)
            {
                throw new ArgumentException(String.Format("Cannot register specific handlers for verb \"{0}\"", verb), "verb");
            }

            _routes.Add(CreateRouteEntry(new RouteMatcher(verb, route), handler));
        }

        protected void ForVerb(RequestVerb verb, RouteSpecification route, IEnumerable<RouteHandler> handlers)
        {
            var asyncHandlers = handlers.Select(h => new RouteHandlerAsync((r, d) => Task.FromResult<TResponse>(h(r, d))));
            _routes.Add(CreateRouteEntry(new RouteMatcher(verb, route), asyncHandlers));
        }

        private TypedRouteHandler CreateRouteEntry(RouteMatcher key, IEnumerable<RouteHandlerAsync> handlers)
        {
            var entry = new TypedRouteHandler(key, handlers);
            entry.Before = this.OnBeforeProcess;
            entry.After = this.OnAfterProcess;
            return entry;
        }

        protected virtual TResponse OnBeforeProcess(IRequestContext request, Route data)
        {
            return default(TResponse);
        }

        protected virtual TResponse OnAfterProcess(IRequestContext request, TResponse response)
        {
            return response;
        }

        public abstract string Name { get; }

        IEnumerable<IRouteHandler> IRoutedApplication.RouteEnteries
        {
            get
            {
                return _routes;
            }
        }

        protected async Task<NameValueCollection> ParametersFromQueryStringAndBody(IRequestContext ctx)
        {
            NameValueCollection parameters = new NameValueCollection();

            // Read parameters from query string
            foreach (var pair in ctx.Request.ResourceParameters.ValuePairs)
            {
                parameters.Add(pair.Item1, pair.Item2);
            }

            // Read parameters from body (these will override ones from the query string)
            if (ctx.Request.HasBody)
            {
                string queryStringInBody = await ctx.Request.ReadBodyAsync<string>();
                parameters.Add(HttpUtility.ParseQueryString(queryStringInBody));
            }

            return parameters;
        }

        /// <summary>
        /// IRouteHandler implementation that adopts the parent's classes generic TResponse definition. 
        /// </summary>
        [DebuggerDisplay("Key = {Key}")]
        private class TypedRouteHandler : IRouteHandler
        {
            public TypedRouteHandler(RouteMatcher key, IEnumerable<RouteHandlerAsync> handlerChain)
            {
                this.Matcher = key;
                this.Handlers = handlerChain;
            }

            public RouteMatcher Matcher { get; private set; }

            public IEnumerable<RouteHandlerAsync> Handlers { get; private set; }

            public RouteHandler Before { get; set; }

            public RouteAfterHandler After { get; set; }

            public async Task<IResponse> TryHandleAsync(IRequestContext ctx, Route data)
            {
                TResponse result;

                using (ctx.Monitor(MonitorEventLevel.Verbose, "Routing.BeforeHandler"))
                {
                    result = this.Before(ctx, data);
                }

                // OnBefore can choose to handle the request, if it does kind the main handler.
                if (result == null)
                {
                    using (ctx.Monitor(MonitorEventLevel.Verbose, "Routing.Handler"))
                    {
                        // Try each handler until we get a response that was actually handled. 
                        foreach (var handler in this.Handlers)
                        {
                            var tempResult = await handler(ctx, data);

                            // Check if it actually did something 
                            if (tempResult != null && tempResult.Status != ResponseStatus.NotHandled)
                            {
                                result = tempResult;
                                break;
                            }
                        }
                    }
                }

                if (result != null)
                {
                    using (ctx.Monitor(MonitorEventLevel.Verbose, "Routing.AfterHandler"))
                    {
                        result = this.After(ctx, result);
                    }
                }

                return result;
            }
        }
    }
}
