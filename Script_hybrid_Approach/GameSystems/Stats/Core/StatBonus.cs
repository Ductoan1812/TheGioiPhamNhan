using System;
using Foundation.Utils;

namespace GameSystems.Stats.Core
{
    /// <summary>
    /// Stat bonus types - cải tiến từ StatBonus.cs hiện tại
    /// </summary>
    public enum BonusType
    {
        Flat,        // +10 damage
        Percentage,  // +15% damage
        Multiplier   // x1.5 damage (rare)
    }
    
    /// <summary>
    /// Stat bonus - áp dụng lên base value
    /// </summary>
    [Serializable]
    public class StatBonus
    {
        [UnityEngine.SerializeField] private StatId statId;
        [UnityEngine.SerializeField] private float value;
        [UnityEngine.SerializeField] private BonusType type;
        [UnityEngine.SerializeField] private string sourceId;
        [UnityEngine.SerializeField] private int priority;
        [UnityEngine.SerializeField] private float duration = -1f; // -1 = permanent
        
        private float startTime;
        
        public StatId StatId => statId;
        public float Value => value;
        public BonusType Type => type;
        public string SourceId => sourceId;
        public int Priority => priority;
        public float Duration => duration;
        public bool IsPermanent => duration < 0;
        public bool IsExpired => !IsPermanent && (UnityEngine.Time.time - startTime) >= duration;
        
        public StatBonus(StatId statId, float value, BonusType type, string sourceId, int priority = 0, float duration = -1f)
        {
            this.statId = statId;
            this.value = value;
            this.type = type;
            this.sourceId = sourceId ?? "Unknown";
            this.priority = priority;
            this.duration = duration;
            this.startTime = UnityEngine.Time.time;
        }
        
        /// <summary>
        /// Apply bonus to a base value
        /// </summary>
        public float ApplyTo(float baseValue)
        {
            if (IsExpired) return baseValue;
            
            switch (type)
            {
                case BonusType.Flat:
                    return baseValue + value;
                    
                case BonusType.Percentage:
                    return baseValue * (1f + value / 100f);
                    
                case BonusType.Multiplier:
                    return baseValue * value;
                    
                default:
                    DebugUtils.LogWarning($"[StatBonus] Unknown bonus type: {type}");
                    return baseValue;
            }
        }
        
        /// <summary>
        /// Get remaining duration
        /// </summary>
        public float GetRemainingDuration()
        {
            if (IsPermanent) return float.MaxValue;
            return UnityEngine.Mathf.Max(0f, duration - (UnityEngine.Time.time - startTime));
        }
        
        /// <summary>
        /// Get display string for UI
        /// </summary>
        public string GetDisplayString()
        {
            string valueStr = type switch
            {
                BonusType.Flat => value >= 0 ? $"+{value:F1}" : $"{value:F1}",
                BonusType.Percentage => value >= 0 ? $"+{value:F1}%" : $"{value:F1}%",
                BonusType.Multiplier => $"×{value:F2}",
                _ => value.ToString("F1")
            };
            
            if (!IsPermanent)
            {
                float remaining = GetRemainingDuration();
                valueStr += $" ({remaining:F1}s)";
            }
            
            return valueStr;
        }
        
        public override string ToString()
        {
            return $"{GetDisplayString()} ({sourceId})";
        }
        
        public override bool Equals(object obj)
        {
            if (obj is StatBonus other)
            {
                return statId == other.statId && 
                       value.Approximately(other.value) && 
                       type == other.type && 
                       sourceId == other.sourceId;
            }
            return false;
        }
        
        public override int GetHashCode()
        {
            return HashCode.Combine(statId, value, type, sourceId);
        }
    }
    
    /// <summary>
    /// Predefined bonus priorities để consistent ordering
    /// </summary>
    public static class BonusPriority
    {
        public const int BASE_STATS = 0;           // Character base stats
        public const int EQUIPMENT = 100;         // Equipment bonuses
        public const int SKILLS = 200;           // Skill bonuses
        public const int BUFFS = 300;            // Temporary buffs
        public const int AURAS = 400;            // Aura effects
        public const int DEBUFFS = 500;          // Debuffs (applied last)
        public const int SPECIAL = 1000;         // Special effects
    }
    
    /// <summary>
    /// Static factory methods cho common bonuses
    /// </summary>
    public static class StatBonusFactory
    {
        /// <summary>
        /// Equipment bonus
        /// </summary>
        public static StatBonus CreateEquipmentBonus(StatId statId, float value, string equipmentId)
        {
            return new StatBonus(statId, value, BonusType.Flat, $"Equipment_{equipmentId}", BonusPriority.EQUIPMENT);
        }
        
        /// <summary>
        /// Skill bonus
        /// </summary>
        public static StatBonus CreateSkillBonus(StatId statId, float value, BonusType type, string skillId)
        {
            return new StatBonus(statId, value, type, $"Skill_{skillId}", BonusPriority.SKILLS);
        }
        
        /// <summary>
        /// Temporary buff
        /// </summary>
        public static StatBonus CreateBuffBonus(StatId statId, float value, BonusType type, string buffId, float duration)
        {
            return new StatBonus(statId, value, type, $"Buff_{buffId}", BonusPriority.BUFFS, duration);
        }
        
        /// <summary>
        /// Debuff
        /// </summary>
        public static StatBonus CreateDebuffBonus(StatId statId, float value, string debuffId, float duration)
        {
            return new StatBonus(statId, value, BonusType.Percentage, $"Debuff_{debuffId}", BonusPriority.DEBUFFS, duration);
        }
        
        /// <summary>
        /// Level scaling bonus
        /// </summary>
        public static StatBonus CreateLevelBonus(StatId statId, int level, float perLevelValue)
        {
            float totalValue = level * perLevelValue;
            return new StatBonus(statId, totalValue, BonusType.Flat, "LevelScaling", BonusPriority.BASE_STATS);
        }
    }
}
