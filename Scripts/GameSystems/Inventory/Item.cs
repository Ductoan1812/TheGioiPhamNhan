using System;
using UnityEngine;

namespace GameSystems.Inventory
{
    /// <summary>
    /// định nghĩa item
    /// </summary>
    public enum ItemRarity
    {
        Common,
        Uncommon,
        Rare,
        Epic,
        Legendary
    }

    /// <summary>
    /// Item types for categorization
    /// </summary>
    public enum ItemType
    {
        Consumable, 
        Equipment, 
        Weapon,
        Armor, 
        Accessory, 
        Material, 
        Quest, 
        Misc 
    }

    /// <summary>
    /// Base item class
    /// </summary>
    [Serializable]
    public abstract class Item
    {
        [SerializeField] protected string id; //mã item
        [SerializeField] protected string displayName; //tên item
        [SerializeField] protected string description; //mô tả item
        [SerializeField] protected ItemType itemType; //loại item
        [SerializeField] protected ItemRarity rarity; //chất lượng item
        [SerializeField] protected int maxStackSize = 1; //số lượng tối đa có thể stack
        [SerializeField] protected bool isConsumable; //có thể sử dụng không
        [SerializeField] protected int value; // Gold/currency value //giá trị item
        [SerializeField] protected Sprite icon;

        // Properties
        public string Id => id;
        public string DisplayName => displayName;
        public string Description { get => description; protected set => description = value; }
        public ItemType Type => itemType;
        public ItemRarity Rarity { get => rarity; protected set => rarity = value; }
        public int MaxStackSize { get => maxStackSize; protected set => maxStackSize = value; }
        public bool IsConsumable { get => isConsumable; protected set => isConsumable = value; }
        public int Value { get => value; protected set => this.value = value; }
        public Sprite Icon { get => icon; protected set => icon = value; }

        public bool IsStackable => maxStackSize > 1;

        protected Item(string id, string displayName, ItemType type, ItemRarity rarity = ItemRarity.Common)
        {
            this.id = id;
            this.displayName = displayName;
            this.itemType = type;
            this.rarity = rarity;
        }

        /// <summary>
        /// Use item (override for specific behavior)
        /// </summary>
        public virtual bool Use(object target = null)
        {
            return false;
        }

        /// <summary>
        /// Check if items are the same type and can stack
        /// </summary>
        public virtual bool CanStackWith(Item other)
        {
            return other != null && 
                   other.Id == Id && 
                   IsStackable && 
                   other.IsStackable;
        }

        /// <summary>
        /// Create a copy of this item
        /// </summary>
        public abstract Item Clone();

        public override string ToString()
        {
            return $"{displayName} ({rarity})";
        }
    }

    /// <summary>
    /// Consumable item (potions, food, etc.)
    /// </summary>
    [Serializable]
    public class ConsumableItem : Item 
    {
        [SerializeField] private int healAmount;
        [SerializeField] private int manaAmount;
        [SerializeField] private float duration;

        public int HealAmount => healAmount;
        public int ManaAmount => manaAmount;
        public float Duration => duration;

        public ConsumableItem(string id, string displayName, int healAmount = 0, int manaAmount = 0) 
            : base(id, displayName, ItemType.Consumable)
        {
            this.healAmount = healAmount;
            this.manaAmount = manaAmount;
            this.isConsumable = true;
            this.maxStackSize = 99;
        }

        public ConsumableItem(string id, string displayName, string description, ItemRarity rarity, int value, Sprite icon, int healAmount = 0, int manaAmount = 0) 
            : base(id, displayName, ItemType.Consumable, rarity)
        {
            this.healAmount = healAmount;
            this.manaAmount = manaAmount;
            this.description = description;
            this.value = value;
            this.icon = icon;
            this.isConsumable = true;
            this.maxStackSize = 99;
        }

        public override bool Use(object target = null)
        {
            // Logic for using consumable (healing, mana restoration, etc.)
            // This would typically be handled by a system that processes the item effects
            return true;
        }

        public override Item Clone()
        {
            var cloned = new ConsumableItem(id, displayName, description, rarity, value, icon, healAmount, manaAmount);
            // Note: duration is not set in constructor, would need to be added if needed
            return cloned;
        }
    }

    /// <summary>
    /// Equipment item (weapons, armor, accessories)
    /// </summary>
    [Serializable]
    public class EquipmentItem : Item
    {
        [SerializeField] private Stats.StatModifier[] statModifiers;
        [SerializeField] private int durability;
        [SerializeField] private int maxDurability;
        [SerializeField] private int requiredLevel;

        public Stats.StatModifier[] StatModifiers => statModifiers;
        public int Durability => durability;
        public int MaxDurability => maxDurability;
        public int RequiredLevel => requiredLevel;
        public bool IsBroken => durability <= 0;
        public float DurabilityPercentage => maxDurability > 0 ? (float)durability / maxDurability : 0f;

        public EquipmentItem(string id, string displayName, ItemType type, Stats.StatModifier[] modifiers = null) 
            : base(id, displayName, type)
        {
            this.statModifiers = modifiers ?? new Stats.StatModifier[0];
            this.maxStackSize = 1;
            this.maxDurability = 100;
            this.durability = maxDurability;
        }

        public EquipmentItem(string id, string displayName, string description, ItemRarity rarity, int value, Sprite icon, ItemType type, Stats.StatModifier[] modifiers = null) 
            : base(id, displayName, type, rarity)
        {
            this.statModifiers = modifiers ?? new Stats.StatModifier[0];
            this.description = description;
            this.value = value;
            this.icon = icon;
            this.maxStackSize = 1;
            this.maxDurability = 100;
            this.durability = maxDurability;
        }

        /// <summary>
        /// Damage equipment durability
        /// </summary>
        public void DamageDurability(int damage)
        {
            durability = Mathf.Max(0, durability - damage);
        }

        /// <summary>
        /// Repair equipment
        /// </summary>
        public void Repair(int amount)
        {
            durability = Mathf.Min(maxDurability, durability + amount);
        }

        /// <summary>
        /// Fully repair equipment
        /// </summary>
        public void FullRepair()
        {
            durability = maxDurability;
        }

        public override Item Clone()
        {
            var clonedModifiers = new Stats.StatModifier[statModifiers.Length];
            Array.Copy(statModifiers, clonedModifiers, statModifiers.Length);

            var cloned = new EquipmentItem(id, displayName, description, rarity, value, icon, itemType, clonedModifiers);
            // Note: durability and requiredLevel properties would need to be settable to copy them
            return cloned;
        }
    }

    /// <summary>
    /// Material item for crafting
    /// </summary>
    [Serializable]
    public class MaterialItem : Item
    {
        [SerializeField] private string materialCategory;
        [SerializeField] private int tier;

        public string MaterialCategory => materialCategory;
        public int Tier => tier;

        public MaterialItem(string id, string displayName, string category, int tier = 1) 
            : base(id, displayName, ItemType.Material)
        {
            this.materialCategory = category;
            this.tier = tier;
            this.maxStackSize = 999;
        }

        public MaterialItem(string id, string displayName, string description, ItemRarity rarity, int value, Sprite icon, string category, int tier = 1) 
            : base(id, displayName, ItemType.Material, rarity)
        {
            this.materialCategory = category;
            this.tier = tier;
            this.description = description;
            this.value = value;
            this.icon = icon;
            this.maxStackSize = 999;
        }

        public override Item Clone()
        {
            return new MaterialItem(id, displayName, description, rarity, value, icon, materialCategory, tier);
        }
    }
}
