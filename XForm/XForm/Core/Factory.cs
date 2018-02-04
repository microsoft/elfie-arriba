// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;

using XForm.Aggregators;
using XForm.Functions;

namespace XForm
{
    public interface INamedBuilder
    {
        /// <summary>
        ///  The name of the thing this builder constructs.
        /// </summary>
        string Name { get; }
    }

    public static class Factories
    {
        public static Factory<IFunctionBuilder> FunctionFactory = new Factory<IFunctionBuilder>();

        public static IEnumerable<string> SupportedFunctions(this Factory<IFunctionBuilder> functionFactory, Type requiredType = null)
        {
            return functionFactory.Builders.Where((fb) => (requiredType == null || fb.ReturnType == null || requiredType.Equals(fb.ReturnType))).Select((fb) => fb.Name);
        }

        public static Factory<IAggregatorBuilder> AggregatorFactory = new Factory<IAggregatorBuilder>();
    }

    public class Factory<T> where T : INamedBuilder
    {
        private Dictionary<string, T> _buildersByName;

        public IEnumerable<T> Builders
        {
            get
            {
                EnsureLoaded();
                return _buildersByName.Values;
            }
        }

        public IEnumerable<string> SupportedNames
        {
            get
            {
                EnsureLoaded();
                return _buildersByName.Keys;
            }
        }

        public bool TryGetBuilder(string name, out T builder)
        {
            EnsureLoaded();

            builder = default(T);
            return _buildersByName.TryGetValue(name, out builder);
        }

        private void EnsureLoaded()
        {
            if (_buildersByName != null) return;

            // Initialize lookup Dictionaries
            _buildersByName = new Dictionary<string, T>(StringComparer.OrdinalIgnoreCase);

            // Add configured type providers
            foreach (T provider in InterfaceLoader.BuildAll<T>())
            {
                _buildersByName[provider.Name] = provider;
            }
        }
    }
}
