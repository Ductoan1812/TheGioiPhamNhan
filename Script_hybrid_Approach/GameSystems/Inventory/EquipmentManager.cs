using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Foundation.Events;
using Foundation.Utils;
using Foundation.Architecture;
using GameSystems.Inventory.Core;
using GameSystems.Stats;

namespace GameSystems.Inventory
{
    /// <summary>
    /// Equipment slot types - cải tiến từ hệ thống equipment hiện tại
    /// </summary>
    public enum EquipmentSlot
    {
        None = 0,
        Weapon = 1,
        Helmet = 2,
        Armor = 3,
        Boots = 4,
        Gloves = 5,
        Ring1 = 6,
        Ring2 = 7,
        Amulet = 8,
        Belt = 9,
        Cloak = 10
    }
    
    /// <summary>
    /// EquipmentManager - quản lý hệ thống trang bị cho entities.
    /// Cải tiến từ logic equipment hiện tại với Foundation patterns.
    /// </summary>
    public class EquipmentManager : Singleton<EquipmentManager>
    {
        [Header("Equipment Configuration")]
        [SerializeField] private bool autoApplyStatBonus = true;
        [SerializeField] private bool validateEquipmentOnLoad = true;
        
        // Equipment data for all entities
        private Dictionary<string, EquipmentSet> entityEquipment = new Dictionary<string, EquipmentSet>();
        
        // Equipment slot configuration
        private static readonly Dictionary<EquipmentSlot, string> SlotNames = new Dictionary<EquipmentSlot, string>
        {
            { EquipmentSlot.Weapon, "Vũ Khí" },
            { EquipmentSlot.Helmet, "Mũ Giáp" },
            { EquipmentSlot.Armor, "Áo Giáp" },
            { EquipmentSlot.Boots, "Giày" },
            { EquipmentSlot.Gloves, "Găng Tay" },
            { EquipmentSlot.Ring1, "Nhẫn 1" },
            { EquipmentSlot.Ring2, "Nhẫn 2" },
            { EquipmentSlot.Amulet, "Bùa Hộ Mệnh" },
            { EquipmentSlot.Belt, "Thắt Lưng" },
            { EquipmentSlot.Cloak, "Áo Choàng" }
        };
        
        public static readonly EquipmentSlot[] AllSlots = Enum.GetValues(typeof(EquipmentSlot))
            .Cast<EquipmentSlot>()
            .Where(s => s != EquipmentSlot.None)
            .ToArray();
        
        // Events
        public event Action<string, EquipmentSlot, InventoryItem> OnItemEquipped;
        public event Action<string, EquipmentSlot, InventoryItem> OnItemUnequipped;
        public event Action<string> OnEquipmentChanged;
        
        protected override void Awake()
        {
            base.Awake();
            InitializeManager();
        }
        
        private void InitializeManager()
        {
            DebugUtils.Log("[EquipmentManager] Initialized equipment system");
            
            // Register for stat system events if available
            if (StatManager.HasInstance)
            {
                // Equipment stats will be managed through StatBonus system
            }
        }
        
        #region Equipment Management
        
        /// <summary>
        /// Get equipment set for entity
        /// </summary>
        public EquipmentSet GetEquipmentSet(string entityId)
        {
            if (string.IsNullOrEmpty(entityId)) return null;
            
            if (!entityEquipment.TryGetValue(entityId, out var equipSet))
            {
                equipSet = new EquipmentSet(entityId);
                entityEquipment[entityId] = equipSet;
            }
            
            return equipSet;
        }
        
        /// <summary>
        /// Equip item to slot
        /// </summary>
        public bool TryEquipItem(string entityId, InventoryItem item, EquipmentSlot slot)
        {
            if (string.IsNullOrEmpty(entityId) || item == null || item.IsEmpty)
                return false;
            
            if (!CanEquipItemToSlot(item, slot))
            {
                DebugUtils.LogWarning($"[EquipmentManager] Cannot equip {item.ItemId} to {slot}");
                return false;
            }
            
            var equipSet = GetEquipmentSet(entityId);
            var previousItem = equipSet.GetEquippedItem(slot);
            
            // Unequip previous item if exists
            if (previousItem != null && !previousItem.IsEmpty)
            {
                TryUnequipItem(entityId, slot);
            }
            
            // Equip new item
            if (equipSet.EquipItem(slot, item))
            {
                // Apply stat bonuses
                if (autoApplyStatBonus)
                {
                    ApplyItemStatBonuses(entityId, item, true);
                }
                
                OnItemEquipped?.Invoke(entityId, slot, item);
                OnEquipmentChanged?.Invoke(entityId);
                
                EventBus.Dispatch(new ItemEquippedEvent(entityId, slot, item));
                
                DebugUtils.Log($"[EquipmentManager] Equipped {item.ItemId} to {slot} for {entityId}");
                return true;
            }
            
            return false;
        }
        
        /// <summary>
        /// Unequip item from slot
        /// </summary>
        public InventoryItem TryUnequipItem(string entityId, EquipmentSlot slot)
        {
            if (string.IsNullOrEmpty(entityId)) return null;
            
            var equipSet = GetEquipmentSet(entityId);
            var item = equipSet.UnequipItem(slot);
            
            if (item != null && !item.IsEmpty)
            {
                // Remove stat bonuses
                if (autoApplyStatBonus)
                {
                    ApplyItemStatBonuses(entityId, item, false);
                }
                
                OnItemUnequipped?.Invoke(entityId, slot, item);
                OnEquipmentChanged?.Invoke(entityId);
                
                EventBus.Dispatch(new ItemUnequippedEvent(entityId, slot, item));
                
                DebugUtils.Log($"[EquipmentManager] Unequipped {item.ItemId} from {slot} for {entityId}");
            }
            
            return item;
        }
        
        /// <summary>
        /// Swap equipment between slots
        /// </summary>
        public bool TrySwapEquipment(string entityId, EquipmentSlot slotA, EquipmentSlot slotB)
        {
            if (string.IsNullOrEmpty(entityId) || slotA == slotB) return false;
            
            var equipSet = GetEquipmentSet(entityId);
            var itemA = equipSet.GetEquippedItem(slotA);
            var itemB = equipSet.GetEquippedItem(slotB);
            
            // Validate swaps
            if (itemA != null && !itemA.IsEmpty && !CanEquipItemToSlot(itemA, slotB))
                return false;
            
            if (itemB != null && !itemB.IsEmpty && !CanEquipItemToSlot(itemB, slotA))
                return false;
            
            // Perform swap
            equipSet.SwapEquipment(slotA, slotB);
            
            OnEquipmentChanged?.Invoke(entityId);
            EventBus.Dispatch(new EquipmentSwappedEvent(entityId, slotA, slotB));
            
            return true;
        }
        
        /// <summary>
        /// Unequip all items
        /// </summary>
        public List<InventoryItem> UnequipAll(string entityId)
        {
            var unequippedItems = new List<InventoryItem>();
            
            foreach (var slot in AllSlots)
            {
                var item = TryUnequipItem(entityId, slot);
                if (item != null && !item.IsEmpty)
                {
                    unequippedItems.Add(item);
                }
            }
            
            return unequippedItems;
        }
        
        #endregion
        
        #region Validation & Queries
        
        /// <summary>
        /// Check if item can be equipped to slot
        /// </summary>
        public bool CanEquipItemToSlot(InventoryItem item, EquipmentSlot slot)
        {
            if (item == null || item.IsEmpty || item.Definition == null)
                return false;
            
            // Check if item is equipment
            if (item.Definition.Category != ItemCategory.Equipment)
                return false;
            
            // Check equipment slot compatibility
            return item.Definition.IsValidForEquipmentSlot(slot);
        }
        
        /// <summary>
        /// Get equipped item in slot
        /// </summary>
        public InventoryItem GetEquippedItem(string entityId, EquipmentSlot slot)
        {
            if (string.IsNullOrEmpty(entityId)) return null;
            
            var equipSet = GetEquipmentSet(entityId);
            return equipSet.GetEquippedItem(slot);
        }
        
        /// <summary>
        /// Check if slot is equipped
        /// </summary>
        public bool IsSlotEquipped(string entityId, EquipmentSlot slot)
        {
            var item = GetEquippedItem(entityId, slot);
            return item != null && !item.IsEmpty;
        }
        
        /// <summary>
        /// Get all equipped items
        /// </summary>
        public Dictionary<EquipmentSlot, InventoryItem> GetAllEquippedItems(string entityId)
        {
            if (string.IsNullOrEmpty(entityId))
                return new Dictionary<EquipmentSlot, InventoryItem>();
            
            var equipSet = GetEquipmentSet(entityId);
            return equipSet.GetAllEquipped();
        }
        
        /// <summary>
        /// Get equipment power rating
        /// </summary>
        public float GetEquipmentPowerRating(string entityId)
        {
            var equipped = GetAllEquippedItems(entityId);
            float totalPower = 0f;
            
            foreach (var kvp in equipped)
            {
                if (kvp.Value?.Definition != null)
                {
                    totalPower += kvp.Value.Definition.GetPowerRating();
                }
            }
            
            return totalPower;
        }
        
        /// <summary>
        /// Find best slot for item
        /// </summary>
        public EquipmentSlot FindBestSlotForItem(InventoryItem item)
        {
            if (item?.Definition == null) return EquipmentSlot.None;
            
            foreach (var slot in AllSlots)
            {
                if (CanEquipItemToSlot(item, slot))
                {
                    return slot;
                }
            }
            
            return EquipmentSlot.None;
        }
        
        #endregion
        
        #region Stat Integration
        
        /// <summary>
        /// Apply/remove item stat bonuses
        /// </summary>
        private void ApplyItemStatBonuses(string entityId, InventoryItem item, bool apply)
        {
            if (!StatManager.HasInstance || item?.Definition?.ItemStats == null)
                return;
            
            var statManager = StatManager.Instance;
            var statCollection = statManager.GetEntityStats(entityId);
            
            if (statCollection == null) return;
            
            string bonusId = $"equipment_{item.GetHashCode()}";
            
            if (apply)
            {
                // Add equipment bonuses
                var bonuses = item.Definition.ItemStats.GetStatBonuses();
                foreach (var bonus in bonuses)
                {
                    statCollection.AddStatBonus(bonus.statId, new StatBonus
                    {
                        id = $"{bonusId}_{bonus.statId}",
                        source = $"Equipment: {item.Definition.Name}",
                        bonusType = bonus.bonusType,
                        value = bonus.value,
                        duration = -1f // Permanent while equipped
                    });
                }
                
                // Add resistances
                if (item.Definition.ItemResistances != null)
                {
                    var resistances = item.Definition.ItemResistances.GetResistanceBonuses();
                    foreach (var resistance in resistances)
                    {
                        statCollection.AddStatBonus(resistance.statId, new StatBonus
                        {
                            id = $"{bonusId}_{resistance.statId}",
                            source = $"Equipment: {item.Definition.Name}",
                            bonusType = resistance.bonusType,
                            value = resistance.value,
                            duration = -1f
                        });
                    }
                }
            }
            else
            {
                // Remove equipment bonuses
                var allStats = statCollection.GetAllStats();
                foreach (var stat in allStats)
                {
                    stat.RemoveBonusesBySource($"Equipment: {item.Definition.Name}");
                }
            }
        }
        
        /// <summary>
        /// Recalculate all equipment stats for entity
        /// </summary>
        public void RecalculateEquipmentStats(string entityId)
        {
            if (!StatManager.HasInstance) return;
            
            var statManager = StatManager.Instance;
            var statCollection = statManager.GetEntityStats(entityId);
            
            if (statCollection == null) return;
            
            // Remove all equipment bonuses
            var allStats = statCollection.GetAllStats();
            foreach (var stat in allStats)
            {
                stat.RemoveBonusesBySource("Equipment:");
            }
            
            // Re-apply all equipped items
            var equipped = GetAllEquippedItems(entityId);
            foreach (var kvp in equipped)
            {
                if (kvp.Value != null && !kvp.Value.IsEmpty)
                {
                    ApplyItemStatBonuses(entityId, kvp.Value, true);
                }
            }
            
            DebugUtils.Log($"[EquipmentManager] Recalculated equipment stats for {entityId}");
        }
        
        #endregion
        
        #region Serialization
        
        /// <summary>
        /// Get serializable equipment data
        /// </summary>
        public SerializableEquipmentData GetSerializableData(string entityId)
        {
            if (string.IsNullOrEmpty(entityId)) return null;
            
            var equipSet = GetEquipmentSet(entityId);
            return equipSet.GetSerializableData();
        }
        
        /// <summary>
        /// Load equipment from serializable data
        /// </summary>
        public void LoadEquipmentData(string entityId, SerializableEquipmentData data)
        {
            if (string.IsNullOrEmpty(entityId) || data == null) return;
            
            var equipSet = GetEquipmentSet(entityId);
            equipSet.LoadFromSerializableData(data);
            
            // Apply stats
            if (autoApplyStatBonus)
            {
                RecalculateEquipmentStats(entityId);
            }
            
            OnEquipmentChanged?.Invoke(entityId);
            
            DebugUtils.Log($"[EquipmentManager] Loaded equipment data for {entityId}");
        }
        
        /// <summary>
        /// Save all equipment data
        /// </summary>
        public Dictionary<string, SerializableEquipmentData> GetAllSerializableData()
        {
            var allData = new Dictionary<string, SerializableEquipmentData>();
            
            foreach (var kvp in entityEquipment)
            {
                allData[kvp.Key] = kvp.Value.GetSerializableData();
            }
            
            return allData;
        }
        
        #endregion
        
        #region Utilities
        
        /// <summary>
        /// Get slot display name
        /// </summary>
        public static string GetSlotDisplayName(EquipmentSlot slot)
        {
            return SlotNames.TryGetValue(slot, out var name) ? name : slot.ToString();
        }
        
        /// <summary>
        /// Validate all equipment
        /// </summary>
        public void ValidateAllEquipment()
        {
            if (!validateEquipmentOnLoad) return;
            
            foreach (var kvp in entityEquipment)
            {
                ValidateEntityEquipment(kvp.Key);
            }
        }
        
        /// <summary>
        /// Validate equipment for specific entity
        /// </summary>
        public int ValidateEntityEquipment(string entityId)
        {
            var equipSet = GetEquipmentSet(entityId);
            return equipSet.ValidateEquipment();
        }
        
        /// <summary>
        /// Get equipment summary
        /// </summary>
        public string GetEquipmentSummary(string entityId)
        {
            var equipped = GetAllEquippedItems(entityId);
            var summary = $"Equipment for {entityId}:\n";
            
            foreach (var slot in AllSlots)
            {
                var item = equipped.ContainsKey(slot) ? equipped[slot] : null;
                var itemName = item?.Definition?.Name ?? "Empty";
                summary += $"  {GetSlotDisplayName(slot)}: {itemName}\n";
            }
            
            summary += $"Power Rating: {GetEquipmentPowerRating(entityId):F1}";
            
            return summary;
        }
        
        #endregion
    }
    
    /// <summary>
    /// Equipment set for one entity
    /// </summary>
    [Serializable]
    public class EquipmentSet
    {
        [SerializeField] private string entityId;
        [SerializeField] private SerializableDict<EquipmentSlot, InventoryItem> equippedItems;
        
        public string EntityId => entityId;
        
        public EquipmentSet(string entityId)
        {
            this.entityId = entityId;
            this.equippedItems = new SerializableDict<EquipmentSlot, InventoryItem>();
            
            // Initialize all slots as empty
            foreach (var slot in EquipmentManager.AllSlots)
            {
                equippedItems[slot] = new InventoryItem();
            }
        }
        
        public bool EquipItem(EquipmentSlot slot, InventoryItem item)
        {
            if (item == null) return false;
            
            equippedItems[slot] = item;
            return true;
        }
        
        public InventoryItem UnequipItem(EquipmentSlot slot)
        {
            if (!equippedItems.TryGetValue(slot, out var item))
                return null;
            
            equippedItems[slot] = new InventoryItem();
            return item;
        }
        
        public InventoryItem GetEquippedItem(EquipmentSlot slot)
        {
            equippedItems.TryGetValue(slot, out var item);
            return item;
        }
        
        public void SwapEquipment(EquipmentSlot slotA, EquipmentSlot slotB)
        {
            var itemA = GetEquippedItem(slotA);
            var itemB = GetEquippedItem(slotB);
            
            equippedItems[slotA] = itemB ?? new InventoryItem();
            equippedItems[slotB] = itemA ?? new InventoryItem();
        }
        
        public Dictionary<EquipmentSlot, InventoryItem> GetAllEquipped()
        {
            var result = new Dictionary<EquipmentSlot, InventoryItem>();
            
            foreach (var kvp in equippedItems)
            {
                if (kvp.Value != null && !kvp.Value.IsEmpty)
                {
                    result[kvp.Key] = kvp.Value;
                }
            }
            
            return result;
        }
        
        public int ValidateEquipment()
        {
            int removedCount = 0;
            
            foreach (var slot in EquipmentManager.AllSlots)
            {
                var item = GetEquippedItem(slot);
                if (item != null && !item.IsEmpty)
                {
                    if (!item.IsValidItem() || !EquipmentManager.Instance.CanEquipItemToSlot(item, slot))
                    {
                        UnequipItem(slot);
                        removedCount++;
                        DebugUtils.LogWarning($"[EquipmentSet] Removed invalid equipment from {slot} for {entityId}");
                    }
                }
            }
            
            return removedCount;
        }
        
        public SerializableEquipmentData GetSerializableData()
        {
            var itemsData = new List<SerializableEquippedItem>();
            
            foreach (var kvp in equippedItems)
            {
                if (kvp.Value != null && !kvp.Value.IsEmpty)
                {
                    itemsData.Add(new SerializableEquippedItem
                    {
                        slot = kvp.Key,
                        itemData = kvp.Value.GetSerializableData()
                    });
                }
            }
            
            return new SerializableEquipmentData
            {
                entityId = entityId,
                equippedItems = itemsData.ToArray()
            };
        }
        
        public void LoadFromSerializableData(SerializableEquipmentData data)
        {
            if (data == null) return;
            
            entityId = data.entityId;
            
            // Clear all slots
            foreach (var slot in EquipmentManager.AllSlots)
            {
                equippedItems[slot] = new InventoryItem();
            }
            
            // Load equipped items
            if (data.equippedItems != null)
            {
                foreach (var equippedItem in data.equippedItems)
                {
                    var item = new InventoryItem();
                    item.LoadFromSerializableData(equippedItem.itemData);
                    equippedItems[equippedItem.slot] = item;
                }
            }
        }
    }
    
    /// <summary>
    /// Serializable equipment data
    /// </summary>
    [Serializable]
    public class SerializableEquipmentData
    {
        public string entityId;
        public SerializableEquippedItem[] equippedItems;
    }
    
    [Serializable]
    public class SerializableEquippedItem
    {
        public EquipmentSlot slot;
        public SerializableItemData itemData;
    }
    
    /// <summary>
    /// Serializable dictionary helper
    /// </summary>
    [Serializable]
    public class SerializableDict<TKey, TValue> : Dictionary<TKey, TValue>, ISerializationCallbackReceiver
    {
        [SerializeField] private List<TKey> keys = new List<TKey>();
        [SerializeField] private List<TValue> values = new List<TValue>();
        
        public void OnBeforeSerialize()
        {
            keys.Clear();
            values.Clear();
            
            foreach (var kvp in this)
            {
                keys.Add(kvp.Key);
                values.Add(kvp.Value);
            }
        }
        
        public void OnAfterDeserialize()
        {
            Clear();
            
            if (keys.Count != values.Count)
            {
                DebugUtils.LogWarning($"[SerializableDict] Key/Value count mismatch: {keys.Count} keys, {values.Count} values");
                return;
            }
            
            for (int i = 0; i < keys.Count; i++)
            {
                Add(keys[i], values[i]);
            }
        }
    }
}
