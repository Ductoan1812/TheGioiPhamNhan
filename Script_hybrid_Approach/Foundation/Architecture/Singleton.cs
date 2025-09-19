using UnityEngine;
using Foundation.Utils;

namespace Foundation.Architecture
{
    /// <summary>
    /// Generic Singleton base class - cải tiến từ pattern hiện tại.
    /// Thay vì mỗi class tự implement singleton, ta có base class.
    /// 
    /// Cải tiến từ:
    /// - PlayerManager.Instance
    /// - UIManager.Instance
    /// - ItemDropManager.Instance
    /// </summary>
    public abstract class Singleton<T> : MonoBehaviour where T : MonoBehaviour
    {
        private static T instance;
        private static bool isQuitting = false;
        private static readonly object lockObject = new object();
        
        public static T Instance
        {
            get
            {
                if (isQuitting)
                {
                    DebugUtils.LogWarning($"[Singleton] Instance of {typeof(T)} requested during application quit. Returning null.");
                    return null;
                }
                
                lock (lockObject)
                {
                    if (instance == null)
                    {
                        instance = FindObjectOfType<T>();
                        
                        if (instance == null)
                        {
                            GameObject singletonObject = new GameObject($"{typeof(T).Name} (Singleton)");
                            instance = singletonObject.AddComponent<T>();
                            DebugUtils.Log($"[Singleton] Created instance of {typeof(T)}");
                        }
                    }
                    
                    return instance;
                }
            }
        }
        
        public static bool HasInstance => instance != null && !isQuitting;
        
        protected virtual void Awake()
        {
            if (instance == null)
            {
                instance = this as T;
                OnAwakeInstance();
            }
            else if (instance != this)
            {
                DebugUtils.LogWarning($"[Singleton] Multiple instances of {typeof(T)} detected. Destroying duplicate.");
                Destroy(gameObject);
            }
        }
        
        protected virtual void OnDestroy()
        {
            if (instance == this)
            {
                OnDestroyInstance();
                instance = null;
            }
        }
        
        protected virtual void OnApplicationQuit()
        {
            isQuitting = true;
        }
        
        /// <summary>
        /// Called when singleton instance is created/awaken
        /// </summary>
        protected virtual void OnAwakeInstance() { }
        
        /// <summary>
        /// Called when singleton instance is destroyed
        /// </summary>
        protected virtual void OnDestroyInstance() { }
    }
    
    /// <summary>
    /// Persistent Singleton - DontDestroyOnLoad automatically
    /// </summary>
    public abstract class PersistentSingleton<T> : Singleton<T> where T : MonoBehaviour
    {
        protected override void OnAwakeInstance()
        {
            base.OnAwakeInstance();
            DontDestroyOnLoad(gameObject);
            DebugUtils.Log($"[PersistentSingleton] {typeof(T).Name} set to DontDestroyOnLoad");
        }
    }
    
    /// <summary>
    /// ScriptableObject Singleton - for data-only singletons
    /// </summary>
    public abstract class ScriptableObjectSingleton<T> : ScriptableObject where T : ScriptableObject
    {
        private static T instance;
        
        public static T Instance
        {
            get
            {
                if (instance == null)
                {
                    // Try to load from Resources
                    instance = Resources.Load<T>(typeof(T).Name);
                    
                    if (instance == null)
                    {
                        // Create runtime instance
                        instance = CreateInstance<T>();
                        DebugUtils.LogWarning($"[ScriptableObjectSingleton] No asset found for {typeof(T).Name}, created runtime instance");
                    }
                }
                
                return instance;
            }
        }
        
        public static bool HasInstance => instance != null;
        
        protected virtual void OnEnable()
        {
            if (instance == null)
                instance = this as T;
        }
    }
}