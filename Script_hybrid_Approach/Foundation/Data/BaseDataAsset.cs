using System;
using UnityEngine;

namespace Foundation.Data
{
    /// <summary>
    /// Base class cho tất cả ScriptableObject data assets.
    /// Cải tiến từ ItemDatabaseSO hiện tại - thêm versioning và validation.
    /// </summary>
    public abstract class BaseDataAsset : ScriptableObject
    {
        [Header("Asset Info")]
        [SerializeField] private string assetId;
        [SerializeField] private string displayName;
        [SerializeField, TextArea(2, 4)] private string description;
        [SerializeField] private int version = 1;
        
        public string AssetId => assetId;
        public string DisplayName => displayName;
        public string Description => description;
        public int Version => version;
        
        protected virtual void OnValidate()
        {
            // Auto-generate ID if empty
            if (string.IsNullOrEmpty(assetId))
            {
                assetId = name.Replace(" ", "_").ToLower();
            }
            
            // Auto-set display name if empty
            if (string.IsNullOrEmpty(displayName))
            {
                displayName = name;
            }
            
            ValidateData();
        }
        
        protected virtual void ValidateData()
        {
            // Override in subclasses for custom validation
        }
        
        /// <summary>
        /// Called when asset is loaded/created
        /// </summary>
        public virtual void Initialize()
        {
            // Override in subclasses for initialization logic
        }
    }
    
    /// <summary>
    /// Generic base cho data collections (như ItemDatabaseSO)
    /// </summary>
    public abstract class BaseDataCollection<T> : BaseDataAsset where T : class
    {
        [Header("Collection Data")]
        [SerializeField] protected T[] items = Array.Empty<T>();
        
        public T[] Items => items;
        public int Count => items?.Length ?? 0;
        
        public virtual T GetById(string id)
        {
            // Override in subclasses with specific ID logic
            return null;
        }
        
        public virtual T[] GetByPredicate(Func<T, bool> predicate)
        {
            if (items == null || predicate == null) return Array.Empty<T>();
            
            var results = new System.Collections.Generic.List<T>();
            foreach (var item in items)
            {
                if (predicate(item))
                    results.Add(item);
            }
            return results.ToArray();
        }
        
        protected override void ValidateData()
        {
            base.ValidateData();
            
            if (items == null)
            {
                Debug.LogWarning($"[{name}] Items array is null!");
                return;
            }
            
            // Check for null items
            for (int i = 0; i < items.Length; i++)
            {
                if (items[i] == null)
                {
                    Debug.LogWarning($"[{name}] Item at index {i} is null!");
                }
            }
        }
    }
}