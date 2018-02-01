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
        private Dictionary<string, T> s_buildersByName;

        public IEnumerable<T> Builders
        {
            get
            {
                EnsureLoaded();
                return s_buildersByName.Values;
            }
        }

        public IEnumerable<string> SupportedNames
        {
            get
            {
                EnsureLoaded();
                return s_buildersByName.Keys;
            }
        }

        public bool TryGetBuilder(string name, out T builder)
        {
            EnsureLoaded();

            builder = default(T);
            return s_buildersByName.TryGetValue(name, out builder);
        }

        private void EnsureLoaded()
        {
            if (s_buildersByName != null) return;

            // Initialize lookup Dictionaries
            s_buildersByName = new Dictionary<string, T>(StringComparer.OrdinalIgnoreCase);

            // Add configured type providers
            foreach (T provider in InterfaceLoader.BuildAll<T>())
            {
                s_buildersByName[provider.Name] = provider;
            }
        }
    }
}
