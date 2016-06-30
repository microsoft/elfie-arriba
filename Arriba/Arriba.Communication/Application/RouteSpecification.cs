// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Text.RegularExpressions;

namespace Arriba.Communication.Application
{
    /// <summary>
    /// Complex specification of route matching.
    /// </summary>
    public class RouteSpecification
    {
        public RouteSpecification(string route, params UrlParameter[] urlParameters)
        {
            this.ResourceRoute = route;
            this.UrlParameters = urlParameters;
        }

        public UrlParameter[] UrlParameters { get; private set; }

        public string ResourceRoute { get; private set; }
    }

    /// <summary>
    /// Route matching specication for url parameters 
    /// </summary>
    public class UrlParameter
    {
        private Regex _valueRegex = null;

        public UrlParameter(string key)
            : this(key, null)
        {
        }

        public UrlParameter(string key, string value)
            : this(key, value, false)
        {
        }

        public UrlParameter(string key, string value, bool isRegex)
        {
            this.Key = key;
            this.Value = value;
            this.ValueIsRegex = isRegex;

            if (isRegex)
            {
                _valueRegex = new Regex(value, RegexOptions.Compiled | RegexOptions.IgnoreCase);
            }
        }

        public string Key { get; private set; }

        public string Value { get; private set; }

        private bool ValueIsRegex { get; set; }

        internal bool ValueMatches(string value)
        {
            if (this.Value == null)
            {
                // Nothing defined == match any 
                return true;
            }
            else if (_valueRegex == null)
            {
                // Case insenstive match
                return String.Equals(value, this.Value, StringComparison.OrdinalIgnoreCase);
            }
            else if (value == null)
            {
                // Empty value passed, but attempted to use regex 
                return false;
            }

            // Use Regex
            return _valueRegex.IsMatch(value);
        }
    }
}
