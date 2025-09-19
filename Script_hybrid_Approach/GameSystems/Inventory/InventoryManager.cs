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
    /// InventoryManager - quản lý tổng hợp hệ thống inventory.
    /// Cải tiến từ InventoryService hiện tại với Foundation patterns.
    /// </summary>
    public class InventoryManager : Singleton<InventoryManager>
    {
        [Header("Inventory Configuration")]
        [SerializeField] private int defaultInventorySize = 30;
        [SerializeField] private bool autoSaveInventory = true;
        [SerializeField] private float autoSaveInterval = 60f;
        [SerializeField] private bool debugMode = false;
        
        // Inventory collections for all entities
        private Dictionary<string, InventoryCollection> entityInventories = new Dictionary<string, InventoryCollection>();
        
        // Drop system
        [Header("Drop System")]
        [SerializeField] private GameObject droppedItemPrefab;
        [SerializeField] private LayerMask dropLayerMask = -1;
        [SerializeField] private float dropRadius = 1f;
        
        // Auto-save timer
        private float nextAutoSave;
        
        // Events
        public event Action<string> OnInventoryCreated;
        public event Action<string> OnInventoryDestroyed;
        public event Action<string, InventoryItem, bool> OnItemTransferred; // (entityId, item, added)
        
        protected override void Awake()
        {
            base.Awake();
            InitializeManager();
        }
        
        private void Start()
        {
            nextAutoSave = Time.time + autoSaveInterval;
        }
        
        private void Update()
        {
            if (autoSaveInventory && Time.time >= nextAutoSave)
            {
                SaveAllInventories();
                nextAutoSave = Time.time + autoSaveInterval;
            }
        }
        
        private void InitializeManager()
        {
            // Subscribe to events
            EventBus.Subscribe<ItemDroppedEvent>(OnItemDropped);
            
            DebugUtils.Log("[InventoryManager] Initialized inventory system");
        }
        
        protected override void OnDestroy()
        {
            EventBus.Unsubscribe<ItemDroppedEvent>(OnItemDropped);
            base.OnDestroy();
        }
        
        #region Inventory Management
        
        /// <summary>
        /// Get or create inventory for entity
        /// </summary>
        public InventoryCollection GetInventory(string entityId, bool createIfNotExists = true)
        {
            if (string.IsNullOrEmpty(entityId)) return null;
            
            if (!entityInventories.TryGetValue(entityId, out var inventory))
            {
                if (!createIfNotExists) return null;
                
                inventory = CreateInventory(entityId, defaultInventorySize);
            }
            
            return inventory;
        }
        
        /// <summary>
        /// Create new inventory for entity
        /// </summary>
        public InventoryCollection CreateInventory(string entityId, int maxSlots = -1)
        {
            if (string.IsNullOrEmpty(entityId)) return null;
            
            if (maxSlots <= 0) maxSlots = defaultInventorySize;
            
            var inventory = new InventoryCollection(entityId, maxSlots);
            entityInventories[entityId] = inventory;
            
            // Subscribe to inventory events
            inventory.OnItemAdded += (item, slot) => OnInventoryItemChanged(entityId, item, true);
            inventory.OnItemRemoved += (item, slot) => OnInventoryItemChanged(entityId, item, false);
            inventory.OnInventoryChanged += () => OnInventoryStateChanged(entityId);
            
            OnInventoryCreated?.Invoke(entityId);
            
            DebugUtils.Log($"[InventoryManager] Created inventory for {entityId} with {maxSlots} slots");
            return inventory;
        }
        
        /// <summary>
        /// Remove inventory for entity
        /// </summary>
        public bool RemoveInventory(string entityId, bool dropItems = false)
        {
            if (string.IsNullOrEmpty(entityId)) return false;
            
            if (!entityInventories.TryGetValue(entityId, out var inventory))
                return false;
            
            if (dropItems)
            {
                DropAllItems(entityId);
            }
            
            entityInventories.Remove(entityId);
            OnInventoryDestroyed?.Invoke(entityId);
            
            DebugUtils.Log($"[InventoryManager] Removed inventory for {entityId}");
            return true;
        }
        
        /// <summary>
        /// Check if entity has inventory
        /// </summary>
        public bool HasInventory(string entityId)
        {
            return !string.IsNullOrEmpty(entityId) && entityInventories.ContainsKey(entityId);
        }
        
        /// <summary>
        /// Get all entity IDs with inventories
        /// </summary>
        public string[] GetAllEntityIds()
        {
            return entityInventories.Keys.ToArray();
        }
        
        #endregion
        
        #region Item Operations
        
        /// <summary>
        /// Add item to entity inventory
        /// </summary>
        public (bool success, int remainingQuantity) AddItemToInventory(string entityId, string itemId, int quantity = 1, Dictionary<string, object> properties = null)
        {
            var item = ItemManager.Instance.CreateItem(itemId, quantity, properties);
            if (item == null) return (false, quantity);
            
            return AddItemToInventory(entityId, item);
        }
        
        /// <summary>
        /// Add item instance to inventory
        /// </summary>
        public (bool success, int remainingQuantity) AddItemToInventory(string entityId, InventoryItem item)
        {
            if (item == null || item.IsEmpty) return (false, 0);
            
            var inventory = GetInventory(entityId);
            if (inventory == null) return (false, item.Quantity);
            
            return inventory.TryAddItem(item);
        }
        
        /// <summary>
        /// Remove item from inventory
        /// </summary>
        public bool RemoveItemFromInventory(string entityId, string itemId, int quantity)
        {
            var inventory = GetInventory(entityId, false);
            if (inventory == null) return false;
            
            return inventory.TryRemoveItem(itemId, quantity);
        }
        
        /// <summary>
        /// Transfer item between inventories
        /// </summary>
        public bool TransferItem(string fromEntityId, string toEntityId, string itemId, int quantity)
        {
            var fromInventory = GetInventory(fromEntityId, false);
            var toInventory = GetInventory(toEntityId);
            
            if (fromInventory == null || toInventory == null) return false;
            
            // Check if source has enough items
            if (fromInventory.GetItemQuantity(itemId) < quantity) return false;
            
            // Create item to transfer
            var itemDef = ItemManager.Instance.GetItemDefinition(itemId);
            if (itemDef == null) return false;
            
            var transferItem = new InventoryItem(itemDef, quantity);
            
            // Try to add to destination first
            var addResult = toInventory.TryAddItem(transferItem);
            if (!addResult.success) return false;
            
            // Remove from source
            int actualTransferred = quantity - addResult.remainingQuantity;
            if (actualTransferred > 0)
            {
                return fromInventory.TryRemoveItem(itemId, actualTransferred);
            }
            
            return false;
        }
        
        /// <summary>
        /// Transfer specific item instance
        /// </summary>
        public bool TransferItem(string fromEntityId, string toEntityId, int fromSlot, int toSlot = -1)
        {
            var fromInventory = GetInventory(fromEntityId, false);
            var toInventory = GetInventory(toEntityId);
            
            if (fromInventory == null || toInventory == null) return false;
            
            var item = fromInventory.GetItemAtSlot(fromSlot);
            if (item.IsEmpty) return false;
            
            // Remove from source
            if (!fromInventory.ClearSlot(fromSlot)) return false;
            
            // Add to destination
            if (toSlot >= 0)
            {
                if (toInventory.IsSlotEmpty(toSlot))
                {
                    return toInventory.SetItemAtSlot(toSlot, item);
                }
                else
                {
                    // Try to stack or find another slot
                    var result = toInventory.TryAddItem(item);
                    if (!result.success)
                    {
                        // Revert - put item back in source
                        fromInventory.SetItemAtSlot(fromSlot, item);
                        return false;
                    }
                    return true;
                }
            }
            else
            {
                var result = toInventory.TryAddItem(item);
                if (!result.success)
                {
                    // Revert
                    fromInventory.SetItemAtSlot(fromSlot, item);
                    return false;
                }
                return true;
            }
        }
        
        /// <summary>
        /// Use item from inventory
        /// </summary>
        public bool UseItem(string entityId, string itemId, int quantity = 1)
        {
            var inventory = GetInventory(entityId, false);
            if (inventory == null) return false;
            
            // Find item in inventory
            var items = inventory.FindItems(itemId);
            if (items.Length == 0) return false;
            
            var item = items[0].item;
            
            // Check if item can be used
            if (item.Definition?.ItemUseEffect == null) return false;
            
            // Use item effect
            bool useSuccess = ExecuteItemUseEffect(entityId, item);
            
            if (useSuccess)
            {
                // Remove used items if they are consumable
                if (item.Definition.IsConsumable)
                {
                    inventory.TryRemoveItem(itemId, quantity);
                }
                
                EventBus.Dispatch(new ItemUsedEvent(entityId, item, quantity));
                return true;
            }
            
            return false;
        }
        
        /// <summary>
        /// Drop item from inventory
        /// </summary>
        public bool DropItem(string entityId, string itemId, int quantity, Vector3? dropPosition = null)
        {
            var inventory = GetInventory(entityId, false);
            if (inventory == null) return false;
            
            // Create item to drop
            var itemDef = ItemManager.Instance.GetItemDefinition(itemId);
            if (itemDef == null) return false;
            
            var dropItem = new InventoryItem(itemDef, quantity);
            
            // Remove from inventory
            if (!inventory.TryRemoveItem(itemId, quantity)) return false;
            
            // Drop in world
            DropItemInWorld(dropItem, dropPosition ?? Vector3.zero, entityId);
            
            return true;
        }
        
        /// <summary>
        /// Drop all items from inventory
        /// </summary>
        public int DropAllItems(string entityId, Vector3? dropPosition = null)
        {
            var inventory = GetInventory(entityId, false);
            if (inventory == null) return 0;
            
            var allItems = inventory.GetAllItems();
            int droppedCount = 0;
            
            foreach (var (item, slot) in allItems)
            {
                if (!item.IsEmpty)
                {
                    DropItemInWorld(item, dropPosition ?? Vector3.zero, entityId);
                    inventory.ClearSlot(slot);
                    droppedCount++;
                }
            }
            
            return droppedCount;
        }
        
        #endregion
        
        #region Equipment Integration
        
        /// <summary>
        /// Equip item from inventory
        /// </summary>
        public bool EquipItemFromInventory(string entityId, int inventorySlot, EquipmentSlot equipmentSlot)
        {
            if (!EquipmentManager.HasInstance) return false;
            
            var inventory = GetInventory(entityId, false);
            if (inventory == null) return false;
            
            var item = inventory.GetItemAtSlot(inventorySlot);
            if (item.IsEmpty) return false;
            
            var equipmentManager = EquipmentManager.Instance;
            
            // Check if item can be equipped
            if (!equipmentManager.CanEquipItemToSlot(item, equipmentSlot)) return false;
            
            // Get currently equipped item
            var currentEquipped = equipmentManager.GetEquippedItem(entityId, equipmentSlot);
            
            // Remove item from inventory
            inventory.ClearSlot(inventorySlot);
            
            // Equip new item
            if (equipmentManager.TryEquipItem(entityId, item, equipmentSlot))
            {
                // Add previously equipped item to inventory if exists
                if (currentEquipped != null && !currentEquipped.IsEmpty)
                {
                    var addResult = inventory.TryAddItem(currentEquipped);
                    if (!addResult.success && addResult.remainingQuantity > 0)
                    {
                        // Drop overflow items
                        DropItemInWorld(currentEquipped, Vector3.zero, entityId);
                    }
                }
                
                EventBus.Dispatch(new ItemEquippedFromInventoryEvent(entityId, item, inventorySlot, equipmentSlot));
                return true;
            }
            else
            {
                // Revert - put item back in inventory
                inventory.SetItemAtSlot(inventorySlot, item);
                return false;
            }
        }
        
        /// <summary>
        /// Unequip item to inventory
        /// </summary>
        public bool UnequipItemToInventory(string entityId, EquipmentSlot equipmentSlot)
        {
            if (!EquipmentManager.HasInstance) return false;
            
            var inventory = GetInventory(entityId);
            var equipmentManager = EquipmentManager.Instance;
            
            var item = equipmentManager.TryUnequipItem(entityId, equipmentSlot);
            if (item == null || item.IsEmpty) return false;
            
            var addResult = inventory.TryAddItem(item);
            if (addResult.success)
            {
                EventBus.Dispatch(new ItemUnequippedToInventoryEvent(entityId, item, equipmentSlot));
                return true;
            }
            else
            {
                // No space - drop item
                DropItemInWorld(item, Vector3.zero, entityId);
                return false;
            }
        }
        
        #endregion
        
        #region Queries & Validation
        
        /// <summary>
        /// Check if entity has item
        /// </summary>
        public bool HasItem(string entityId, string itemId, int minQuantity = 1)
        {
            var inventory = GetInventory(entityId, false);
            if (inventory == null) return false;
            
            return inventory.HasItem(itemId, minQuantity);
        }
        
        /// <summary>
        /// Get item quantity
        /// </summary>
        public int GetItemQuantity(string entityId, string itemId)
        {
            var inventory = GetInventory(entityId, false);
            if (inventory == null) return 0;
            
            return inventory.GetItemQuantity(itemId);
        }
        
        /// <summary>
        /// Get inventory summary
        /// </summary>
        public string GetInventorySummary(string entityId)
        {
            var inventory = GetInventory(entityId, false);
            if (inventory == null) return $"No inventory for {entityId}";
            
            var summary = $"Inventory for {entityId}:\n";
            summary += $"  Slots: {inventory.UsedSlots}/{inventory.MaxSlots}\n";
            
            var allItems = inventory.GetAllItems();
            var itemGroups = allItems
                .Where(x => !x.item.IsEmpty)
                .GroupBy(x => x.item.ItemId)
                .OrderBy(g => g.Key);
            
            foreach (var group in itemGroups)
            {
                int totalQuantity = group.Sum(x => x.item.Quantity);
                var itemName = group.First().item.Definition?.Name ?? group.Key;
                summary += $"  {itemName}: {totalQuantity}\n";
            }
            
            return summary;
        }
        
        /// <summary>
        /// Validate all inventories
        /// </summary>
        public void ValidateAllInventories()
        {
            foreach (var kvp in entityInventories)
            {
                kvp.Value.ValidateInventory();
            }
        }
        
        /// <summary>
        /// Cleanup all inventories
        /// </summary>
        public int CleanupAllInventories()
        {
            int totalCleaned = 0;
            
            foreach (var kvp in entityInventories)
            {
                totalCleaned += kvp.Value.CleanupInventory();
            }
            
            return totalCleaned;
        }
        
        #endregion
        
        #region World Drop System
        
        /// <summary>
        /// Drop item in world
        /// </summary>
        private void DropItemInWorld(InventoryItem item, Vector3 position, string dropperEntityId)
        {
            if (droppedItemPrefab == null)
            {
                DebugUtils.LogWarning("[InventoryManager] No dropped item prefab configured");
                return;
            }
            
            // Find valid drop position
            Vector3 dropPos = FindValidDropPosition(position);
            
            // Create dropped item
            var droppedObj = Instantiate(droppedItemPrefab, dropPos, Quaternion.identity);
            
            // Configure dropped item component
            var droppedItemComponent = droppedObj.GetComponent<DroppedItem>();
            if (droppedItemComponent != null)
            {
                droppedItemComponent.Initialize(item, dropperEntityId);
            }
            
            EventBus.Dispatch(new ItemDroppedInWorldEvent(item, dropPos, dropperEntityId));
            
            if (debugMode)
            {
                DebugUtils.Log($"[InventoryManager] Dropped {item.ItemId} x{item.Quantity} at {dropPos}");
            }
        }
        
        /// <summary>
        /// Find valid position to drop item
        /// </summary>
        private Vector3 FindValidDropPosition(Vector3 basePosition)
        {
            // Try original position first
            if (IsValidDropPosition(basePosition))
                return basePosition;
            
            // Try positions in circle around base position
            for (int attempts = 0; attempts < 8; attempts++)
            {
                float angle = attempts * 45f * Mathf.Deg2Rad;
                Vector3 offset = new Vector3(
                    Mathf.Cos(angle) * dropRadius,
                    0,
                    Mathf.Sin(angle) * dropRadius
                );
                
                Vector3 testPos = basePosition + offset;
                if (IsValidDropPosition(testPos))
                    return testPos;
            }
            
            // Fallback to base position
            return basePosition;
        }
        
        /// <summary>
        /// Check if position is valid for dropping
        /// </summary>
        private bool IsValidDropPosition(Vector3 position)
        {
            // Simple ground check
            return Physics.Raycast(position + Vector3.up, Vector3.down, 2f, dropLayerMask);
        }
        
        #endregion
        
        #region Item Use Effects
        
        /// <summary>
        /// Execute item use effect
        /// </summary>
        private bool ExecuteItemUseEffect(string entityId, InventoryItem item)
        {
            if (item.Definition?.ItemUseEffect == null) return false;
            
            var useEffect = item.Definition.ItemUseEffect;
            
            try
            {
                switch (useEffect.EffectType)
                {
                    case UseEffectType.RestoreHealth:
                        return ApplyHealthRestore(entityId, useEffect.Value);
                    
                    case UseEffectType.RestoreQi:
                        return ApplyQiRestore(entityId, useEffect.Value);
                    
                    case UseEffectType.BuffStats:
                        return ApplyStatBuff(entityId, useEffect);
                    
                    case UseEffectType.Custom:
                        return ExecuteCustomEffect(entityId, item, useEffect);
                    
                    default:
                        DebugUtils.LogWarning($"[InventoryManager] Unknown use effect type: {useEffect.EffectType}");
                        return false;
                }
            }
            catch (Exception ex)
            {
                DebugUtils.LogError($"[InventoryManager] Error executing item use effect: {ex.Message}");
                return false;
            }
        }
        
        private bool ApplyHealthRestore(string entityId, float amount)
        {
            // Integrate with stats system if available
            if (StatManager.HasInstance)
            {
                var statManager = StatManager.Instance;
                var statCollection = statManager.GetEntityStats(entityId);
                
                if (statCollection != null)
                {
                    var hpStat = statCollection.GetStat("hp");
                    if (hpStat != null)
                    {
                        hpStat.AddStatBonus("hp", new StatBonus
                        {
                            id = $"health_restore_{Guid.NewGuid()}",
                            source = "Item Use",
                            bonusType = BonusType.Flat,
                            value = amount,
                            duration = 0f // Instant
                        });
                        return true;
                    }
                }
            }
            
            DebugUtils.Log($"[InventoryManager] Restored {amount} health for {entityId}");
            return true;
        }
        
        private bool ApplyQiRestore(string entityId, float amount)
        {
            if (StatManager.HasInstance)
            {
                var statManager = StatManager.Instance;
                var statCollection = statManager.GetEntityStats(entityId);
                
                if (statCollection != null)
                {
                    var qiStat = statCollection.GetStat("qi");
                    if (qiStat != null)
                    {
                        qiStat.AddStatBonus("qi", new StatBonus
                        {
                            id = $"qi_restore_{Guid.NewGuid()}",
                            source = "Item Use",
                            bonusType = BonusType.Flat,
                            value = amount,
                            duration = 0f
                        });
                        return true;
                    }
                }
            }
            
            DebugUtils.Log($"[InventoryManager] Restored {amount} qi for {entityId}");
            return true;
        }
        
        private bool ApplyStatBuff(string entityId, ItemUseEffect useEffect)
        {
            // Implementation for stat buffs
            DebugUtils.Log($"[InventoryManager] Applied stat buff for {entityId}");
            return true;
        }
        
        private bool ExecuteCustomEffect(string entityId, InventoryItem item, ItemUseEffect useEffect)
        {
            // Custom effect implementation
            EventBus.Dispatch(new CustomItemUseEvent(entityId, item, useEffect));
            return true;
        }
        
        #endregion
        
        #region Serialization & Persistence
        
        /// <summary>
        /// Save all inventories
        /// </summary>
        public void SaveAllInventories()
        {
            if (!autoSaveInventory) return;
            
            var allData = new Dictionary<string, SerializableInventoryData>();
            
            foreach (var kvp in entityInventories)
            {
                allData[kvp.Key] = kvp.Value.GetSerializableData();
            }
            
            // Save to PlayerPrefs or file system
            string jsonData = JsonUtility.ToJson(new InventorySaveData { inventories = allData });
            PlayerPrefs.SetString("InventoryData", jsonData);
            
            if (debugMode)
            {
                DebugUtils.Log($"[InventoryManager] Saved {allData.Count} inventories");
            }
        }
        
        /// <summary>
        /// Load all inventories
        /// </summary>
        public void LoadAllInventories()
        {
            string jsonData = PlayerPrefs.GetString("InventoryData", "");
            if (string.IsNullOrEmpty(jsonData)) return;
            
            try
            {
                var saveData = JsonUtility.FromJson<InventorySaveData>(jsonData);
                if (saveData?.inventories != null)
                {
                    foreach (var kvp in saveData.inventories)
                    {
                        var inventory = CreateInventory(kvp.Key, kvp.Value.maxSlots);
                        inventory.LoadFromSerializableData(kvp.Value);
                    }
                }
                
                DebugUtils.Log($"[InventoryManager] Loaded {entityInventories.Count} inventories");
            }
            catch (Exception ex)
            {
                DebugUtils.LogError($"[InventoryManager] Error loading inventories: {ex.Message}");
            }
        }
        
        #endregion
        
        #region Event Handlers
        
        private void OnItemDropped(ItemDroppedEvent eventData)
        {
            if (debugMode)
            {
                DebugUtils.Log($"[InventoryManager] Item dropped: {eventData.Item.ItemId} - {eventData.Reason}");
            }
        }
        
        private void OnInventoryItemChanged(string entityId, InventoryItem item, bool added)
        {
            OnItemTransferred?.Invoke(entityId, item, added);
            
            if (debugMode)
            {
                string action = added ? "added to" : "removed from";
                DebugUtils.Log($"[InventoryManager] Item {item.ItemId} {action} {entityId}'s inventory");
            }
        }
        
        private void OnInventoryStateChanged(string entityId)
        {
            if (debugMode)
            {
                DebugUtils.Log($"[InventoryManager] Inventory state changed for {entityId}");
            }
        }
        
        #endregion
    }
    
    /// <summary>
    /// Dropped item component
    /// </summary>
    public class DroppedItem : MonoBehaviour
    {
        [SerializeField] private InventoryItem item;
        [SerializeField] private string dropperEntityId;
        [SerializeField] private float pickupDelay = 0.5f;
        [SerializeField] private float despawnTime = 300f; // 5 minutes
        
        private float dropTime;
        private bool canPickup;
        
        public InventoryItem Item => item;
        public string DropperEntityId => dropperEntityId;
        
        public void Initialize(InventoryItem droppedItem, string dropperId)
        {
            item = droppedItem;
            dropperEntityId = dropperId;
            dropTime = Time.time;
            
            // Setup visual representation
            SetupVisuals();
            
            // Schedule pickup availability
            Invoke(nameof(EnablePickup), pickupDelay);
            
            // Schedule despawn
            Invoke(nameof(Despawn), despawnTime);
        }
        
        private void SetupVisuals()
        {
            // Configure visual representation based on item
            if (item?.Definition != null)
            {
                // Set sprite, model, etc.
                var renderer = GetComponent<SpriteRenderer>();
                if (renderer != null && item.Definition.Icon != null)
                {
                    renderer.sprite = item.Definition.Icon;
                }
            }
        }
        
        private void EnablePickup()
        {
            canPickup = true;
        }
        
        private void Despawn()
        {
            if (this != null)
            {
                Destroy(gameObject);
            }
        }
        
        private void OnTriggerEnter2D(Collider2D other)
        {
            if (!canPickup) return;
            
            // Check if collector can pick up item
            string collectorId = other.GetComponent<EntityIdentifier>()?.EntityId;
            if (!string.IsNullOrEmpty(collectorId))
            {
                TryPickup(collectorId);
            }
        }
        
        public bool TryPickup(string collectorId)
        {
            if (!canPickup || item.IsEmpty) return false;
            
            var result = InventoryManager.Instance.AddItemToInventory(collectorId, item);
            if (result.success)
            {
                EventBus.Dispatch(new ItemPickedUpEvent(collectorId, item, dropperEntityId));
                Destroy(gameObject);
                return true;
            }
            
            return false;
        }
    }
    
    /// <summary>
    /// Entity identifier component
    /// </summary>
    public class EntityIdentifier : MonoBehaviour
    {
        [SerializeField] private string entityId;
        
        public string EntityId
        {
            get => string.IsNullOrEmpty(entityId) ? gameObject.name : entityId;
            set => entityId = value;
        }
    }
    
    #region Serialization Classes
    
    [Serializable]
    public class InventorySaveData
    {
        public Dictionary<string, SerializableInventoryData> inventories;
    }
    
    #endregion
}
