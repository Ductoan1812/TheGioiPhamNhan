using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Foundation.Utils;
using GameSystems.Stats.Core;

namespace GameSystems.Stats
{
    /// <summary>
    /// StatEntry - đại diện cho 1 stat với base value và bonuses.
    /// Cải tiến từ StatEntry trong StatSystem.cs hiện tại.
    /// </summary>
    [Serializable]
    public class StatEntry
    {
        [SerializeField] private StatId statId;
        [SerializeField] private float baseValue;
        [SerializeField] private List<StatBonus> bonuses = new List<StatBonus>();
        
        // Cache
        private float cachedFinalValue;
        private bool isDirty = true;
        
        // Events
        public event Action<StatId, float> OnValueChanged;
        
        // Properties
        public StatId StatId => statId;
        public float BaseValue => baseValue;
        public IReadOnlyList<StatBonus> Bonuses => bonuses;
        public int BonusCount => bonuses.Count;
        
        /// <summary>
        /// Final value (base + all bonuses)
        /// </summary>
        public float FinalValue
        {
            get
            {
                if (isDirty)
                {
                    cachedFinalValue = CalculateFinalValue();
                    isDirty = false;
                }
                return cachedFinalValue;
            }
        }
        
        public StatEntry(StatId statId, float baseValue = 0f)
        {
            this.statId = statId;
            this.baseValue = baseValue;
        }
        
        #region Base Value Management
        
        /// <summary>
        /// Set base value
        /// </summary>
        public void SetBaseValue(float value)
        {
            if (!baseValue.Approximately(value))
            {
                baseValue = value;
                MarkDirty();
            }
        }
        
        /// <summary>
        /// Add to base value
        /// </summary>
        public void AddToBaseValue(float delta)
        {
            SetBaseValue(baseValue + delta);
        }
        
        #endregion
        
        #region Bonus Management
        
        /// <summary>
        /// Add bonus to this stat
        /// </summary>
        public void AddBonus(StatBonus bonus)
        {
            if (bonus == null)
            {
                DebugUtils.LogWarning($"[StatEntry] Attempted to add null bonus to {statId}");
                return;
            }
            
            if (bonus.StatId != statId)
            {
                DebugUtils.LogWarning($"[StatEntry] Bonus stat mismatch: {bonus.StatId} != {statId}");
                return;
            }
            
            bonuses.Add(bonus);
            MarkDirty();
            
            DebugUtils.Log($"[StatEntry] Added bonus to {statId}: {bonus}");
        }
        
        /// <summary>
        /// Remove specific bonus
        /// </summary>
        public bool RemoveBonus(StatBonus bonus)
        {
            if (bonuses.Remove(bonus))
            {
                MarkDirty();
                DebugUtils.Log($"[StatEntry] Removed bonus from {statId}: {bonus}");
                return true;
            }
            return false;
        }
        
        /// <summary>
        /// Remove bonus by ID
        /// </summary>
        public bool RemoveBonusById(string bonusId)
        {
            int removedCount = bonuses.RemoveAll(b => b.SourceId == bonusId);
            if (removedCount > 0)
            {
                MarkDirty();
                DebugUtils.Log($"[StatEntry] Removed {removedCount} bonuses with ID '{bonusId}' from {statId}");
                return true;
            }
            return false;
        }
        
        /// <summary>
        /// Remove all bonuses from source
        /// </summary>
        public int RemoveBonusesFromSource(string sourceId)
        {
            int removedCount = bonuses.RemoveAll(b => b.SourceId == sourceId);
            if (removedCount > 0)
            {
                MarkDirty();
                DebugUtils.Log($"[StatEntry] Removed {removedCount} bonuses from source '{sourceId}' on {statId}");
            }
            return removedCount;
        }
        
        /// <summary>
        /// Clear all bonuses
        /// </summary>
        public void ClearBonuses()
        {
            if (bonuses.Count > 0)
            {
                int count = bonuses.Count;
                bonuses.Clear();
                MarkDirty();
                DebugUtils.Log($"[StatEntry] Cleared {count} bonuses from {statId}");
            }
        }
        
        /// <summary>
        /// Get bonuses from specific source
        /// </summary>
        public IEnumerable<StatBonus> GetBonusesFromSource(string sourceId)
        {
            return bonuses.Where(b => b.SourceId == sourceId);
        }
        
        /// <summary>
        /// Get bonuses by type
        /// </summary>
        public IEnumerable<StatBonus> GetBonusesByType(BonusType type)
        {
            return bonuses.Where(b => b.Type == type);
        }
        
        /// <summary>
        /// Check if has bonus from source
        /// </summary>
        public bool HasBonusFromSource(string sourceId)
        {
            return bonuses.Any(b => b.SourceId == sourceId);
        }
        
        #endregion
        
        #region Calculation
        
        /// <summary>
        /// Calculate final value with all bonuses applied
        /// </summary>
        private float CalculateFinalValue()
        {
            if (bonuses.Count == 0)
                return baseValue;
            
            // Step 1: Start with base value
            float result = baseValue;
            
            // Step 2: Apply flat bonuses first
            float flatBonus = bonuses
                .Where(b => b.Type == BonusType.Flat)
                .Sum(b => b.Value);
            result += flatBonus;
            
            // Step 3: Apply multiplier bonuses
            float multiplierBonus = bonuses
                .Where(b => b.Type == BonusType.Multiplier)
                .Sum(b => b.Value);
            result *= (1f + multiplierBonus);
            
            // Step 4: Apply percentage bonuses (similar to multiplicative but calculated differently)
            float percentageBonus = bonuses
                .Where(b => b.Type == BonusType.Percentage)
                .Sum(b => b.Value / 100f);
            result *= (1f + percentageBonus);
            
            // Note: No override bonuses since BonusType doesn't include Override
            
            // Step 6: Ensure non-negative for most stats (except some special ones)
            if (ShouldClampToZero(statId))
            {
                result = Mathf.Max(0f, result);
            }
            
            return result;
        }
        
        /// <summary>
        /// Check if stat should be clamped to zero minimum
        /// </summary>
        private bool ShouldClampToZero(StatId statId)
        {
            // Most stats should not go negative, but some might allow it
            switch (statId)
            {
                case StatId.KhiHuyet:
                case StatId.KhiHuyetMax:
                case StatId.LinhLuc:
                case StatId.LinhLucMax:
                case StatId.ThoNguyen:
                case StatId.ThoNguyenMax:
                case StatId.CongVatLy:
                case StatId.CongPhapThuat:
                case StatId.PhongVatLy:
                case StatId.PhongPhapThuat:
                case StatId.TocDo:
                case StatId.TiLeBaoKich:
                case StatId.SatThuongBaoKich:
                case StatId.InventorySize:
                    return true;
                    
                default:
                    return true; // Default to clamping
            }
        }
        
        #endregion
        
        #region Utility
        
        /// <summary>
        /// Mark as dirty for recalculation
        /// </summary>
        private void MarkDirty()
        {
            isDirty = true;
            OnValueChanged?.Invoke(statId, FinalValue);
        }
        
        /// <summary>
        /// Force recalculation
        /// </summary>
        public void ForceRecalculate()
        {
            MarkDirty();
            _ = FinalValue; // Access to trigger calculation
        }
        
        /// <summary>
        /// Get total bonus value by type
        /// </summary>
        public float GetTotalBonusByType(BonusType type)
        {
            return bonuses
                .Where(b => b.Type == type)
                .Sum(b => b.Value);
        }
        
        /// <summary>
        /// Get bonus breakdown for debugging
        /// </summary>
        public string GetBonusBreakdown()
        {
            if (bonuses.Count == 0)
                return $"{statId}: {baseValue} (base only)";
            
            var breakdown = $"{statId}: {baseValue} (base)";
            
            var flatBonuses = GetTotalBonusByType(BonusType.Flat);
            if (!flatBonuses.Approximately(0f))
                breakdown += $" + {flatBonuses} (flat)";
            
            var multBonuses = GetTotalBonusByType(BonusType.Multiplier);
            if (!multBonuses.Approximately(0f))
                breakdown += $" × {1f + multBonuses:F2} (mult)";
            
            var percBonuses = GetTotalBonusByType(BonusType.Percentage);
            if (!percBonuses.Approximately(0f))
                breakdown += $" × {1f + percBonuses / 100f:F2} (%)";
            
            // Note: No override bonuses since BonusType doesn't include Override
            
            breakdown += $" = {FinalValue}";
            return breakdown;
        }
        
        #endregion
        
        #region Object Overrides
        
        public override string ToString()
        {
            return GetBonusBreakdown();
        }
        
        public override bool Equals(object obj)
        {
            if (obj is StatEntry other)
            {
                return statId == other.statId && 
                       baseValue.Approximately(other.baseValue) &&
                       bonuses.SequenceEqual(other.bonuses);
            }
            return false;
        }
        
        public override int GetHashCode()
        {
            return HashCode.Combine(statId, baseValue, bonuses.Count);
        }
        
        #endregion
    }
}
