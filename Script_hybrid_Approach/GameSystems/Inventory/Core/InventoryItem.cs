using System;
using UnityEngine;
using Foundation.Utils;
using GameSystems.Stats.Core;
using GameSystems.Inventory.Core;

namespace GameSystems.Inventory.Core
{
    /// <summary>
    /// InventoryItem - enhanced từ InventoryItem hiện tại.
    /// Represents actual item instances trong inventory với quantity, overrides, etc.
    /// </summary>
    [Serializable]
    public class InventoryItem
    {
        [SerializeField] private string itemId;
        [SerializeField] private int quantity = 1;
        [SerializeField] private int slotIndex = -1;
        
        // Instance overrides (for modified items)
        [SerializeField] private ItemStats statOverrides;
        [SerializeField] private ItemAffix[] affixOverrides;
        [SerializeField] private string customName;
        [SerializeField] private string customDescription;
        
        // Runtime data
        [NonSerialized] private ItemDefinition cachedDefinition;
        [NonSerialized] private bool definitionDirty = true;
        
        // Properties
        public string ItemId { get => itemId; set => SetItemId(value); }
        public int Quantity { get => quantity; set => SetQuantity(value); }
        public int SlotIndex { get => slotIndex; set => slotIndex = value; }
        public ItemStats StatOverrides { get => statOverrides; set => statOverrides = value; }
        public ItemAffix[] AffixOverrides { get => affixOverrides; set => affixOverrides = value; }
        public string CustomName { get => customName; set => customName = value; }
        public string CustomDescription { get => customDescription; set => customDescription = value; }
        
        // Computed properties
        public bool IsEmpty => string.IsNullOrEmpty(itemId) || quantity <= 0;
        public bool IsValid => !IsEmpty && Definition != null;
        public bool HasOverrides => statOverrides != null || affixOverrides != null || !string.IsNullOrEmpty(customName);
        public string DisplayName => !string.IsNullOrEmpty(customName) ? customName : Definition?.DisplayName ?? "Unknown Item";
        public string DisplayDescription => !string.IsNullOrEmpty(customDescription) ? customDescription : Definition?.Description ?? "";
        
        // Get definition from ItemManager (cached)
        public ItemDefinition Definition
        {
            get
            {
                if (definitionDirty || cachedDefinition == null)
                {
                    cachedDefinition = ItemManager.Instance?.GetItemDefinition(itemId);
                    definitionDirty = false;
                }
                return cachedDefinition;
            }
        }
        
        // Events
        public event Action<InventoryItem> OnQuantityChanged;
        public event Action<InventoryItem> OnModified;
        
        public InventoryItem() { }
        
        public InventoryItem(string itemId, int quantity = 1)
        {
            this.itemId = itemId;
            this.quantity = quantity;
        }
        
        public InventoryItem(ItemDefinition definition, int quantity = 1)
        {
            this.itemId = definition?.Id;
            this.quantity = quantity;
            this.cachedDefinition = definition;
            this.definitionDirty = false;
        }
        
        #region Quantity Management
        
        /// <summary>
        /// Set quantity with validation
        /// </summary>
        private void SetQuantity(int newQuantity)
        {
            int oldQuantity = quantity;
            quantity = Mathf.Max(0, newQuantity);
            
            if (quantity != oldQuantity)
            {
                OnQuantityChanged?.Invoke(this);
            }
        }
        
        /// <summary>
        /// Add to quantity
        /// </summary>
        public void AddQuantity(int amount)
        {
            SetQuantity(quantity + amount);
        }
        
        /// <summary>
        /// Remove from quantity
        /// </summary>
        public bool RemoveQuantity(int amount)
        {
            if (amount > quantity) return false;
            SetQuantity(quantity - amount);
            return true;
        }
        
        /// <summary>
        /// Check if can stack with another item
        /// </summary>
        public bool CanStackWith(InventoryItem other)
        {
            if (other == null || IsEmpty || other.IsEmpty) return false;
            if (itemId != other.itemId) return false;
            if (HasOverrides || other.HasOverrides) return false; // Modified items don't stack
            return true;
        }
        
        /// <summary>
        /// Get max stack size
        /// </summary>
        public int GetMaxStackSize()
        {
            return Definition?.MaxStackSize ?? 1;
        }
        
        /// <summary>
        /// Check if stack is full
        /// </summary>
        public bool IsStackFull()
        {
            return quantity >= GetMaxStackSize();
        }
        
        /// <summary>
        /// Get remaining stack space
        /// </summary>
        public int GetRemainingStackSpace()
        {
            return Mathf.Max(0, GetMaxStackSize() - quantity);
        }
        
        #endregion
        
        #region Item ID Management
        
        private void SetItemId(string newItemId)
        {
            if (itemId != newItemId)
            {
                itemId = newItemId;
                definitionDirty = true;
                cachedDefinition = null;
            }
        }
        
        #endregion
        
        #region Stats & Bonuses
        
        /// <summary>
        /// Get effective stats (definition + overrides)
        /// </summary>
        public ItemStats GetEffectiveStats()
        {
            var baseStats = Definition?.BaseStats?.Clone() ?? new ItemStats();
            
            if (statOverrides != null)
            {
                // Add overrides to base stats
                baseStats.Attack += statOverrides.Attack;
                baseStats.Defense += statOverrides.Defense;
                baseStats.Health += statOverrides.Health;
                baseStats.Mana += statOverrides.Mana;
                baseStats.MoveSpeed += statOverrides.MoveSpeed;
                baseStats.CritRate += statOverrides.CritRate;
                baseStats.CritDamage += statOverrides.CritDamage;
                baseStats.Penetration += statOverrides.Penetration;
                baseStats.Lifesteal += statOverrides.Lifesteal;
                
                // Resistances
                if (statOverrides.Resistances != null && baseStats.Resistances != null)
                {
                    baseStats.Resistances.Metal += statOverrides.Resistances.Metal;
                    baseStats.Resistances.Wood += statOverrides.Resistances.Wood;
                    baseStats.Resistances.Water += statOverrides.Resistances.Water;
                    baseStats.Resistances.Fire += statOverrides.Resistances.Fire;
                    baseStats.Resistances.Earth += statOverrides.Resistances.Earth;
                    baseStats.Resistances.Thunder += statOverrides.Resistances.Thunder;
                    baseStats.Resistances.Dark += statOverrides.Resistances.Dark;
                    baseStats.Resistances.Light += statOverrides.Resistances.Light;
                }
            }
            
            return baseStats;
        }
        
        /// <summary>
        /// Get effective affixes (definition + overrides)
        /// </summary>
        public ItemAffix[] GetEffectiveAffixes()
        {
            var baseAffixes = Definition?.Affixes ?? Array.Empty<ItemAffix>();
            
            if (affixOverrides == null || affixOverrides.Length == 0)
                return baseAffixes;
            
            // Combine base and override affixes
            var combined = new System.Collections.Generic.List<ItemAffix>(baseAffixes);
            combined.AddRange(affixOverrides);
            return combined.ToArray();
        }
        
        /// <summary>
        /// Get all stat bonuses this item provides
        /// </summary>
        public StatBonus[] GetStatBonuses(string sourceId)
        {
            var bonuses = new System.Collections.Generic.List<StatBonus>();
            
            // Get bonuses from effective stats
            var effectiveStats = GetEffectiveStats();
            if (effectiveStats != null)
            {
                if (effectiveStats.Attack > 0)
                    bonuses.Add(new StatBonus(StatId.CongVatLy, effectiveStats.Attack, BonusType.Flat, sourceId));
                if (effectiveStats.Defense > 0)
                    bonuses.Add(new StatBonus(StatId.PhongVatLy, effectiveStats.Defense, BonusType.Flat, sourceId));
                if (effectiveStats.Health > 0)
                    bonuses.Add(new StatBonus(StatId.KhiHuyetMax, effectiveStats.Health, BonusType.Flat, sourceId));
                if (effectiveStats.Mana > 0)
                    bonuses.Add(new StatBonus(StatId.LinhLucMax, effectiveStats.Mana, BonusType.Flat, sourceId));
                if (effectiveStats.MoveSpeed > 0)
                    bonuses.Add(new StatBonus(StatId.TocDo, effectiveStats.MoveSpeed, BonusType.Flat, sourceId));
                if (effectiveStats.CritRate > 0)
                    bonuses.Add(new StatBonus(StatId.TiLeBaoKich, effectiveStats.CritRate, BonusType.Flat, sourceId));
                if (effectiveStats.CritDamage > 0)
                    bonuses.Add(new StatBonus(StatId.SatThuongBaoKich, effectiveStats.CritDamage, BonusType.Flat, sourceId));
            }
            
            // Get bonuses from effective affixes
            var effectiveAffixes = GetEffectiveAffixes();
            foreach (var affix in effectiveAffixes)
            {
                if (affix?.TryGetStatBonus(sourceId, out var affixBonus) == true)
                {
                    bonuses.Add(affixBonus);
                }
            }
            
            return bonuses.ToArray();
        }
        
        #endregion
        
        #region Item Operations
        
        /// <summary>
        /// Use/consume this item
        /// </summary>
        public bool TryUse(int amount = 1)
        {
            if (amount > quantity) return false;
            
            var useEffect = Definition?.UseEffect;
            if (useEffect != null)
            {
                // Apply use effect (delegate to UseEffect system)
                ApplyUseEffect(useEffect);
            }
            
            // Consume item
            RemoveQuantity(amount);
            return true;
        }
        
        private void ApplyUseEffect(ItemUseEffect useEffect)
        {
            // TODO: Implement use effect system
            DebugUtils.Log($"[InventoryItem] Applied use effect: {useEffect}");
        }
        
        /// <summary>
        /// Split stack into separate item
        /// </summary>
        public InventoryItem SplitStack(int splitAmount)
        {
            if (splitAmount <= 0 || splitAmount >= quantity) return null;
            
            var newItem = Clone();
            newItem.quantity = splitAmount;
            newItem.slotIndex = -1; // Will be assigned when placed
            
            RemoveQuantity(splitAmount);
            
            return newItem;
        }
        
        /// <summary>
        /// Merge with another compatible item
        /// </summary>
        public bool TryMergeWith(InventoryItem other)
        {
            if (!CanStackWith(other)) return false;
            
            int maxStack = GetMaxStackSize();
            int totalQuantity = quantity + other.quantity;
            
            if (totalQuantity <= maxStack)
            {
                // Complete merge
                AddQuantity(other.quantity);
                other.SetQuantity(0);
                return true;
            }
            else
            {
                // Partial merge
                int canTake = maxStack - quantity;
                if (canTake > 0)
                {
                    AddQuantity(canTake);
                    other.RemoveQuantity(canTake);
                    return true;
                }
            }
            
            return false;
        }
        
        #endregion
        
        #region Validation & Requirements
        
        /// <summary>
        /// Check if player can use this item
        /// </summary>
        public bool CanBeUsedBy(int playerLevel, ItemRealm playerRealm)
        {
            return Definition?.MeetsRequirements(playerLevel, playerRealm) ?? false;
        }
        
        /// <summary>
        /// Check if item can be equipped in slot
        /// </summary>
        public bool CanEquipInSlot(string slotId)
        {
            return Definition?.CanEquipInSlot(slotId) ?? false;
        }
        
        /// <summary>
        /// Validate item integrity
        /// </summary>
        public bool IsValidItem()
        {
            if (IsEmpty) return false;
            if (Definition == null) return false;
            if (quantity > GetMaxStackSize()) return false;
            return true;
        }
        
        #endregion
        
        #region Serialization & Cloning
        
        /// <summary>
        /// Clone this item
        /// </summary>
        public InventoryItem Clone()
        {
            var clone = new InventoryItem
            {
                itemId = itemId,
                quantity = quantity,
                slotIndex = slotIndex,
                statOverrides = statOverrides?.Clone(),
                affixOverrides = affixOverrides?.Clone() as ItemAffix[],
                customName = customName,
                customDescription = customDescription,
                cachedDefinition = cachedDefinition,
                definitionDirty = definitionDirty
            };
            
            return clone;
        }
        
        /// <summary>
        /// Create single item clone (quantity = 1)
        /// </summary>
        public InventoryItem CloneAsSingle()
        {
            var clone = Clone();
            clone.quantity = 1;
            clone.slotIndex = -1;
            return clone;
        }
        
        /// <summary>
        /// Get serializable data for save/load
        /// </summary>
        public SerializableItemData GetSerializableData()
        {
            return new SerializableItemData
            {
                itemId = itemId,
                quantity = quantity,
                slotIndex = slotIndex,
                statOverrides = statOverrides,
                affixOverrides = affixOverrides,
                customName = customName,
                customDescription = customDescription
            };
        }
        
        /// <summary>
        /// Load from serializable data
        /// </summary>
        public void LoadFromSerializableData(SerializableItemData data)
        {
            if (data == null) return;
            
            itemId = data.itemId;
            quantity = data.quantity;
            slotIndex = data.slotIndex;
            statOverrides = data.statOverrides;
            affixOverrides = data.affixOverrides;
            customName = data.customName;
            customDescription = data.customDescription;
            
            definitionDirty = true;
            cachedDefinition = null;
        }
        
        #endregion
        
        #region Object Overrides
        
        public override string ToString()
        {
            var name = DisplayName;
            if (quantity > 1)
                name += $" x{quantity}";
            if (HasOverrides)
                name += " (Modified)";
            return name;
        }
        
        public override bool Equals(object obj)
        {
            if (obj is InventoryItem other)
            {
                return itemId == other.itemId && 
                       quantity == other.quantity && 
                       slotIndex == other.slotIndex &&
                       HasOverrides == other.HasOverrides;
            }
            return false;
        }
        
        public override int GetHashCode()
        {
            return HashCode.Combine(itemId, quantity, slotIndex);
        }
        
        #endregion
    }
    
    /// <summary>
    /// Serializable data structure for save/load
    /// </summary>
    [Serializable]
    public class SerializableItemData
    {
        public string itemId;
        public int quantity;
        public int slotIndex;
        public ItemStats statOverrides;
        public ItemAffix[] affixOverrides;
        public string customName;
        public string customDescription;
    }
}
