using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Foundation.Events;
using Foundation.Utils;
using GameSystems.Stats.Core;

namespace GameSystems.Stats
{
    /// <summary>
    /// StatCollection - cải tiến từ StatCollection trong StatSystem.cs hiện tại.
    /// Quản lý toàn bộ stats của 1 entity với events và validation.
    /// </summary>
    [Serializable]
    public class StatCollection
    {
        [SerializeField] private List<StatEntry> stats = new List<StatEntry>();
        [SerializeField] private string ownerId;
        
        // Fast lookup dictionary
        private Dictionary<StatId, StatEntry> statLookup;
        private bool isDictionaryDirty = true;
        
        // Events
        public event Action<StatId, float, float> OnStatChanged; // (statId, oldValue, newValue)
        public event Action<StatId, float> OnFinalChanged; // For compatibility with old system
        public event Action OnStatsRecalculated;
        
        // Properties
        public string OwnerId => ownerId;
        public IReadOnlyList<StatEntry> AllStats => stats;
        public int StatCount => stats.Count;
        
        public StatCollection(string ownerId = "")
        {
            this.ownerId = ownerId;
            InitializeDefaultStats();
        }
        
        #region Initialization
        
        /// <summary>
        /// Initialize with default stats for game
        /// </summary>
        private void InitializeDefaultStats()
        {
            // Survival stats
            SetBaseStat(StatId.KhiHuyetMax, GameConstants.Player.DEFAULT_MOVE_SPEED * 10f); // HP based on move speed logic
            SetBaseStat(StatId.KhiHuyet, GetStat(StatId.KhiHuyetMax));
            SetBaseStat(StatId.LinhLucMax, 100f);
            SetBaseStat(StatId.LinhLuc, GetStat(StatId.LinhLucMax));
            SetBaseStat(StatId.ThoNguyenMax, 100f);
            SetBaseStat(StatId.ThoNguyen, GetStat(StatId.ThoNguyenMax));
            
            // Combat stats
            SetBaseStat(StatId.CongVatLy, 10f);
            SetBaseStat(StatId.CongPhapThuat, 5f);
            SetBaseStat(StatId.PhongVatLy, 5f);
            SetBaseStat(StatId.PhongPhapThuat, 3f);
            SetBaseStat(StatId.TocDo, GameConstants.Player.DEFAULT_MOVE_SPEED);
            
            // Extended stats
            SetBaseStat(StatId.TiLeBaoKich, 5f); // 5% crit rate
            SetBaseStat(StatId.SatThuongBaoKich, 200f); // 200% crit damage
            SetBaseStat(StatId.InventorySize, 20f);
            
            DebugUtils.Log($"[StatCollection] Initialized default stats for {ownerId}");
        }
        
        /// <summary>
        /// Initialize from existing data (migration helper)
        /// </summary>
        public void InitializeFromLegacyData(Dictionary<StatId, float> legacyStats)
        {
            stats.Clear();
            isDictionaryDirty = true;
            
            foreach (var kvp in legacyStats)
            {
                SetBaseStat(kvp.Key, kvp.Value);
            }
            
            DebugUtils.Log($"[StatCollection] Initialized from legacy data: {legacyStats.Count} stats");
        }
        
        #endregion
        
        #region Stat Access
        
        /// <summary>
        /// Get final stat value (base + bonuses)
        /// </summary>
        public float GetStat(StatId statId)
        {
            var entry = GetStatEntry(statId);
            return entry?.FinalValue ?? 0f;
        }
        
        /// <summary>
        /// Get base stat value (without bonuses)  
        /// </summary>
        public float GetBaseStat(StatId statId)
        {
            var entry = GetStatEntry(statId);
            return entry?.BaseValue ?? 0f;
        }
        
        /// <summary>
        /// Set base stat value
        /// </summary>
        public void SetBaseStat(StatId statId, float value)
        {
            var entry = GetOrCreateStatEntry(statId);
            float oldValue = entry.FinalValue;
            
            entry.SetBaseValue(value);
            
            if (!oldValue.Approximately(entry.FinalValue))
            {
                NotifyStatChanged(statId, oldValue, entry.FinalValue);
            }
        }
        
        /// <summary>
        /// Modify stat by delta (add/subtract)
        /// </summary>
        public void ModifyStat(StatId statId, float delta)
        {
            float currentValue = GetBaseStat(statId);
            SetBaseStat(statId, currentValue + delta);
        }
        
        /// <summary>
        /// Get stat entry for advanced operations
        /// </summary>
        public StatEntry GetStatEntry(StatId statId)
        {
            EnsureDictionary();
            statLookup.TryGetValue(statId, out StatEntry entry);
            return entry;
        }
        
        private StatEntry GetOrCreateStatEntry(StatId statId)
        {
            var entry = GetStatEntry(statId);
            if (entry == null)
            {
                entry = new StatEntry(statId);
                entry.OnValueChanged += OnStatEntryChanged;
                stats.Add(entry);
                isDictionaryDirty = true;
                DebugUtils.Log($"[StatCollection] Created new stat entry: {statId}");
            }
            return entry;
        }
        
        #endregion
        
        #region Bonus Management
        
        /// <summary>
        /// Add bonus to stat
        /// </summary>
        public void AddBonus(StatBonus bonus)
        {
            if (bonus == null) return;
            
            var entry = GetOrCreateStatEntry(bonus.StatId);
            entry.AddBonus(bonus);
            
            // Dispatch event
            EventBus.Dispatch(new StatBonusAddedEvent(ownerId, bonus));
        }
        
        /// <summary>
        /// Remove specific bonus
        /// </summary>
        public bool RemoveBonus(StatBonus bonus)
        {
            var entry = GetStatEntry(bonus.StatId);
            if (entry != null && entry.RemoveBonus(bonus))
            {
                EventBus.Dispatch(new StatBonusRemovedEvent(ownerId, bonus));
                return true;
            }
            return false;
        }
        
        /// <summary>
        /// Remove all bonuses from a source
        /// </summary>
        public void RemoveBonusesFromSource(string sourceId)
        {
            int totalRemoved = 0;
            foreach (var entry in stats)
            {
                if (entry.RemoveBonusById(sourceId))
                    totalRemoved++;
            }
            
            if (totalRemoved > 0)
            {
                DebugUtils.Log($"[StatCollection] Removed bonuses from source '{sourceId}': {totalRemoved} stats affected");
                EventBus.Dispatch(new StatBonusSourceRemovedEvent(ownerId, sourceId));
            }
        }
        
        /// <summary>
        /// Get all bonuses for a stat
        /// </summary>
        public IReadOnlyList<StatBonus> GetBonuses(StatId statId)
        {
            var entry = GetStatEntry(statId);
            return entry?.Bonuses ?? new List<StatBonus>();
        }
        
        #endregion
        
        #region Bulk Operations
        
        /// <summary>
        /// Add multiple bonuses at once
        /// </summary>
        public void AddBonuses(IEnumerable<StatBonus> bonuses)
        {
            foreach (var bonus in bonuses)
            {
                AddBonus(bonus);
            }
        }
        
        /// <summary>
        /// Recalculate all stats (expensive, use sparingly)
        /// </summary>
        public void RecalculateAll()
        {
            foreach (var entry in stats)
            {
                // Force recalculation by accessing FinalValue
                _ = entry.FinalValue;
            }
            
            OnStatsRecalculated?.Invoke();
            EventBus.Dispatch(new StatsRecalculatedEvent(ownerId));
            DebugUtils.Log($"[StatCollection] Recalculated all stats for {ownerId}");
        }
        
        /// <summary>
        /// Clear all bonuses
        /// </summary>
        public void ClearAllBonuses()
        {
            foreach (var entry in stats)
            {
                entry.ClearBonuses();
            }
            
            DebugUtils.Log($"[StatCollection] Cleared all bonuses for {ownerId}");
        }
        
        #endregion
        
        #region Resource Management
        
        /// <summary>
        /// Clamp current resource to max (HP, MP, Stamina)
        /// </summary>
        public void ClampCurrentToMax(StatId currentStat, StatId maxStat)
        {
            float currentValue = GetStat(currentStat);
            float maxValue = GetStat(maxStat);
            
            if (currentValue > maxValue)
            {
                SetBaseStat(currentStat, maxValue);
                DebugUtils.Log($"[StatCollection] Clamped {currentStat} to max: {maxValue}");
            }
        }
        
        /// <summary>
        /// Restore current resource to percentage of max
        /// </summary>
        public void RestoreResource(StatId currentStat, StatId maxStat, float percentage = 1f)
        {
            float maxValue = GetStat(maxStat);
            float targetValue = maxValue * percentage;
            SetBaseStat(currentStat, targetValue);
        }
        
        /// <summary>
        /// Get resource percentage (current / max)
        /// </summary>
        public float GetResourcePercentage(StatId currentStat, StatId maxStat)
        {
            float currentValue = GetStat(currentStat);
            float maxValue = GetStat(maxStat);
            return maxValue > 0 ? currentValue / maxValue : 0f;
        }
        
        #endregion
        
        #region Events
        
        private void OnStatEntryChanged(StatId statId, float newValue)
        {
            OnFinalChanged?.Invoke(statId, newValue);
        }
        
        private void NotifyStatChanged(StatId statId, float oldValue, float newValue)
        {
            OnStatChanged?.Invoke(statId, oldValue, newValue);
            OnFinalChanged?.Invoke(statId, newValue);
            
            // Dispatch global event
            EventBus.Dispatch(new StatChangedEvent(ownerId, statId, oldValue, newValue));
        }
        
        #endregion
        
        #region Internal Helpers
        
        private void EnsureDictionary()
        {
            if (isDictionaryDirty || statLookup == null)
            {
                statLookup = stats.ToDictionary(s => s.StatId, s => s);
                isDictionaryDirty = false;
            }
        }
        
        #endregion
        
        #region Serialization Support
        
        /// <summary>
        /// Get serializable data for save/load
        /// </summary>
        public Dictionary<StatId, float> GetSerializableData()
        {
            return stats.ToDictionary(s => s.StatId, s => s.BaseValue);
        }
        
        /// <summary>
        /// Load from serializable data
        /// </summary>
        public void LoadFromSerializableData(Dictionary<StatId, float> data)
        {
            foreach (var kvp in data)
            {
                SetBaseStat(kvp.Key, kvp.Value);
            }
            
            DebugUtils.Log($"[StatCollection] Loaded {data.Count} stats from serializable data");
        }
        
        #endregion
        
        #region Debug & Validation
        
        /// <summary>
        /// Validate stats for common issues
        /// </summary>
        public bool ValidateStats()
        {
            bool isValid = true;
            
            // Check for negative resources
            var resourceStats = new[] 
            { 
                (StatId.KhiHuyet, StatId.KhiHuyetMax),
                (StatId.LinhLuc, StatId.LinhLucMax),
                (StatId.ThoNguyen, StatId.ThoNguyenMax)
            };
            
            foreach (var (current, max) in resourceStats)
            {
                if (GetStat(current) < 0)
                {
                    DebugUtils.LogWarning($"[StatCollection] Negative resource: {current} = {GetStat(current)}");
                    isValid = false;
                }
                
                if (GetStat(current) > GetStat(max))
                {
                    DebugUtils.LogWarning($"[StatCollection] Resource exceeds max: {current} ({GetStat(current)}) > {max} ({GetStat(max)})");
                    isValid = false;
                }
            }
            
            return isValid;
        }
        
        /// <summary>
        /// Log all stats for debugging
        /// </summary>
        public void LogAllStats()
        {
            DebugUtils.Log($"[StatCollection] All stats for {ownerId}:");
            foreach (var entry in stats.OrderBy(s => s.StatId))
            {
                DebugUtils.Log($"  {entry}");
            }
        }
        
        #endregion
    }
}
