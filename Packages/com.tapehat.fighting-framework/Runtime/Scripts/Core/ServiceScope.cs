using System;
using System.Collections.Generic;
using UnityEngine;

namespace FightingFramework.Core
{
    public abstract class ServiceScope : MonoBehaviour
    {
        private static ServiceScope current;
        private readonly Dictionary<Type, object> services = new Dictionary<Type, object>();
        
        protected virtual void Awake()
        {
            if (current != null && current != this)
            {
                Debug.LogWarning($"Multiple ServiceScope instances found. Destroying {gameObject.name}");
                Destroy(gameObject);
                return;
            }
            
            current = this;
            ConfigureServices();
        }
        
        protected virtual void OnDestroy()
        {
            if (current == this)
            {
                current = null;
                services.Clear();
            }
        }
        
        protected abstract void ConfigureServices();
        
        public static T Get<T>() where T : class
        {
            if (current?.services.TryGetValue(typeof(T), out var service) == true)
            {
                return service as T;
            }
            
            throw new InvalidOperationException($"Service {typeof(T).Name} not registered in current ServiceScope");
        }
        
        public static bool TryGet<T>(out T service) where T : class
        {
            service = null;
            
            if (current?.services.TryGetValue(typeof(T), out var foundService) == true)
            {
                service = foundService as T;
                return service != null;
            }
            
            return false;
        }
        
        public static bool IsRegistered<T>() where T : class
        {
            return current?.services.ContainsKey(typeof(T)) == true;
        }
        
        protected void Register<T>(T service) where T : class
        {
            if (service == null)
            {
                Debug.LogError($"Cannot register null service of type {typeof(T).Name}");
                return;
            }
            
            var serviceType = typeof(T);
            if (services.ContainsKey(serviceType))
            {
                Debug.LogWarning($"Service {serviceType.Name} is already registered. Overwriting...");
            }
            
            services[serviceType] = service;
        }
        
        protected void Unregister<T>() where T : class
        {
            services.Remove(typeof(T));
        }
        
        protected void RegisterInterface<TInterface, TImplementation>(TImplementation service) 
            where TInterface : class 
            where TImplementation : class, TInterface
        {
            Register<TInterface>(service);
        }
    }
}