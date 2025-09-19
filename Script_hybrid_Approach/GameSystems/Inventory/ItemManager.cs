using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Foundation.Events;
using Foundation.Utils;
using Foundation.Architecture;
using GameSystems.Inventory.Core;

namespace GameSystems.Inventory
{
    /// <summary>
    /// ItemManager - quản lý toàn bộ item definitions và database.
    /// Cải tiến từ ItemDatabaseSO hiện tại với Foundation patterns.
    /// </summary>
    public class ItemManager : Singleton<ItemManager>
    {
        [Header("Configuration")]
        [SerializeField] private bool enableDebugLogging = false;
        [SerializeField] private ItemDatabaseAsset defaultDatabase;
        [SerializeField] private bool loadDatabaseOnAwake = true;
        
        // Item database
        private Dictionary<string, ItemDefinition> itemDefinitions = new Dictionary<string, ItemDefinition>();
        private Dictionary<ItemCategory, List<ItemDefinition>> itemsByCategory = new Dictionary<ItemCategory, List<ItemDefinition>>();
        private Dictionary<ItemRarity, List<ItemDefinition>> itemsByRarity = new Dictionary<ItemRarity, List<ItemDefinition>>();
        
        // Events
        public event Action<ItemDefinition> OnItemRegistered;
        public event Action<string> OnItemUnregistered;
        public event Action OnDatabaseLoaded;
        
        // Properties
        public int ItemCount => itemDefinitions.Count;
        public IReadOnlyDictionary<string, ItemDefinition> AllItems => itemDefinitions;
        
        #region Unity Lifecycle
        
        protected override void Awake()
        {
            base.Awake();
            
            InitializeCategoryMaps();
            
            if (loadDatabaseOnAwake && defaultDatabase != null)
            {
                LoadDatabase(defaultDatabase);
            }
            
            DebugUtils.Log();
        }
        
        private void Start()
        {
            // Subscribe to global events if needed
            EventBus.Subscribe<ItemRequestEvent>(OnItemRequested);
            
            DebugUtils.Log();
        }
        
        protected override void OnDestroy()
        {
            EventBus.Unsubscribe<ItemRequestEvent>(OnItemRequested);
            base.OnDestroy();
        }
        
        #endregion
        
        #region Database Management
        
        /// <summary>
        /// Load item database from asset
        /// </summary>
        public bool LoadDatabase(ItemDatabaseAsset database)
        {
            if (database == null)
            {
                DebugUtils.LogError("[ItemManager] Cannot load null database");
                return false;
            }
            
            try
            {
                ClearDatabase();
                
                var items = database.GetAllItems();
                foreach (var item in items)
                {
                    RegisterItem(item);
                }
                
                DebugUtils.Log();
                OnDatabaseLoaded?.Invoke();
                EventBus.Dispatch(new ItemDatabaseLoadedEvent(ItemCount));
                
                return true;
            }
            catch (Exception ex)
            {
                DebugUtils.LogError($"[ItemManager] Failed to load database: {ex.Message}");
                return false;
            }
        }
        
        /// <summary>
        /// Load database from JSON file
        /// </summary>
        public bool LoadDatabaseFromJson(string jsonPath)
        {
            try
            {
                if (!System.IO.File.Exists(jsonPath))
                {
                    DebugUtils.LogError($"[ItemManager] JSON file not found: {jsonPath}");
                    return false;
                }
                
                string json = System.IO.File.ReadAllText(jsonPath);
                var wrapper = JsonUtility.FromJson<ItemDefinitionWrapper>(json);
                
                ClearDatabase();
                
                foreach (var item in wrapper.items)
                {
                    RegisterItem(item);
                }
                
                DebugUtils.Log();
                OnDatabaseLoaded?.Invoke();
                
                return true;
            }
            catch (Exception ex)
            {
                DebugUtils.LogError($"[ItemManager] Failed to load JSON database: {ex.Message}");
                return false;
            }
        }
        
        /// <summary>
        /// Clear all loaded items
        /// </summary>
        public void ClearDatabase()
        {
            itemDefinitions.Clear();
            InitializeCategoryMaps();
            DebugUtils.Log();
        }
        
        private void InitializeCategoryMaps()
        {
            itemsByCategory.Clear();
            itemsByRarity.Clear();
            
            foreach (ItemCategory category in Enum.GetValues(typeof(ItemCategory)))
            {
                itemsByCategory[category] = new List<ItemDefinition>();
            }
            
            foreach (ItemRarity rarity in Enum.GetValues(typeof(ItemRarity)))
            {
                itemsByRarity[rarity] = new List<ItemDefinition>();
            }
        }
        
        #endregion
        
        #region Item Registration
        
        /// <summary>
        /// Register item definition
        /// </summary>
        public bool RegisterItem(ItemDefinition item)
        {
            if (item == null)
            {
                DebugUtils.LogWarning("[ItemManager] Cannot register null item");
                return false;
            }
            
            if (string.IsNullOrEmpty(item.Id))
            {
                DebugUtils.LogWarning("[ItemManager] Cannot register item with empty ID");
                return false;
            }
            
            if (itemDefinitions.ContainsKey(item.Id))
            {
                DebugUtils.LogWarning($"[ItemManager] Item ID already exists: {item.Id}");
                return false;
            }
            
            itemDefinitions[item.Id] = item;
            itemsByCategory[item.Category].Add(item);
            itemsByRarity[item.Rarity].Add(item);
            
            OnItemRegistered?.Invoke(item);
            EventBus.Dispatch(new ItemRegisteredEvent(item));
            
            DebugUtils.Log();
            return true;
        }
        
        /// <summary>
        /// Unregister item by ID
        /// </summary>
        public bool UnregisterItem(string itemId)
        {
            if (string.IsNullOrEmpty(itemId)) return false;
            
            if (itemDefinitions.TryGetValue(itemId, out var item))
            {
                itemDefinitions.Remove(itemId);
                itemsByCategory[item.Category].Remove(item);
                itemsByRarity[item.Rarity].Remove(item);
                
                OnItemUnregistered?.Invoke(itemId);
                EventBus.Dispatch(new ItemUnregisteredEvent(itemId));
                
                DebugUtils.Log();
                return true;
            }
            
            return false;
        }
        
        /// <summary>
        /// Update existing item definition
        /// </summary>
        public bool UpdateItem(ItemDefinition updatedItem)
        {
            if (updatedItem == null || string.IsNullOrEmpty(updatedItem.Id)) return false;
            
            if (itemDefinitions.ContainsKey(updatedItem.Id))
            {
                // Remove from category lists
                var oldItem = itemDefinitions[updatedItem.Id];
                itemsByCategory[oldItem.Category].Remove(oldItem);
                itemsByRarity[oldItem.Rarity].Remove(oldItem);
                
                // Update definition
                itemDefinitions[updatedItem.Id] = updatedItem;
                itemsByCategory[updatedItem.Category].Add(updatedItem);
                itemsByRarity[updatedItem.Rarity].Add(updatedItem);
                
                EventBus.Dispatch(new ItemUpdatedEvent(updatedItem));
                DebugUtils.Log();
                return true;
            }
            
            return false;
        }
        
        #endregion
        
        #region Item Queries
        
        /// <summary>
        /// Get item definition by ID
        /// </summary>
        public ItemDefinition GetItemDefinition(string itemId)
        {
            if (string.IsNullOrEmpty(itemId)) return null;
            itemDefinitions.TryGetValue(itemId, out var item);
            return item;
        }
        
        /// <summary>
        /// Check if item exists
        /// </summary>
        public bool HasItem(string itemId)
        {
            return !string.IsNullOrEmpty(itemId) && itemDefinitions.ContainsKey(itemId);
        }
        
        /// <summary>
        /// Get items by category
        /// </summary>
        public ItemDefinition[] GetItemsByCategory(ItemCategory category)
        {
            if (itemsByCategory.TryGetValue(category, out var items))
            {
                return items.ToArray();
            }
            return Array.Empty<ItemDefinition>();
        }
        
        /// <summary>
        /// Get items by rarity
        /// </summary>
        public ItemDefinition[] GetItemsByRarity(ItemRarity rarity)
        {
            if (itemsByRarity.TryGetValue(rarity, out var items))
            {
                return items.ToArray();
            }
            return Array.Empty<ItemDefinition>();
        }
        
        /// <summary>
        /// Get items by multiple criteria
        /// </summary>
        public ItemDefinition[] GetItems(ItemCategory? category = null, ItemRarity? rarity = null, ItemElement? element = null, int? minLevel = null, int? maxLevel = null)
        {
            var query = itemDefinitions.Values.AsEnumerable();
            
            if (category.HasValue)
                query = query.Where(i => i.Category == category.Value);
            
            if (rarity.HasValue)
                query = query.Where(i => i.Rarity == rarity.Value);
            
            if (element.HasValue)
                query = query.Where(i => i.Element == element.Value);
            
            if (minLevel.HasValue)
                query = query.Where(i => i.Level >= minLevel.Value);
            
            if (maxLevel.HasValue)
                query = query.Where(i => i.Level <= maxLevel.Value);
            
            return query.ToArray();
        }
        
        /// <summary>
        /// Search items by name or ID
        /// </summary>
        public ItemDefinition[] SearchItems(string searchTerm, bool caseSensitive = false)
        {
            if (string.IsNullOrEmpty(searchTerm)) return Array.Empty<ItemDefinition>();
            
            var comparison = caseSensitive ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;
            
            return itemDefinitions.Values
                .Where(i => i.Id.Contains(searchTerm, comparison) || 
                           i.DisplayName.Contains(searchTerm, comparison) ||
                           i.Description.Contains(searchTerm, comparison))
                .ToArray();
        }
        
        /// <summary>
        /// Get random item by criteria
        /// </summary>
        public ItemDefinition GetRandomItem(ItemCategory? category = null, ItemRarity? rarity = null)
        {
            var candidates = GetItems(category, rarity);
            if (candidates.Length == 0) return null;
            
            int randomIndex = UnityEngine.Random.Range(0, candidates.Length);
            return candidates[randomIndex];
        }
        
        #endregion
        
        #region Item Creation
        
        /// <summary>
        /// Create inventory item from definition
        /// </summary>
        public InventoryItem CreateItem(string itemId, int quantity = 1)
        {
            var definition = GetItemDefinition(itemId);
            if (definition == null)
            {
                DebugUtils.LogWarning($"[ItemManager] Cannot create item - definition not found: {itemId}");
                return null;
            }
            
            return CreateItem(definition, quantity);
        }
        
        /// <summary>
        /// Create inventory item from definition
        /// </summary>
        public InventoryItem CreateItem(ItemDefinition definition, int quantity = 1)
        {
            if (definition == null) return null;
            
            var item = new InventoryItem(definition, quantity);
            
            EventBus.Dispatch(new ItemCreatedEvent(item));
            DebugUtils.Log();
            
            return item;
        }
        
        /// <summary>
        /// Create random item
        /// </summary>
        public InventoryItem CreateRandomItem(ItemCategory? category = null, ItemRarity? rarity = null, int quantity = 1)
        {
            var definition = GetRandomItem(category, rarity);
            return definition != null ? CreateItem(definition, quantity) : null;
        }
        
        #endregion
        
        #region Item Validation
        
        /// <summary>
        /// Validate item definition
        /// </summary>
        public bool ValidateItem(ItemDefinition item, out string error)
        {
            error = null;
            
            if (item == null)
            {
                error = "Item is null";
                return false;
            }
            
            if (string.IsNullOrEmpty(item.Id))
            {
                error = "Item ID is empty";
                return false;
            }
            
            if (string.IsNullOrEmpty(item.DisplayName))
            {
                error = "Item display name is empty";
                return false;
            }
            
            if (item.MaxStackSize <= 0)
            {
                error = "Max stack size must be greater than 0";
                return false;
            }
            
            if (item.Level < 1)
            {
                error = "Item level must be at least 1";
                return false;
            }
            
            return true;
        }
        
        /// <summary>
        /// Validate inventory item
        /// </summary>
        public bool ValidateInventoryItem(InventoryItem item, out string error)
        {
            error = null;
            
            if (item == null)
            {
                error = "Item is null";
                return false;
            }
            
            if (item.IsEmpty)
            {
                error = "Item is empty";
                return false;
            }
            
            var definition = GetItemDefinition(item.ItemId);
            if (definition == null)
            {
                error = $"Item definition not found: {item.ItemId}";
                return false;
            }
            
            if (item.Quantity > definition.MaxStackSize)
            {
                error = $"Item quantity ({item.Quantity}) exceeds max stack size ({definition.MaxStackSize})";
                return false;
            }
            
            return true;
        }
        
        #endregion
        
        #region Events
        
        private void OnItemRequested(ItemRequestEvent evt)
        {
            var item = CreateItem(evt.ItemId, evt.Quantity);
            if (item != null)
            {
                EventBus.Dispatch(new ItemProvidedEvent(item, evt.RequesterId));
            }
            else
            {
                EventBus.Dispatch(new ItemRequestFailedEvent(evt.ItemId, evt.RequesterId, "Item not found"));
            }
        }
        
        #endregion
        
        #region Debug & Utilities
        
        /// <summary>
        /// Get debug info
        /// </summary>
        public string GetDebugInfo()
        {
            var categoryBreakdown = itemsByCategory
                .Where(kvp => kvp.Value.Count > 0)
                .Select(kvp => $"{kvp.Key}: {kvp.Value.Count}")
                .ToArray();
            
            return $"ItemManager: {ItemCount} items loaded\nCategories: {string.Join(", ", categoryBreakdown)}";
        }
        
        /// <summary>
        /// Log all items
        /// </summary>
        public void LogAllItems()
        {
            DebugUtils.Log($"[ItemManager] All items ({ItemCount}):");
            foreach (var kvp in itemDefinitions.OrderBy(x => x.Key))
            {
                DebugUtils.Log($"  {kvp.Value}");
            }
        }
        
        /// <summary>
        /// Validate all items
        /// </summary>
        public bool ValidateAllItems()
        {
            bool allValid = true;
            
            foreach (var kvp in itemDefinitions)
            {
                if (!ValidateItem(kvp.Value, out string error))
                {
                    DebugUtils.LogError($"[ItemManager] Invalid item {kvp.Key}: {error}");
                    allValid = false;
                }
            }
            
            return allValid;
        }
        
        #endregion
    }
    
    #region Supporting Classes
    
    /// <summary>
    /// ScriptableObject wrapper for item database
    /// </summary>
    [CreateAssetMenu(fileName = "ItemDatabase", menuName = "GameSystems/Inventory/Item Database")]
    public class ItemDatabaseAsset : ScriptableObject
    {
        [SerializeField] private ItemDefinition[] items = Array.Empty<ItemDefinition>();
        
        public ItemDefinition[] GetAllItems() => items;
        
        public void SetItems(ItemDefinition[] newItems)
        {
            items = newItems ?? Array.Empty<ItemDefinition>();
        }
        
        public int ItemCount => items?.Length ?? 0;
    }
    
    /// <summary>
    /// JSON wrapper for serialization
    /// </summary>
    [Serializable]
    public class ItemDefinitionWrapper
    {
        public ItemDefinition[] items;
    }
    
    #endregion
}
