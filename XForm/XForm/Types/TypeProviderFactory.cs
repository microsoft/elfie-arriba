using System;
using System.Collections.Generic;

namespace XForm.Types
{
    public static class TypeProviderFactory
    {
        private static Dictionary<string, ITypeProvider> _providersByName;
        private static Dictionary<Type, ITypeProvider> _providersByType;

        public static ITypeProvider TryGet(string typeName)
        {
            EnsureLoaded();

            ITypeProvider provider;
            if (!_providersByName.TryGetValue(typeName, out provider)) provider = null;
            return provider;
        }

        public static ITypeProvider TryGet(Type type)
        {
            EnsureLoaded();

            ITypeProvider provider;
            if (!_providersByType.TryGetValue(type, out provider)) provider = null;
            return provider;
        }

        private static void EnsureLoaded()
        {
            if (_providersByName != null) return;

            // Initialize lookup Dictionaries
            _providersByName = new Dictionary<string, ITypeProvider>(StringComparer.OrdinalIgnoreCase);
            _providersByType = new Dictionary<Type, ITypeProvider>();

            // Add built-in type support
            Add(new String8TypeProvider());
        }

        private static void Add(ITypeProvider provider)
        {
            _providersByName.Add(provider.Name, provider);
            _providersByType.Add(provider.Type, provider);
        }
    }
}
