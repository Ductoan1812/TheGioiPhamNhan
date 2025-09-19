using System;
using System.Collections.Generic;
using UnityEngine;

namespace Foundation.Architecture
{
    /// <summary>
    /// Generic object pool for performance optimization
    /// </summary>
    public class ObjectPool<T> where T : class
    {
        private readonly Queue<T> pool = new();
        private readonly Func<T> createFunc;
        private readonly Action<T> resetAction;
        private readonly Action<T> destroyAction;
        private readonly int maxSize;

        public int CountInactive => pool.Count;
        public int CountActive { get; private set; }
        public int CountTotal => CountActive + CountInactive;

        /// <summary>
        /// Constructor for object pool
        /// </summary>
        /// <param name="createFunc">Function to create new objects</param>
        /// <param name="resetAction">Action to reset objects when returned to pool</param>
        /// <param name="destroyAction">Action to destroy objects when pool is cleared</param>
        /// <param name="maxSize">Maximum pool size (-1 for unlimited)</param>
        public ObjectPool(
            Func<T> createFunc, 
            Action<T> resetAction = null, 
            Action<T> destroyAction = null, 
            int maxSize = -1)
        {
            this.createFunc = createFunc ?? throw new ArgumentNullException(nameof(createFunc));
            this.resetAction = resetAction;
            this.destroyAction = destroyAction;
            this.maxSize = maxSize;
        }

        /// <summary>
        /// Get object from pool or create new one
        /// </summary>
        public T Get()
        {
            T item;
            
            if (pool.Count > 0)
            {
                item = pool.Dequeue();
            }
            else
            {
                item = createFunc();
            }

            CountActive++;
            return item;
        }

        /// <summary>
        /// Return object to pool
        /// </summary>
        public void Return(T item)
        {
            if (item == null)
            {
                Debug.LogWarning("Attempted to return null object to pool");
                return;
            }

            CountActive--;
            
            // Reset object state
            resetAction?.Invoke(item);

            // Add to pool if under max size
            if (maxSize == -1 || pool.Count < maxSize)
            {
                pool.Enqueue(item);
            }
            else
            {
                // Pool is full, destroy the object
                destroyAction?.Invoke(item);
            }
        }

        /// <summary>
        /// Pre-populate pool with objects
        /// </summary>
        public void Prewarm(int count)
        {
            for (int i = 0; i < count; i++)
            {
                var item = createFunc();
                resetAction?.Invoke(item);
                pool.Enqueue(item);
            }
        }

        /// <summary>
        /// Clear all objects from pool
        /// </summary>
        public void Clear()
        {
            while (pool.Count > 0)
            {
                var item = pool.Dequeue();
                destroyAction?.Invoke(item);
            }
            
            CountActive = 0;
        }
    }

    /// <summary>
    /// Specialized Unity GameObject pool
    /// </summary>
    public class GameObjectPool : ObjectPool<GameObject>
    {
        private readonly GameObject prefab;
        private readonly Transform parent;

        public GameObjectPool(
            GameObject prefab, 
            Transform parent = null, 
            int maxSize = -1) 
            : base(
                () => CreateObject(prefab, parent),
                obj => ResetObject(obj),
                obj => DestroyObject(obj),
                maxSize)
        {
            this.prefab = prefab;
            this.parent = parent;
        }

        private static GameObject CreateObject(GameObject prefab, Transform parent)
        {
            var obj = UnityEngine.Object.Instantiate(prefab, parent);
            obj.SetActive(false);
            return obj;
        }

        private static void ResetObject(GameObject obj)
        {
            obj.SetActive(false);
            obj.transform.position = Vector3.zero;
            obj.transform.rotation = Quaternion.identity;
        }

        private static void DestroyObject(GameObject obj)
        {
            if (obj != null)
            {
                if (Application.isPlaying)
                    UnityEngine.Object.Destroy(obj);
                else
                    UnityEngine.Object.DestroyImmediate(obj);
            }
        }

        /// <summary>
        /// Get GameObject from pool and activate it
        /// </summary>
        public GameObject GetAndActivate()
        {
            var obj = Get();
            obj.SetActive(true);
            return obj;
        }

        /// <summary>
        /// Return GameObject to pool and deactivate it
        /// </summary>
        public void ReturnAndDeactivate(GameObject obj)
        {
            if (obj != null)
            {
                obj.SetActive(false);
                Return(obj);
            }
        }
    }
}
