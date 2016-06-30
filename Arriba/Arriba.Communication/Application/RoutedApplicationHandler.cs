// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Composition;
using System.Linq;
using System.Threading.Tasks;

namespace Arriba.Communication.Application
{
    /// <summary>
    /// Application implementation that delegates to IRoutedApplication implementations. 
    /// </summary>
    [Export(typeof(IApplication))]
    internal class RoutedApplicationHandler : IApplication
    {
        private Dictionary<RequestVerb, IRouteHandler[]> _routeLookup = new Dictionary<RequestVerb, IRouteHandler[]>();
        private List<IRoutedApplication> _apps = new List<IRoutedApplication>();

        [ImportingConstructor]
        public RoutedApplicationHandler([ImportMany] IEnumerable<IRoutedApplication> routes)
        {
            _apps.AddRange(routes);

            this.UpdateRoutes();
        }

        private void UpdateRoutes()
        {
            _routeLookup.Clear();

            // Build a lookup of VERB to *sorted* set of route entries. 
            foreach (var verbSet in _apps.SelectMany(a => a.RouteEnteries).GroupBy(e => e.Matcher.Verb))
            {
                _routeLookup.Add(verbSet.Key, verbSet.OrderByDescending(v => v.Matcher.SortOrder).ToArray());
            }
        }

        public string Name
        {
            get
            {
                return "RoutedApplication(" + String.Join(", ", _apps.Select(s => s.Name)) + ")";
            }
        }

        public async Task<IResponse> TryProcessAsync(IRequestContext ctx)
        {
            Route data;

            IRouteHandler[] candidateRoutes;

            // Get the entries for the verbs
            if (_routeLookup.TryGetValue(ctx.Request.Method, out candidateRoutes) && candidateRoutes != null)
            {
                foreach (var route in candidateRoutes)
                {
                    if (route.Matcher.TryGetRouteMatch(ctx.Request.Method, ctx.Request.Resource, ctx.Request.ResourceParameters, out data))
                    {
                        var response = await route.TryHandleAsync(ctx, data);
                        if (response != null && response.Status != ResponseStatus.NotHandled)
                        {
                            return response;
                        }
                    }
                }
            }

            // DEBUG: Breakpoint here to debug route matching failures.
            return Response.NotHandled;
        }
    }
}
