using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace OpenSEMI.Core.Container
{
    public class w : IContainer
    {
        private readonly ConcurrentDictionary<Type, CachedTypeInfo> _serviceTypeLookup = new ConcurrentDictionary<Type, CachedTypeInfo>();
        private readonly ConcurrentDictionary<Type, object> _serviceInstanceLookup = new ConcurrentDictionary<Type, object>();
        private readonly ConcurrentDictionary<Type, Action<object>> _serviceTypeCallbackLookup = new ConcurrentDictionary<Type, Action<object>>();

        #region IContainer Implementation

        public void Register<TService, TImplementation>() where TImplementation : TService
        {
            _serviceTypeLookup[typeof(TService)] = new CachedTypeInfo { Type = typeof(TImplementation), IsSingleton = true };
        }

        public void Register<TService, TImplementation>(bool singleton = true) where TImplementation : TService
        {
            _serviceTypeLookup[typeof(TService)] = new CachedTypeInfo { Type = typeof(TImplementation), IsSingleton = singleton };
        }

        public void Register<TService>(Type implementationType, bool singleton = true)
        {
            if (implementationType == null)
                throw new ArgumentNullException("implementationType cannot be null.");

            _serviceTypeLookup[typeof(TService)] = new CachedTypeInfo { Type = implementationType, IsSingleton = singleton };
        }

        public void Register<TService>(Type implementationType, Action<TService> callback, bool singleton = true)
        {
            if (implementationType == null)
                throw new ArgumentNullException("serviceType cannot be null.");

            _serviceTypeLookup[typeof(TService)] = new CachedTypeInfo { Type = implementationType, IsSingleton = singleton };

            if (callback != null)
                _serviceTypeCallbackLookup[typeof(TService)] = (x) => callback((TService)x);
        }

        public void Register(Type serviceType, Type implementationType, bool singleton = true)
        {
            if (serviceType == null)
                throw new ArgumentNullException("serviceType cannot be null.");

            if (implementationType == null)
                throw new ArgumentNullException("serviceType cannot be null.");

            if (!serviceType.IsAssignableFrom(implementationType))
                throw new ArgumentException(string.Format("Service could not be registered. {0} does not implement {1}.", implementationType.Name, serviceType.Name));

            _serviceTypeLookup[serviceType] = new CachedTypeInfo { Type = implementationType, IsSingleton = singleton };
        }

        public void Register<TService>(TService instance)
        {
            if (instance == null)
                throw new ArgumentNullException("instance cannot be null.");

            _serviceInstanceLookup[typeof(TService)] = instance;
        }

        public T Resolve<T>()
        {
            return (T)Resolve(typeof(T));
        }

        private object Resolve(Type type)
        {
            CachedTypeInfo containerType;
            object instance = null;

            // If the type isn't registered, register the type to itself.
            if (!_serviceTypeLookup.TryGetValue(type, out containerType))
            {
                Register(type, type);
                containerType = new CachedTypeInfo { Type = type, IsSingleton = true };
            }

            // TODO: Should it use the instance by default? I'd assume so initially.
            // Check if the service has an instance in the list of instances, if so, return it here.
            if (_serviceInstanceLookup.TryGetValue(type, out instance))
                return instance;

            var constructor = ConstructorCache.GetConstructor(containerType.Type);
            if (constructor != null)
            {
                // Get constructor parameters.
                var parameters = ParameterCache.GetParameters(constructor);
                var parameterObjects = new List<object>();

                foreach (var parameter in parameters)
                {
                    parameterObjects.Add(Resolve(parameter.ParameterType));
                }

                var obj = Activator.CreateInstance(containerType.Type, parameterObjects.ToArray());

                Action<object> callback;
                if (_serviceTypeCallbackLookup.TryGetValue(type, out callback))
                    callback(obj);

                if (containerType.IsSingleton)
                    _serviceInstanceLookup[type] = obj;

                return obj;
            }
            else
            {
                // Return null rather than throw an exception for resolve failures.
                // This null will happen when there are 0 constructors for the supplied type.
                return null;
            }
        }

        public bool IsRegistered<TService>()
        {
            if (_serviceTypeLookup.ContainsKey(typeof(TService)) || _serviceInstanceLookup.ContainsKey(typeof(TService)))
                return true;

            return false;
        }

        #endregion

        #region IServiceProvider Implementation

        public object GetService(Type serviceType)
        {
            return Resolve(serviceType);
        }

        #endregion
    }
}
