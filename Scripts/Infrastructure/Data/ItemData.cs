using UnityEngine;
using GameSystems.Inventory;
using GameSystems.Stats;

namespace Infrastructure.Data
{
    /// <summary>
    /// ScriptableObject for item configuration
    /// </summary>
    [CreateAssetMenu(fileName = "New Item", menuName = "Game Data/Items/Item")]
    public class ItemData : ScriptableObject
    {
        [Header("Basic Info")]
        [SerializeField] private string itemId;
        [SerializeField] private string displayName;
        [SerializeField] private string description;
        [SerializeField] private ItemType itemType;
        [SerializeField] private ItemRarity rarity = ItemRarity.Common;
        [SerializeField] private Sprite icon;
        [SerializeField] private GameObject worldPrefab;

        [Header("Stacking")]
        [SerializeField] private int maxStackSize = 1;
        [SerializeField] private bool isConsumable = false;

        [Header("Value")]
        [SerializeField] private int goldValue = 1;
        [SerializeField] private int sellValue = 1;

        // Properties
        public string ItemId => itemId;
        public string DisplayName => displayName;
        public string Description => description;
        public ItemType Type => itemType;
        public ItemRarity Rarity => rarity;
        public Sprite Icon => icon;
        public GameObject WorldPrefab => worldPrefab;
        public int MaxStackSize => maxStackSize;
        public bool IsConsumable => isConsumable;
        public int GoldValue => goldValue;
        public int SellValue => sellValue;

        /// <summary>
        /// Create runtime item instance from this data
        /// </summary>
        public virtual Item CreateItem()
        {
            switch (itemType)
            {
                case ItemType.Consumable:
                    return CreateConsumableItem();
                case ItemType.Equipment:
                case ItemType.Weapon:
                case ItemType.Armor:
                case ItemType.Accessory:
                    return CreateEquipmentItem();
                case ItemType.Material:
                    return CreateMaterialItem();
                default:
                    return CreateBasicItem();
            }
        }

        protected virtual ConsumableItem CreateConsumableItem()
        {
            var consumableData = this as ConsumableItemData;
            if (consumableData != null)
            {
                // Create with heal/mana amounts
                return new ConsumableItem(itemId, displayName, description, rarity, goldValue, icon, 
                    consumableData.HealAmount, consumableData.ManaAmount);
            }

            // Create without heal/mana amounts
            return new ConsumableItem(itemId, displayName, description, rarity, goldValue, icon, 0, 0);
        }

        protected virtual EquipmentItem CreateEquipmentItem()
        {
            var equipmentData = this as EquipmentItemData;
            StatModifier[] modifiers = null;

            if (equipmentData != null && equipmentData.StatModifiers != null)
            {
                modifiers = new StatModifier[equipmentData.StatModifiers.Length];
                for (int i = 0; i < equipmentData.StatModifiers.Length; i++)
                {
                    var modData = equipmentData.StatModifiers[i];
                    modifiers[i] = new StatModifier(modData.Value, modData.Type, modData.Order, this);
                }
            }

            return new EquipmentItem(itemId, displayName, description, rarity, goldValue, icon, itemType, modifiers);
        }

        protected virtual MaterialItem CreateMaterialItem()
        {
            var materialData = this as MaterialItemData;
            var category = materialData?.MaterialCategory ?? "Generic";
            var tier = materialData?.Tier ?? 1;

            return new MaterialItem(itemId, displayName, description, rarity, goldValue, icon, category, tier);
        }

        protected virtual Item CreateBasicItem()
        {
            // For generic items, create a simple consumable with no effects
            return new ConsumableItem(itemId, displayName, description, rarity, goldValue, icon, 0, 0);
        }

        protected virtual void OnValidate()
        {
            // Auto-generate ID if empty
            if (string.IsNullOrEmpty(itemId))
            {
                itemId = name.ToLower().Replace(" ", "_");
            }

            // Auto-set display name if empty
            if (string.IsNullOrEmpty(displayName))
            {
                displayName = name;
            }

            // Ensure sell value is not higher than gold value
            sellValue = Mathf.Min(sellValue, goldValue);
        }
    }

    /// <summary>
    /// Consumable item data
    /// </summary>
    [CreateAssetMenu(fileName = "New Consumable", menuName = "Game Data/Items/Consumable")]
    public class ConsumableItemData : ItemData
    {
        [Header("Consumable Effects")]
        [SerializeField] private int healAmount = 0;
        [SerializeField] private int manaAmount = 0;
        [SerializeField] private float duration = 0f;
        [SerializeField] private bool hasInstantEffect = true;

        public int HealAmount => healAmount;
        public int ManaAmount => manaAmount;
        public float Duration => duration;
        public bool HasInstantEffect => hasInstantEffect;

        protected override void OnValidate()
        {
            base.OnValidate();
            // Force consumable settings - these are handled in constructor
        }
    }

    /// <summary>
    /// Equipment item data
    /// </summary>
    [CreateAssetMenu(fileName = "New Equipment", menuName = "Game Data/Items/Equipment")]
    public class EquipmentItemData : ItemData
    {
        [Header("Equipment Stats")]
        [SerializeField] private StatModifierData[] statModifiers;
        [SerializeField] private int durability = 100;
        [SerializeField] private int requiredLevel = 1;

        public StatModifierData[] StatModifiers => statModifiers;
        public int Durability => durability;
        public int RequiredLevel => requiredLevel;

        protected override void OnValidate()
        {
            base.OnValidate();
            // Force equipment settings - these are handled in constructor
        }
    }

    /// <summary>
    /// Material item data
    /// </summary>
    [CreateAssetMenu(fileName = "New Material", menuName = "Game Data/Items/Material")]
    public class MaterialItemData : ItemData
    {
        [Header("Material Properties")]
        [SerializeField] private string materialCategory = "Generic";
        [SerializeField] private int tier = 1;

        public string MaterialCategory => materialCategory;
        public int Tier => tier;

        protected override void OnValidate()
        {
            base.OnValidate();
            // Force material settings - these are handled in constructor
        }
    }

    /// <summary>
    /// Serializable stat modifier data
    /// </summary>
    [System.Serializable]
    public class StatModifierData
    {
        [SerializeField] private StatType statType;
        [SerializeField] private float value;
        [SerializeField] private ModifierType modifierType;
        [SerializeField] private int order = 0;

        public StatType StatType => statType;
        public float Value => value;
        public ModifierType Type => modifierType;
        public int Order => order;
    }
}
