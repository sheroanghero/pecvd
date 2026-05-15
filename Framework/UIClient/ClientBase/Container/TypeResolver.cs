using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Reflection;

namespace OpenSEMI.Core.Container
{
    internal static class TypeResolver
    {
        private static ConcurrentDictionary<Type, Type> _typeCache = new ConcurrentDictionary<Type, Type>();

        public static Type Resolve<T>(string className)
        {
            var type = typeof(T);
            Type implementationType = null;

            if (!_typeCache.TryGetValue(type, out implementationType))
            {
                implementationType =
                    _typeCache[type] = Resolve(className, type);
            }

            return implementationType;
        }

        public static Type Resolve(string implementingType, Type serviceType = null)
        {
            var assemblies = AppDomain.CurrentDomain.GetAssemblies();
            var types = assemblies.SelectMany(a => a.GetTypes());

            Type type = null;

            if (serviceType != null)
                type = types.FirstOrDefault(t => t.Name == implementingType && serviceType.IsAssignableFrom(t));
            else
                type = types.FirstOrDefault(t => t.Name == implementingType);

            return type;
        }
    }
}
