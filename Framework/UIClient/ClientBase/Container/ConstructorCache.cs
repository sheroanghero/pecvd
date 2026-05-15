using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Reflection;

namespace OpenSEMI.Core.Container
{
    internal class ConstructorCache
    {
        private static ConcurrentDictionary<Type, ConstructorInfo> _constructorCache = new ConcurrentDictionary<Type, ConstructorInfo>();

        public static ConstructorInfo GetConstructor(Type type)
        {
            ConstructorInfo constructor;

            if (!_constructorCache.TryGetValue(type, out constructor))
                constructor = _constructorCache[type] = DiscoverConstructor(type.GetTypeInfo());

            return constructor;
        }
        private static ConstructorInfo DiscoverConstructor(TypeInfo typeInfo)
        {
            var constructors = typeInfo.DeclaredConstructors;

            if (constructors.Any())
                return constructors.First();

            return null;
        }
    }
}
