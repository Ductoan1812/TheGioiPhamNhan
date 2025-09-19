using System.Collections.Generic;
using UnityEngine;
using Foundation.Utils;

namespace Foundation.Architecture
{
    /// <summary>
    /// Object Pool base class - cải tiến từ pattern sẽ cần trong Performance system.
    /// Thay vì mỗi system tự tạo pool, ta có base class chung.
    /// </summary>
    public interface IPoolable
    {
        void OnSpawn();
        void OnDespawn();
    }
    
    /// <summary>
    /// Generic Object Pool
    /// </summary>
    public class ObjectPool<T> where T : Component, IPoolable
    {
        private readonly T prefab;
        private readonly Transform parent;
        private readonly Queue<T> pool = new Queue<T>();
        private readonly HashSet<T> activeObjects = new HashSet<T>();
        
        public int PoolSize => pool.Count;
        public int ActiveCount => activeObjects.Count;
        public int TotalCount => PoolSize + ActiveCount;
        
        public ObjectPool(T prefab, int initialSize = 0, Transform parent = null)
        {
            this.prefab = prefab;
            this.parent = parent;
            
            // Pre-warm pool
            for (int i = 0; i < initialSize; i++)
            {
                var obj = CreateNewObject();
                obj.gameObject.SetActive(false);
                pool.Enqueue(obj);
            }
            
            DebugUtils.Log($"[ObjectPool] Created pool for {typeof(T).Name} with {initialSize} objects");
        }
        
        /// <summary>
        /// Get object from pool
        /// </summary>
        public T Get()
        {
            T obj;
            
            if (pool.Count > 0)
            {
                obj = pool.Dequeue();
            }
            else
            {
                obj = CreateNewObject();
                DebugUtils.Log($"[ObjectPool] Pool empty, created new {typeof(T).Name}");
            }
            
            obj.gameObject.SetActive(true);
            obj.OnSpawn();
            activeObjects.Add(obj);
            
            return obj;
        }
        
        /// <summary>
        /// Return object to pool
        /// </summary>
        public void Return(T obj)
        {
            if (obj == null) return;
            
            if (!activeObjects.Contains(obj))
            {
                DebugUtils.LogWarning($"[ObjectPool] Trying to return object not from this pool: {obj.name}");
                return;
            }
            
            obj.OnDespawn();
            obj.gameObject.SetActive(false);
            
            activeObjects.Remove(obj);
            pool.Enqueue(obj);
        }
        
        /// <summary>
        /// Return all active objects to pool
        /// </summary>
        public void ReturnAll()
        {
            var activeList = new List<T>(activeObjects);
            foreach (var obj in activeList)
            {
                Return(obj);
            }
        }
        
        /// <summary>
        /// Clear pool completely
        /// </summary>
        public void Clear()
        {
            ReturnAll();
            
            while (pool.Count > 0)
            {
                var obj = pool.Dequeue();
                if (obj != null)
                    Object.Destroy(obj.gameObject);
            }
            
            DebugUtils.Log($"[ObjectPool] Cleared pool for {typeof(T).Name}");
        }
        
        private T CreateNewObject()
        {
            var obj = Object.Instantiate(prefab, parent);
            obj.name = $"{prefab.name} (Pooled)";
            return obj;
        }
    }
    
    /// <summary>
    /// Pool Manager - quản lý nhiều pools
    /// </summary>
    public class PoolManager : PersistentSingleton<PoolManager>
    {
        private readonly Dictionary<string, object> pools = new Dictionary<string, object>();
        
        [SerializeField] private Transform poolParent;
        
        protected override void OnAwakeInstance()
        {
            base.OnAwakeInstance();
            
            if (poolParent == null)
            {
                poolParent = new GameObject("Object Pools").transform;
                poolParent.SetParent(transform);
            }
        }
        
        /// <summary>
        /// Create or get existing pool
        /// </summary>
        public ObjectPool<T> GetOrCreatePool<T>(T prefab, int initialSize = 10, string poolName = null) 
            where T : Component, IPoolable
        {
            if (prefab == null)
            {
                DebugUtils.LogError("[PoolManager] Cannot create pool with null prefab");
                return null;
            }
            
            string key = poolName ?? prefab.name;
            
            if (pools.TryGetValue(key, out object existingPool))
            {
                return existingPool as ObjectPool<T>;
            }
            
            // Create parent for this pool type
            var typeParent = new GameObject($"Pool_{key}").transform;
            typeParent.SetParent(poolParent);
            
            var newPool = new ObjectPool<T>(prefab, initialSize, typeParent);
            pools[key] = newPool;
            
            DebugUtils.Log($"[PoolManager] Created pool '{key}' with {initialSize} objects");
            return newPool;
        }
        
        /// <summary>
        /// Get existing pool
        /// </summary>
        public ObjectPool<T> GetPool<T>(string poolName) where T : Component, IPoolable
        {
            if (pools.TryGetValue(poolName, out object pool))
            {
                return pool as ObjectPool<T>;
            }
            
            DebugUtils.LogWarning($"[PoolManager] Pool '{poolName}' not found");
            return null;
        }
        
        /// <summary>
        /// Remove pool
        /// </summary>
        public void RemovePool(string poolName)
        {
            if (pools.TryGetValue(poolName, out object pool))
            {
                // Clear pool if it supports it
                if (pool is ObjectPool<Component> componentPool)
                {
                    componentPool.Clear();
                }
                
                pools.Remove(poolName);
                DebugUtils.Log($"[PoolManager] Removed pool '{poolName}'");
            }
        }
        
        /// <summary>
        /// Clear all pools
        /// </summary>
        public void ClearAllPools()
        {
            foreach (var poolName in new List<string>(pools.Keys))
            {
                RemovePool(poolName);
            }
            
            DebugUtils.Log("[PoolManager] Cleared all pools");
        }
        
        protected override void OnDestroyInstance()
        {
            base.OnDestroyInstance();
            ClearAllPools();
        }
        
        #region Debug
        
        [ContextMenu("Log Pool Statistics")]
        private void LogPoolStatistics()
        {
            DebugUtils.Log($"[PoolManager] Pool Statistics ({pools.Count} pools):");
            foreach (var kvp in pools)
            {
                if (kvp.Value is ObjectPool<Component> pool)
                {
                    DebugUtils.Log($"  {kvp.Key}: Active={pool.ActiveCount}, Pooled={pool.PoolSize}, Total={pool.TotalCount}");
                }
            }
        }
        
        #endregion
    }
}