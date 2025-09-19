using System;
using UnityEngine;
using Foundation.Utils;
using GameSystems.Stats.Core;

namespace GameSystems.Inventory.Core
{
    /// <summary>
    /// ItemDefinition - enhanced ItemData với Foundation integration.
    /// Cải tiến từ ItemData.cs hiện tại với better structure và metadata.
    /// </summary>
    [Serializable]
    public class ItemDefinition
    {
        [SerializeField] private string id;
        [SerializeField] private string displayName;
        [SerializeField] private ItemCategory category;
        [SerializeField] private ItemRarity rarity;
        [SerializeField] private ItemElement element;
        [SerializeField] private ItemRealm realmRequirement;
        
        [SerializeField] private int level = 1;
        [SerializeField] private int maxStackSize = 1;
        [SerializeField] private int socketCount = 0;
        
        [SerializeField] private ItemStats baseStats;
        [SerializeField] private ItemAffix[] affixes;
        [SerializeField] private ItemUseEffect useEffect;
        
        [SerializeField] private string description;
        [SerializeField] private string iconAddress;
        [SerializeField] private string textureAddress;
        
        // Properties
        public string Id => id;
        public string DisplayName => displayName;
        public ItemCategory Category => category;
        public ItemRarity Rarity => rarity;
        public ItemElement Element => element;
        public ItemRealm RealmRequirement => realmRequirement;
        public int Level => level;
        public int MaxStackSize => maxStackSize;
        public int SocketCount => socketCount;
        public ItemStats BaseStats => baseStats;
        public ItemAffix[] Affixes => affixes;
        public ItemUseEffect UseEffect => useEffect;
        public string Description => description;
        public string IconAddress => iconAddress;
        public string TextureAddress => textureAddress;
        
        public ItemDefinition() { }
        
        public ItemDefinition(string id, string displayName, ItemCategory category)
        {
            this.id = id;
            this.displayName = displayName;
            this.category = category;
            this.rarity = ItemRarity.Common;
            this.element = ItemElement.None;
            this.realmRequirement = ItemRealm.PhamNhan;
            this.baseStats = new ItemStats();
            this.affixes = Array.Empty<ItemAffix>();
        }
        
        /// <summary>
        /// Check if item can be equipped in slot
        /// </summary>
        public bool CanEquipInSlot(string slotId)
        {
            return EquipmentSlotRules.IsValidForSlot(slotId, category);
        }
        
        /// <summary>
        /// Check if player meets requirements
        /// </summary>
        public bool MeetsRequirements(int playerLevel, ItemRealm playerRealm)
        {
            if (level > playerLevel) return false;
            if ((int)realmRequirement > (int)playerRealm) return false;
            return true;
        }
        
        /// <summary>
        /// Get stat bonuses this item provides
        /// </summary>
        public StatBonus[] GetStatBonuses(string sourceId)
        {
            if (baseStats == null) return Array.Empty<StatBonus>();
            
            var bonuses = new System.Collections.Generic.List<StatBonus>();
            
            // Base stats to stat bonuses
            if (baseStats.Attack > 0)
                bonuses.Add(new StatBonus(StatId.CongVatLy, baseStats.Attack, BonusType.Flat, sourceId));
            if (baseStats.Defense > 0)
                bonuses.Add(new StatBonus(StatId.PhongVatLy, baseStats.Defense, BonusType.Flat, sourceId));
            if (baseStats.Health > 0)
                bonuses.Add(new StatBonus(StatId.KhiHuyetMax, baseStats.Health, BonusType.Flat, sourceId));
            if (baseStats.Mana > 0)
                bonuses.Add(new StatBonus(StatId.LinhLucMax, baseStats.Mana, BonusType.Flat, sourceId));
            if (baseStats.MoveSpeed > 0)
                bonuses.Add(new StatBonus(StatId.TocDo, baseStats.MoveSpeed, BonusType.Flat, sourceId));
            if (baseStats.CritRate > 0)
                bonuses.Add(new StatBonus(StatId.TiLeBaoKich, baseStats.CritRate, BonusType.Flat, sourceId));
            if (baseStats.CritDamage > 0)
                bonuses.Add(new StatBonus(StatId.SatThuongBaoKich, baseStats.CritDamage, BonusType.Flat, sourceId));
            
            // Affixes to stat bonuses
            if (affixes != null)
            {
                foreach (var affix in affixes)
                {
                    if (affix != null && affix.TryGetStatBonus(sourceId, out var affixBonus))
                    {
                        bonuses.Add(affixBonus);
                    }
                }
            }
            
            return bonuses.ToArray();
        }
        
        /// <summary>
        /// Clone definition for modifications
        /// </summary>
        public ItemDefinition Clone()
        {
            return new ItemDefinition
            {
                id = id,
                displayName = displayName,
                category = category,
                rarity = rarity,
                element = element,
                realmRequirement = realmRequirement,
                level = level,
                maxStackSize = maxStackSize,
                socketCount = socketCount,
                baseStats = baseStats?.Clone(),
                affixes = affixes?.Clone() as ItemAffix[],
                useEffect = useEffect?.Clone(),
                description = description,
                iconAddress = iconAddress,
                textureAddress = textureAddress
            };
        }
        
        public override string ToString()
        {
            return $"{displayName} ({id}) - {category} {rarity}";
        }
        
        public override bool Equals(object obj)
        {
            return obj is ItemDefinition other && id == other.id;
        }
        
        public override int GetHashCode()
        {
            return id?.GetHashCode() ?? 0;
        }
    }
    
    /// <summary>
    /// Enhanced item enums with Foundation patterns
    /// </summary>
    public enum ItemCategory
    {
        // Weapons
        Weapon, Sword, Bow, Staff,
        
        // Armor
        Armor, Helmet, Chest, Legs, Boots, Gloves, Cloak,
        
        // Accessories
        Ring, Necklace, Accessory,
        
        // Consumables
        Consumable, Potion, Food, Scroll,
        
        // Materials
        Material, Ore, Herb, Gem,
        
        // Special
        Manual, Currency, Quest, Pet, Artifact
    }
    
    public enum ItemRarity
    {
        Common,    // Phàm
        Uncommon,  // Hoàng  
        Rare,      // Huyền
        Epic,      // Địa
        Legendary, // Thiên
        Immortal,  // Tiên
        Divine     // Thần
    }
    
    public enum ItemElement
    {
        None,   // none
        Metal,  // kim
        Wood,   // mộc
        Water,  // thủy
        Fire,   // hỏa
        Earth,  // thổ
        Thunder,// lôi
        Dark,   // âm
        Light   // dương
    }
    
    public enum ItemRealm
    {
        PhamNhan,    // Phàm nhân
        LuyenKhi,    // Luyện khí 
        TrucCo,      // Trúc cơ
        KimDan,      // Kim đan
        NguyenAnh,   // Nguyên anh
        HoaThan,     // Hóa thần
        LuyenHu,     // Luyện hư
        HopThe,      // Hợp thể
        DaiThua,     // Đại thừa
        ChuanTien    // Chuẩn tiên
    }
    
    /// <summary>
    /// Enhanced ItemStats với Foundation integration
    /// </summary>
    [Serializable]
    public class ItemStats
    {
        [SerializeField] private float attack;
        [SerializeField] private float defense;
        [SerializeField] private float health;
        [SerializeField] private float mana;
        [SerializeField] private float moveSpeed;
        [SerializeField] private float critRate;
        [SerializeField] private float critDamage;
        [SerializeField] private float penetration;
        [SerializeField] private float lifesteal;
        [SerializeField] private ItemResistances resistances;
        
        public float Attack { get => attack; set => attack = value; }
        public float Defense { get => defense; set => defense = value; }
        public float Health { get => health; set => health = value; }
        public float Mana { get => mana; set => mana = value; }
        public float MoveSpeed { get => moveSpeed; set => moveSpeed = value; }
        public float CritRate { get => critRate; set => critRate = value; }
        public float CritDamage { get => critDamage; set => critDamage = value; }
        public float Penetration { get => penetration; set => penetration = value; }
        public float Lifesteal { get => lifesteal; set => lifesteal = value; }
        public ItemResistances Resistances { get => resistances; set => resistances = value; }
        
        public ItemStats()
        {
            resistances = new ItemResistances();
        }
        
        public ItemStats Clone()
        {
            return new ItemStats
            {
                attack = attack,
                defense = defense,
                health = health,
                mana = mana,
                moveSpeed = moveSpeed,
                critRate = critRate,
                critDamage = critDamage,
                penetration = penetration,
                lifesteal = lifesteal,
                resistances = resistances?.Clone()
            };
        }
        
        public override string ToString()
        {
            var parts = new System.Collections.Generic.List<string>();
            if (attack > 0) parts.Add($"ATK: {attack}");
            if (defense > 0) parts.Add($"DEF: {defense}");
            if (health > 0) parts.Add($"HP: {health}");
            if (mana > 0) parts.Add($"MP: {mana}");
            if (moveSpeed > 0) parts.Add($"SPD: {moveSpeed}");
            if (critRate > 0) parts.Add($"CRIT: {critRate}%");
            if (critDamage > 0) parts.Add($"CRITDMG: {critDamage}%");
            return string.Join(", ", parts);
        }
    }
    
    /// <summary>
    /// Element resistances for items
    /// </summary>
    [Serializable]
    public class ItemResistances
    {
        [SerializeField] private float metal;    // kim
        [SerializeField] private float wood;     // mộc
        [SerializeField] private float water;    // thủy
        [SerializeField] private float fire;     // hỏa
        [SerializeField] private float earth;    // thổ
        [SerializeField] private float thunder;  // lôi
        [SerializeField] private float dark;     // âm
        [SerializeField] private float light;    // dương
        
        public float Metal { get => metal; set => metal = value; }
        public float Wood { get => wood; set => wood = value; }
        public float Water { get => water; set => water = value; }
        public float Fire { get => fire; set => fire = value; }
        public float Earth { get => earth; set => earth = value; }
        public float Thunder { get => thunder; set => thunder = value; }
        public float Dark { get => dark; set => dark = value; }
        public float Light { get => light; set => light = value; }
        
        public ItemResistances Clone()
        {
            return new ItemResistances
            {
                metal = metal, wood = wood, water = water, fire = fire,
                earth = earth, thunder = thunder, dark = dark, light = light
            };
        }
        
        public float GetResistance(ItemElement element)
        {
            return element switch
            {
                ItemElement.Metal => metal,
                ItemElement.Wood => wood,
                ItemElement.Water => water,
                ItemElement.Fire => fire,
                ItemElement.Earth => earth,
                ItemElement.Thunder => thunder,
                ItemElement.Dark => dark,
                ItemElement.Light => light,
                _ => 0f
            };
        }
        
        public void SetResistance(ItemElement element, float value)
        {
            switch (element)
            {
                case ItemElement.Metal: metal = value; break;
                case ItemElement.Wood: wood = value; break;
                case ItemElement.Water: water = value; break;
                case ItemElement.Fire: fire = value; break;
                case ItemElement.Earth: earth = value; break;
                case ItemElement.Thunder: thunder = value; break;
                case ItemElement.Dark: dark = value; break;
                case ItemElement.Light: light = value; break;
            }
        }
    }
    
    /// <summary>
    /// Enhanced ItemAffix với StatBonus integration
    /// </summary>
    [Serializable]
    public class ItemAffix
    {
        [SerializeField] private string id;
        [SerializeField] private float value;
        [SerializeField] private int tier;
        [SerializeField] private AffixType type;
        
        public string Id { get => id; set => id = value; }
        public float Value { get => value; set => this.value = value; }
        public int Tier { get => tier; set => tier = value; }
        public AffixType Type { get => type; set => type = value; }
        
        public ItemAffix() { }
        
        public ItemAffix(string id, float value, int tier = 1, AffixType type = AffixType.Flat)
        {
            this.id = id;
            this.value = value;
            this.tier = tier;
            this.type = type;
        }
        
        /// <summary>
        /// Try convert affix to stat bonus
        /// </summary>
        public bool TryGetStatBonus(string sourceId, out StatBonus bonus)
        {
            bonus = null;
            
            // Map affix IDs to stat IDs (extend this mapping as needed)
            var statId = MapAffixToStat(id);
            if (statId == StatId.KhiHuyet) return false; // Invalid mapping
            
            var bonusType = type switch
            {
                AffixType.Flat => BonusType.Flat,
                AffixType.Percentage => BonusType.Percentage,
                AffixType.Multiplicative => BonusType.Multiplier,
                _ => BonusType.Flat
            };
            
            bonus = new StatBonus(statId, value, bonusType, sourceId);
            return true;
        }
        
        private StatId MapAffixToStat(string affixId)
        {
            // Map common affix IDs to stats (expand as needed)
            return affixId?.ToLower() switch
            {
                "atk" or "attack" => StatId.CongVatLy,
                "def" or "defense" => StatId.PhongVatLy,
                "hp" or "health" => StatId.KhiHuyetMax,
                "mp" or "mana" => StatId.LinhLucMax,
                "spd" or "speed" => StatId.TocDo,
                "crit" or "critrate" => StatId.TiLeBaoKich,
                "critdmg" or "critdamage" => StatId.SatThuongBaoKich,
                _ => StatId.KhiHuyet // Default/invalid
            };
        }
        
        public ItemAffix Clone()
        {
            return new ItemAffix(id, value, tier, type);
        }
        
        public override string ToString()
        {
            var typeStr = type == AffixType.Percentage ? "%" : "";
            return $"{id}: +{value}{typeStr} (T{tier})";
        }
    }
    
    public enum AffixType
    {
        Flat,
        Percentage,
        Multiplicative
    }
    
    /// <summary>
    /// Enhanced ItemUseEffect với Foundation integration
    /// </summary>
    [Serializable]
    public class ItemUseEffect
    {
        [SerializeField] private string effectType;
        [SerializeField] private float magnitude;
        [SerializeField] private float duration;
        [SerializeField] private string spellId;
        [SerializeField] private string description;
        
        public string EffectType { get => effectType; set => effectType = value; }
        public float Magnitude { get => magnitude; set => magnitude = value; }
        public float Duration { get => duration; set => duration = value; }
        public string SpellId { get => spellId; set => spellId = value; }
        public string Description { get => description; set => description = value; }
        
        public ItemUseEffect() { }
        
        public ItemUseEffect(string effectType, float magnitude, float duration = 0f)
        {
            this.effectType = effectType;
            this.magnitude = magnitude;
            this.duration = duration;
        }
        
        public ItemUseEffect Clone()
        {
            return new ItemUseEffect
            {
                effectType = effectType,
                magnitude = magnitude,
                duration = duration,
                spellId = spellId,
                description = description
            };
        }
        
        public override string ToString()
        {
            if (duration > 0)
                return $"{effectType}: {magnitude} for {duration}s";
            return $"{effectType}: {magnitude}";
        }
    }
    
    /// <summary>
    /// Equipment slot rules helper
    /// </summary>
    public static class EquipmentSlotRules
    {
        public static bool IsValidForSlot(string slotId, ItemCategory category)
        {
            if (string.IsNullOrEmpty(slotId)) return false;
            
            var slot = slotId.ToLowerInvariant();
            
            return slot switch
            {
                "weapon" or "mainhand" => category == ItemCategory.Weapon || category == ItemCategory.Sword || category == ItemCategory.Bow || category == ItemCategory.Staff,
                "helmet" or "head" => category == ItemCategory.Helmet,
                "chest" or "body" => category == ItemCategory.Chest || category == ItemCategory.Armor,
                "legs" or "pants" => category == ItemCategory.Legs,
                "boots" or "feet" => category == ItemCategory.Boots,
                "gloves" or "hands" => category == ItemCategory.Gloves,
                "cloak" or "back" => category == ItemCategory.Cloak,
                "ring1" or "ring2" or "ring" => category == ItemCategory.Ring,
                "necklace" or "neck" => category == ItemCategory.Necklace,
                "accessory" => category == ItemCategory.Accessory || category == ItemCategory.Ring || category == ItemCategory.Necklace,
                _ => false
            };
        }
        
        public static bool IsKnownSlot(string slotId)
        {
            if (string.IsNullOrEmpty(slotId)) return false;
            
            var slot = slotId.ToLowerInvariant();
            var knownSlots = new[] { "weapon", "mainhand", "helmet", "head", "chest", "body", "legs", "pants", "boots", "feet", "gloves", "hands", "cloak", "back", "ring1", "ring2", "ring", "necklace", "neck", "accessory" };
            
            return Array.Exists(knownSlots, s => s == slot);
        }
    }
}
