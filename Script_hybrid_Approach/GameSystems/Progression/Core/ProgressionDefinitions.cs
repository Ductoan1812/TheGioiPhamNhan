using System;
using UnityEngine;
using GameSystems.Stats.Core;

namespace GameSystems.Progression.Core
{
    /// <summary>
    /// Cultivation Realm definitions - enhanced từ Realm enum hiện tại
    /// </summary>
    public enum CultivationRealm
    {
        [RealmMeta("Phàm Nhân", 0, 1, 1.0f, "#FFFFFF")]
        PhamNhan = 0,
        
        [RealmMeta("Luyện Khí", 1, 10, 1.2f, "#87CEEB")]
        LuyenKhi = 1,
        [RealmMeta("Luyện Khí 1", 2, 10, 1.2f, "#87CEEB")]
        LuyenKhi_1 = 2,
        [RealmMeta("Luyện Khí 2", 3, 10, 1.2f, "#87CEEB")]
        LuyenKhi_2 = 3,
        [RealmMeta("Luyện Khí 3", 4, 10, 1.2f, "#87CEEB")]
        LuyenKhi_3 = 4,
        [RealmMeta("Luyện Khí 4", 5, 10, 1.2f, "#87CEEB")]
        LuyenKhi_4 = 5,
        [RealmMeta("Luyện Khí 5", 6, 10, 1.2f, "#87CEEB")]
        LuyenKhi_5 = 6,
        [RealmMeta("Luyện Khí 6", 7, 10, 1.2f, "#87CEEB")]
        LuyenKhi_6 = 7,
        [RealmMeta("Luyện Khí 7", 8, 10, 1.2f, "#87CEEB")]
        LuyenKhi_7 = 8,
        [RealmMeta("Luyện Khí 8", 9, 10, 1.2f, "#87CEEB")]
        LuyenKhi_8 = 9,
        [RealmMeta("Luyện Khí 9", 10, 10, 1.2f, "#87CEEB")]
        LuyenKhi_9 = 10,
        
        [RealmMeta("Trúc Cơ", 11, 15, 1.5f, "#90EE90")]
        TrucCo = 11,
        [RealmMeta("Kim Đan", 12, 20, 1.8f, "#FFD700")]
        KimDan = 12,
        [RealmMeta("Nguyên Anh", 13, 25, 2.2f, "#FF6347")]
        NguyenAnh = 13,
        [RealmMeta("Hóa Thần", 14, 30, 2.8f, "#9370DB")]
        HoaThan = 14,
        [RealmMeta("Luyện Hư", 15, 35, 3.5f, "#4169E1")]
        LuyenHu = 15,
        [RealmMeta("Hợp Thể", 16, 40, 4.5f, "#DC143C")]
        HopThe = 16,
        [RealmMeta("Đại Thừa", 17, 50, 6.0f, "#8B0000")]
        DaiThua = 17,
        [RealmMeta("Chuẩn Tiên", 18, 100, 10.0f, "#FF1493")]
        ChuanTien = 18
    }
    
    /// <summary>
    /// Realm metadata attribute
    /// </summary>
    [AttributeUsage(AttributeTargets.Field)]
    public class RealmMetaAttribute : Attribute
    {
        public string DisplayName { get; }
        public int MinLevel { get; }
        public int LevelsInRealm { get; }
        public float ExpMultiplier { get; }
        public string Color { get; }
        
        public RealmMetaAttribute(string displayName, int minLevel, int levelsInRealm, float expMultiplier, string color)
        {
            DisplayName = displayName;
            MinLevel = minLevel;
            LevelsInRealm = levelsInRealm;
            ExpMultiplier = expMultiplier;
            Color = color;
        }
    }
    
    /// <summary>
    /// Experience type definitions
    /// </summary>
    public enum ExperienceType
    {
        Combat,      // Từ chiến đấu
        Quest,       // Từ nhiệm vụ
        Cultivation, // Từ tu luyện
        Item,        // Từ dùng item
        Discovery,   // Từ khám phá
        Social,      // Từ tương tác xã hội
        Crafting,    // Từ chế tạo
        Event        // Từ sự kiện đặc biệt
    }
    
    /// <summary>
    /// Progression milestone definitions
    /// </summary>
    [Serializable]
    public class ProgressionMilestone
    {
        [SerializeField] private string id;
        [SerializeField] private string name;
        [SerializeField] private string description;
        [SerializeField] private int requiredLevel;
        [SerializeField] private CultivationRealm requiredRealm;
        [SerializeField] private ProgressionReward[] rewards;
        [SerializeField] private bool isRepeatable;
        
        public string Id => id;
        public string Name => name;
        public string Description => description;
        public int RequiredLevel => requiredLevel;
        public CultivationRealm RequiredRealm => requiredRealm;
        public ProgressionReward[] Rewards => rewards;
        public bool IsRepeatable => isRepeatable;
        
        public ProgressionMilestone(string id, string name, int requiredLevel, CultivationRealm requiredRealm)
        {
            this.id = id;
            this.name = name;
            this.requiredLevel = requiredLevel;
            this.requiredRealm = requiredRealm;
            this.rewards = new ProgressionReward[0];
        }
        
        public bool CanClaim(int currentLevel, CultivationRealm currentRealm)
        {
            return currentLevel >= requiredLevel && currentRealm >= requiredRealm;
        }
    }
    
    /// <summary>
    /// Progression rewards
    /// </summary>
    [Serializable]
    public class ProgressionReward
    {
        [SerializeField] private ProgressionRewardType type;
        [SerializeField] private string targetId; // stat id, item id, etc.
        [SerializeField] private float value;
        [SerializeField] private string description;
        
        public ProgressionRewardType Type => type;
        public string TargetId => targetId;
        public float Value => value;
        public string Description => description;
        
        public ProgressionReward(ProgressionRewardType type, string targetId, float value, string description = "")
        {
            this.type = type;
            this.targetId = targetId;
            this.value = value;
            this.description = description;
        }
    }
    
    /// <summary>
    /// Types of progression rewards
    /// </summary>
    public enum ProgressionRewardType
    {
        StatBonus,      // Permanent stat increase
        StatPoints,     // Allocatable stat points
        Item,           // Item reward
        Currency,       // Money reward
        Skill,          // Unlock skill
        Feature,        // Unlock feature
        Title,          // Unlock title
        Achievement     // Achievement unlock
    }
    
    /// <summary>
    /// Experience gain configuration
    /// </summary>
    [Serializable]
    public class ExperienceConfig
    {
        [Header("Base Experience Settings")]
        [SerializeField] private float baseExpPerLevel = 100f;
        [SerializeField] private float expGrowthRate = 1.2f;
        [SerializeField] private float maxExpMultiplier = 10f;
        
        [Header("Experience Type Multipliers")]
        [SerializeField] private float combatMultiplier = 1.0f;
        [SerializeField] private float questMultiplier = 1.5f;
        [SerializeField] private float cultivationMultiplier = 0.8f;
        [SerializeField] private float itemMultiplier = 1.2f;
        [SerializeField] private float discoveryMultiplier = 2.0f;
        [SerializeField] private float socialMultiplier = 0.5f;
        [SerializeField] private float craftingMultiplier = 0.7f;
        [SerializeField] private float eventMultiplier = 3.0f;
        
        public float BaseExpPerLevel => baseExpPerLevel;
        public float ExpGrowthRate => expGrowthRate;
        public float MaxExpMultiplier => maxExpMultiplier;
        
        public float GetMultiplier(ExperienceType expType)
        {
            return expType switch
            {
                ExperienceType.Combat => combatMultiplier,
                ExperienceType.Quest => questMultiplier,
                ExperienceType.Cultivation => cultivationMultiplier,
                ExperienceType.Item => itemMultiplier,
                ExperienceType.Discovery => discoveryMultiplier,
                ExperienceType.Social => socialMultiplier,
                ExperienceType.Crafting => craftingMultiplier,
                ExperienceType.Event => eventMultiplier,
                _ => 1.0f
            };
        }
        
        /// <summary>
        /// Calculate experience required for specific level
        /// </summary>
        public float GetExpRequiredForLevel(int level, CultivationRealm realm)
        {
            if (level <= 1) return 0f;
            
            var realmMeta = RealmMetaHelper.GetRealmMeta(realm);
            float realmMultiplier = realmMeta?.ExpMultiplier ?? 1.0f;
            
            float baseExp = baseExpPerLevel * Mathf.Pow(expGrowthRate, level - 1);
            float finalExp = baseExp * realmMultiplier;
            
            return Mathf.Min(finalExp, baseExpPerLevel * maxExpMultiplier);
        }
        
        /// <summary>
        /// Calculate total experience required to reach level
        /// </summary>
        public float GetTotalExpForLevel(int targetLevel, CultivationRealm realm)
        {
            float totalExp = 0f;
            for (int level = 2; level <= targetLevel; level++)
            {
                totalExp += GetExpRequiredForLevel(level, realm);
            }
            return totalExp;
        }
    }
    
    /// <summary>
    /// Helper for realm metadata
    /// </summary>
    public static class RealmMetaHelper
    {
        private static readonly System.Collections.Generic.Dictionary<CultivationRealm, RealmMetaAttribute> _metaCache 
            = new System.Collections.Generic.Dictionary<CultivationRealm, RealmMetaAttribute>();
        
        static RealmMetaHelper()
        {
            var type = typeof(CultivationRealm);
            foreach (CultivationRealm realm in Enum.GetValues(type))
            {
                var field = type.GetField(realm.ToString());
                var attr = (RealmMetaAttribute)Attribute.GetCustomAttribute(field, typeof(RealmMetaAttribute));
                if (attr != null)
                {
                    _metaCache[realm] = attr;
                }
            }
        }
        
        public static RealmMetaAttribute GetRealmMeta(CultivationRealm realm)
        {
            _metaCache.TryGetValue(realm, out var meta);
            return meta;
        }
        
        public static string GetDisplayName(CultivationRealm realm)
        {
            return GetRealmMeta(realm)?.DisplayName ?? realm.ToString();
        }
        
        public static Color GetRealmColor(CultivationRealm realm)
        {
            var meta = GetRealmMeta(realm);
            if (meta != null && ColorUtility.TryParseHtmlString(meta.Color, out var color))
            {
                return color;
            }
            return Color.white;
        }
        
        public static int GetMinLevel(CultivationRealm realm)
        {
            return GetRealmMeta(realm)?.MinLevel ?? 0;
        }
        
        public static int GetLevelsInRealm(CultivationRealm realm)
        {
            return GetRealmMeta(realm)?.LevelsInRealm ?? 1;
        }
        
        public static float GetExpMultiplier(CultivationRealm realm)
        {
            return GetRealmMeta(realm)?.ExpMultiplier ?? 1.0f;
        }
        
        /// <summary>
        /// Get realm for specific level
        /// </summary>
        public static CultivationRealm GetRealmForLevel(int level)
        {
            var realms = Enum.GetValues(typeof(CultivationRealm));
            CultivationRealm bestRealm = CultivationRealm.PhamNhan;
            
            foreach (CultivationRealm realm in realms)
            {
                var meta = GetRealmMeta(realm);
                if (meta != null && level >= meta.MinLevel)
                {
                    bestRealm = realm;
                }
            }
            
            return bestRealm;
        }
        
        /// <summary>
        /// Check if can advance to next realm
        /// </summary>
        public static bool CanAdvanceRealm(int currentLevel, CultivationRealm currentRealm)
        {
            var nextRealm = (CultivationRealm)((int)currentRealm + 1);
            if (!Enum.IsDefined(typeof(CultivationRealm), nextRealm)) return false;
            
            var nextMeta = GetRealmMeta(nextRealm);
            return nextMeta != null && currentLevel >= nextMeta.MinLevel;
        }
        
        /// <summary>
        /// Get next realm
        /// </summary>
        public static CultivationRealm? GetNextRealm(CultivationRealm currentRealm)
        {
            var nextRealm = (CultivationRealm)((int)currentRealm + 1);
            return Enum.IsDefined(typeof(CultivationRealm), nextRealm) ? nextRealm : null;
        }
    }
    
    /// <summary>
    /// Progression data for serialization
    /// </summary>
    [Serializable]
    public class ProgressionData
    {
        [SerializeField] private string entityId;
        [SerializeField] private int level = 1;
        [SerializeField] private float experience = 0f;
        [SerializeField] private CultivationRealm realm = CultivationRealm.PhamNhan;
        [SerializeField] private System.Collections.Generic.List<string> claimedMilestones = new System.Collections.Generic.List<string>();
        [SerializeField] private long lastExpGainTime;
        [SerializeField] private float totalLifetimeExp = 0f;
        
        public string EntityId => entityId;
        public int Level { get => level; set => level = value; }
        public float Experience { get => experience; set => experience = value; }
        public CultivationRealm Realm { get => realm; set => realm = value; }
        public System.Collections.Generic.List<string> ClaimedMilestones => claimedMilestones;
        public long LastExpGainTime { get => lastExpGainTime; set => lastExpGainTime = value; }
        public float TotalLifetimeExp { get => totalLifetimeExp; set => totalLifetimeExp = value; }
        
        public ProgressionData(string entityId)
        {
            this.entityId = entityId;
            this.level = 1;
            this.experience = 0f;
            this.realm = CultivationRealm.PhamNhan;
            this.claimedMilestones = new System.Collections.Generic.List<string>();
            this.lastExpGainTime = System.DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            this.totalLifetimeExp = 0f;
        }
        
        public bool HasClaimedMilestone(string milestoneId)
        {
            return claimedMilestones.Contains(milestoneId);
        }
        
        public void ClaimMilestone(string milestoneId)
        {
            if (!claimedMilestones.Contains(milestoneId))
            {
                claimedMilestones.Add(milestoneId);
            }
        }
        
        public void AddExperience(float amount)
        {
            experience += amount;
            totalLifetimeExp += amount;
            lastExpGainTime = System.DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        }
    }
}
