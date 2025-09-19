using System;
using System.Collections.Generic;
using UnityEngine;
using Foundation.Utils;

namespace Foundation.Architecture
{
    /// <summary>
    /// Service Locator pattern - central registry cho services.
    /// Thay vì access trực tiếp singletons, ta register/resolve services.
    /// 
    /// Ví dụ:
    /// ServiceLocator.Register<IInventoryService>(inventoryService);
    /// var inventory = ServiceLocator.Resolve<IInventoryService>();
    /// </summary>
    public static class ServiceLocator
    {
        private static readonly Dictionary<Type, object> services = new Dictionary<Type, object>();
        private static readonly Dictionary<Type, Func<object>> factories = new Dictionary<Type, Func<object>>();
        private static bool isInitialized = false;
        
        #region Initialization
        
        public static void Initialize()
        {
            if (isInitialized)
            {
                DebugUtils.LogWarning("[ServiceLocator] Already initialized");
                return;
            }
            
            services.Clear();
            factories.Clear();
            isInitialized = true;
            
            DebugUtils.Log("[ServiceLocator] Initialized");
        }
        
        public static void Shutdown()
        {
            // Dispose services that implement IDisposable
            foreach (var service in services.Values)
            {
                if (service is IDisposable disposable)
                {
                    try
                    {
                        disposable.Dispose();
                    }
                    catch (Exception e)
                    {
                        DebugUtils.LogError($"[ServiceLocator] Error disposing service: {e.Message}");
                    }
                }
            }
            
            services.Clear();
            factories.Clear();
            isInitialized = false;
            
            DebugUtils.Log("[ServiceLocator] Shutdown complete");
        }
        
        #endregion
        
        #region Service Registration
        
        /// <summary>
        /// Register service instance
        /// </summary>
        public static void Register<T>(T service) where T : class
        {
            Register<T>(service, false);
        }
        
        /// <summary>
        /// Register service instance with replace option
        /// </summary>
        public static void Register<T>(T service, bool replace) where T : class
        {
            if (service == null)
            {
                DebugUtils.LogError($"[ServiceLocator] Cannot register null service for {typeof(T).Name}");
                return;
            }
            
            Type serviceType = typeof(T);
            
            if (services.ContainsKey(serviceType) && !replace)
            {
                DebugUtils.LogWarning($"[ServiceLocator] Service {serviceType.Name} already registered. Use replace=true to override.");
                return;
            }
            
            services[serviceType] = service;
            DebugUtils.Log($"[ServiceLocator] Registered service: {serviceType.Name}");
        }
        
        /// <summary>
        /// Register service factory (lazy instantiation)
        /// </summary>
        public static void RegisterFactory<T>(Func<T> factory) where T : class
        {
            if (factory == null)
            {
                DebugUtils.LogError($"[ServiceLocator] Cannot register null factory for {typeof(T).Name}");
                return;
            }
            
            Type serviceType = typeof(T);
            factories[serviceType] = () => factory();
            DebugUtils.Log($"[ServiceLocator] Registered factory: {serviceType.Name}");
        }
        
        /// <summary>
        /// Register singleton factory
        /// </summary>
        public static void RegisterSingleton<T>(Func<T> factory) where T : class
        {
            if (factory == null)
            {
                DebugUtils.LogError($"[ServiceLocator] Cannot register null singleton factory for {typeof(T).Name}");
                return;
            }
            
            T instance = null;
            RegisterFactory<T>(() =>
            {
                if (instance == null)
                    instance = factory();
                return instance;
            });
        }
        
        #endregion
        
        #region Service Resolution
        
        /// <summary>
        /// Resolve service (required)
        /// </summary>
        public static T Resolve<T>() where T : class
        {
            Type serviceType = typeof(T);
            
            // Try cached instance first
            if (services.TryGetValue(serviceType, out object service))
            {
                return service as T;
            }
            
            // Try factory
            if (factories.TryGetValue(serviceType, out Func<object> factory))
            {
                try
                {
                    service = factory();
                    if (service != null)
                    {
                        services[serviceType] = service;
                        return service as T;
                    }
                }
                catch (Exception e)
                {
                    DebugUtils.LogError($"[ServiceLocator] Error creating service {serviceType.Name}: {e.Message}");
                }
            }
            
            DebugUtils.LogError($"[ServiceLocator] Service {serviceType.Name} not found");
            return null;
        }
        
        /// <summary>
        /// Try resolve service (optional)
        /// </summary>
        public static T TryResolve<T>() where T : class
        {
            try
            {
                return Resolve<T>();
            }
            catch
            {
                return null;
            }
        }
        
        /// <summary>
        /// Check if service is registered
        /// </summary>
        public static bool IsRegistered<T>()
        {
            Type serviceType = typeof(T);
            return services.ContainsKey(serviceType) || factories.ContainsKey(serviceType);
        }
        
        #endregion
        
        #region Service Management
        
        /// <summary>
        /// Unregister service
        /// </summary>
        public static bool Unregister<T>()
        {
            Type serviceType = typeof(T);
            bool removed = false;
            
            if (services.ContainsKey(serviceType))
            {
                var service = services[serviceType];
                if (service is IDisposable disposable)
                {
                    disposable.Dispose();
                }
                services.Remove(serviceType);
                removed = true;
            }
            
            if (factories.ContainsKey(serviceType))
            {
                factories.Remove(serviceType);
                removed = true;
            }
            
            if (removed)
                DebugUtils.Log($"[ServiceLocator] Unregistered service: {serviceType.Name}");
            
            return removed;
        }
        
        /// <summary>
        /// Get all registered service types
        /// </summary>
        public static Type[] GetRegisteredTypes()
        {
            var types = new List<Type>();
            types.AddRange(services.Keys);
            types.AddRange(factories.Keys);
            return types.ToArray();
        }
        
        /// <summary>
        /// Clear all services
        /// </summary>
        public static void Clear()
        {
            Shutdown();
            Initialize();
        }
        
        #endregion
        
        #region Debug
        
        [System.Diagnostics.Conditional("UNITY_EDITOR")]
        [System.Diagnostics.Conditional("DEVELOPMENT_BUILD")]
        public static void LogRegisteredServices()
        {
            DebugUtils.Log($"[ServiceLocator] Registered Services ({services.Count + factories.Count}):");
            
            foreach (var service in services)
            {
                DebugUtils.Log($"  Instance: {service.Key.Name}");
            }
            
            foreach (var factory in factories)
            {
                DebugUtils.Log($"  Factory: {factory.Key.Name}");
            }
        }
        
        #endregion
    }
    
    /// <summary>
    /// Service initializer - helper để register services at startup
    /// </summary>
    [DefaultExecutionOrder(-1000)]
    public class ServiceInitializer : MonoBehaviour
    {
        [SerializeField] private bool initializeOnAwake = true;
        [SerializeField] private bool shutdownOnDestroy = true;
        
        private void Awake()
        {
            if (initializeOnAwake)
            {
                ServiceLocator.Initialize();
                RegisterServices();
            }
        }
        
        private void OnDestroy()
        {
            if (shutdownOnDestroy)
            {
                ServiceLocator.Shutdown();
            }
        }
        
        /// <summary>
        /// Override để register services
        /// </summary>
        protected virtual void RegisterServices()
        {
            // Override in subclasses
            DebugUtils.Log("[ServiceInitializer] No services registered (override RegisterServices method)");
        }
        
        [ContextMenu("Log Registered Services")]
        private void LogServices()
        {
            ServiceLocator.LogRegisteredServices();
        }
    }
}