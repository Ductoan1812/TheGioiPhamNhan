using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Foundation.Events;

namespace GameSystems.Stats
{
    /// <summary>
    /// Collection of stats for an entity
    /// </summary>
    [Serializable]
    public class StatCollection
    {
        [SerializeField] private List<Stat> stats = new();
        private readonly Dictionary<StatType, Stat> statLookup = new();

        /// <summary>
        /// Initialize stat collection
        /// </summary>
        public void Initialize()
        {
            statLookup.Clear();
            foreach (var stat in stats)
            {
                statLookup[stat.Type] = stat;
            }
        }

        /// <summary>
        /// Add a new stat
        /// </summary>
        public void AddStat(StatType type, float baseValue)
        {
            if (HasStat(type))
            {
                Debug.LogWarning($"Stat {type} already exists in collection");
                return;
            }

            var stat = new Stat(type, baseValue);
            stats.Add(stat);
            statLookup[type] = stat;
        }

        /// <summary>
        /// Remove a stat
        /// </summary>
        public bool RemoveStat(StatType type)
        {
            if (!HasStat(type)) return false;

            var stat = statLookup[type];
            stats.Remove(stat);
            statLookup.Remove(type);
            
            return true;
        }

        /// <summary>
        /// Get stat by type
        /// </summary>
        public Stat GetStat(StatType type)
        {
            return statLookup.TryGetValue(type, out var stat) ? stat : null;
        }

        /// <summary>
        /// Get stat value
        /// </summary>
        public float GetStatValue(StatType type)
        {
            var stat = GetStat(type);
            return stat?.Value ?? 0f;
        }

        /// <summary>
        /// Set stat base value
        /// </summary>
        public void SetStatValue(StatType type, float value)
        {
            var stat = GetStat(type);
            if (stat != null)
            {
                stat.SetBaseValue(value);
            }
            else
            {
                AddStat(type, value);
            }
        }

        /// <summary>
        /// Check if stat exists
        /// </summary>
        public bool HasStat(StatType type)
        {
            return statLookup.ContainsKey(type);
        }

        /// <summary>
        /// Add modifier to stat
        /// </summary>
        public void AddModifier(StatType type, StatModifier modifier)
        {
            var stat = GetStat(type);
            stat?.AddModifier(modifier);
        }

        /// <summary>
        /// Remove modifier from stat
        /// </summary>
        public bool RemoveModifier(StatType type, StatModifier modifier)
        {
            var stat = GetStat(type);
            return stat?.RemoveModifier(modifier) ?? false;
        }

        /// <summary>
        /// Remove all modifiers from source for all stats
        /// </summary>
        public void RemoveAllModifiersFromSource(object source)
        {
            foreach (var stat in stats)
            {
                stat.RemoveModifiersFromSource(source);
            }
        }

        /// <summary>
        /// Get all stats
        /// </summary>
        public IReadOnlyList<Stat> GetAllStats()
        {
            return stats.AsReadOnly();
        }

        /// <summary>
        /// Get stats by category (example filtering)
        /// </summary>
        public IEnumerable<Stat> GetStatsByCategory(params StatType[] types)
        {
            return stats.Where(stat => types.Contains(stat.Type));
        }

        /// <summary>
        /// Calculate total stat value (useful for composite calculations)
        /// </summary>
        public float CalculateTotalValue(params StatType[] types)
        {
            return types.Sum(type => GetStatValue(type));
        }

        /// <summary>
        /// Copy stats from another collection
        /// </summary>
        public void CopyFrom(StatCollection other)
        {
            stats.Clear();
            statLookup.Clear();

            foreach (var otherStat in other.stats)
            {
                AddStat(otherStat.Type, otherStat.BaseValue);
                
                // Copy modifiers
                var newStat = GetStat(otherStat.Type);
                foreach (var modifier in otherStat.GetModifiers())
                {
                    newStat.AddModifier(modifier);
                }
            }
        }

        /// <summary>
        /// Reset all stats to base values
        /// </summary>
        public void ResetToBaseValues()
        {
            foreach (var stat in stats)
            {
                stat.RemoveModifiersFromSource(null); // Remove all modifiers
            }
        }
    }
}
