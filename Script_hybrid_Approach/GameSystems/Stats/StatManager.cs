using System;
using System.Collections.Generic;
using UnityEngine;
using Foundation.Events;
using Foundation.Utils;
using Foundation.Architecture;
using GameSystems.Stats.Core;

namespace GameSystems.Stats
{
    /// <summary>
    /// StatManager - cải tiến từ StatSystem.cs hiện tại.
    /// Quản lý stats cho nhiều entities, events và tích hợp với Foundation layer.
    /// </summary>
    public class StatManager : Singleton<StatManager>
    {
        [Header("Configuration")]
        [SerializeField] private bool enableDebugLogging = false;
        [SerializeField] private float autoSaveInterval = 30f; // Auto save every 30 seconds
        
        // Entity collections
        private Dictionary<string, StatCollection> entityStats = new Dictionary<string, StatCollection>();
        private float lastAutoSaveTime;
        
        // Events
        public event Action<string> OnEntityRegistered;
        public event Action<string> OnEntityUnregistered;
        
        // Properties
        public int EntityCount => entityStats.Count;
        public IReadOnlyDictionary<string, StatCollection> AllEntityStats => entityStats;
        
        #region Unity Lifecycle
        
        protected override void Awake()
        {
            base.Awake();
            
            // Subscribe to global events - temporarily commented for compilation
            // EventBus.Subscribe<StatChangedEvent>(OnStatChanged);
            // EventBus.Subscribe<StatBonusAddedEvent>(OnBonusAdded);
            // EventBus.Subscribe<StatBonusRemovedEvent>(OnBonusRemoved);
            
            if (enableDebugLogging)
                DebugUtils.Log("[StatManager] Initialized");
        }
        
        private void Update()
        {
            // Auto save check
            if (Time.time - lastAutoSaveTime > autoSaveInterval)
            {
                AutoSaveStats();
                lastAutoSaveTime = Time.time;
            }
        }
        
        protected override void OnDestroy()
        {
            // Unsubscribe from events
            EventBus.Unsubscribe<StatChangedEvent>(OnStatChanged);
            EventBus.Unsubscribe<StatBonusAddedEvent>(OnBonusAdded);
            EventBus.Unsubscribe<StatBonusRemovedEvent>(OnBonusRemoved);
            
            base.OnDestroy();
        }
        
        #endregion
        
        #region Entity Management
        
        /// <summary>
        /// Register entity with stats
        /// </summary>
        public StatCollection RegisterEntity(string entityId)
        {
            if (string.IsNullOrEmpty(entityId))
            {
                DebugUtils.LogError("[StatManager] Cannot register entity with null/empty ID");
                return null;
            }
            
            if (entityStats.ContainsKey(entityId))
            {
                DebugUtils.LogWarning($"[StatManager] Entity already registered: {entityId}");
                return entityStats[entityId];
            }
            
            var collection = new StatCollection(entityId);
            entityStats[entityId] = collection;
            
            OnEntityRegistered?.Invoke(entityId);
            EventBus.Dispatch(new EntityStatsRegisteredEvent(entityId, collection.StatCount));
            
            if (enableDebugLogging)
                DebugUtils.Log($"[StatManager] Registered entity: {entityId}");
            return collection;
        }
        
        /// <summary>
        /// Register entity with existing data (migration helper)
        /// </summary>
        public StatCollection RegisterEntityFromLegacy(string entityId, Dictionary<StatId, float> legacyStats)
        {
            var collection = RegisterEntity(entityId);
            if (collection != null && legacyStats != null)
            {
                collection.InitializeFromLegacyData(legacyStats);
                DebugUtils.Log($"[StatManager] Migrated legacy stats for {entityId}: {legacyStats.Count} stats");
            }
            return collection;
        }
        
        /// <summary>
        /// Unregister entity
        /// </summary>
        public bool UnregisterEntity(string entityId)
        {
            if (entityStats.Remove(entityId))
            {
                OnEntityUnregistered?.Invoke(entityId);
                EventBus.Dispatch(new EntityStatsUnregisteredEvent(entityId));
                DebugUtils.Log($"[StatManager] Unregistered entity: {entityId}");
                return true;
            }
            return false;
        }
        
        /// <summary>
        /// Get stats collection for entity
        /// </summary>
        public StatCollection GetEntityStats(string entityId)
        {
            entityStats.TryGetValue(entityId, out StatCollection collection);
            return collection;
        }
        
        /// <summary>
        /// Check if entity is registered
        /// </summary>
        public bool IsEntityRegistered(string entityId)
        {
            return entityStats.ContainsKey(entityId);
        }
        
        #endregion
        
        #region Stat Operations
        
        /// <summary>
        /// Get stat value for entity
        /// </summary>
        public float GetStat(string entityId, StatId statId)
        {
            var collection = GetEntityStats(entityId);
            return collection?.GetStat(statId) ?? 0f;
        }
        
        /// <summary>
        /// Set stat value for entity
        /// </summary>
        public void SetStat(string entityId, StatId statId, float value)
        {
            var collection = GetEntityStats(entityId);
            if (collection != null)
            {
                collection.SetBaseStat(statId, value);
            }
            else
            {
                DebugUtils.LogWarning($"[StatManager] Entity not found: {entityId}");
            }
        }
        
        /// <summary>
        /// Modify stat by delta for entity
        /// </summary>
        public void ModifyStat(string entityId, StatId statId, float delta)
        {
            var collection = GetEntityStats(entityId);
            if (collection != null)
            {
                collection.ModifyStat(statId, delta);
            }
            else
            {
                DebugUtils.LogWarning($"[StatManager] Entity not found: {entityId}");
            }
        }
        
        #endregion
        
        #region Bonus Management
        
        /// <summary>
        /// Add bonus to entity stat
        /// </summary>
        public void AddBonus(string entityId, StatBonus bonus)
        {
            var collection = GetEntityStats(entityId);
            if (collection != null)
            {
                collection.AddBonus(bonus);
            }
            else
            {
                DebugUtils.LogWarning($"[StatManager] Cannot add bonus - entity not found: {entityId}");
            }
        }
        
        /// <summary>
        /// Remove specific bonus from entity
        /// </summary>
        public bool RemoveBonus(string entityId, StatBonus bonus)
        {
            var collection = GetEntityStats(entityId);
            return collection?.RemoveBonus(bonus) ?? false;
        }
        
        /// <summary>
        /// Remove all bonuses from source for entity
        /// </summary>
        public void RemoveBonusesFromSource(string entityId, string sourceId)
        {
            var collection = GetEntityStats(entityId);
            collection?.RemoveBonusesFromSource(sourceId);
        }
        
        /// <summary>
        /// Remove bonuses from source for all entities
        /// </summary>
        public void RemoveBonusesFromSourceGlobal(string sourceId)
        {
            foreach (var collection in entityStats.Values)
            {
                collection.RemoveBonusesFromSource(sourceId);
            }
            
            DebugUtils.Log($"[StatManager] Removed global bonuses from source: {sourceId}");
        }
        
        #endregion
        
        #region Bulk Operations
        
        /// <summary>
        /// Apply bonuses to multiple entities
        /// </summary>
        public void ApplyBonusesToEntities(IEnumerable<string> entityIds, StatBonus bonus)
        {
            foreach (var entityId in entityIds)
            {
                AddBonus(entityId, bonus);
            }
        }
        
        /// <summary>
        /// Recalculate stats for all entities
        /// </summary>
        public void RecalculateAllStats()
        {
            foreach (var collection in entityStats.Values)
            {
                collection.RecalculateAll();
            }
            
            DebugUtils.Log("[StatManager] Recalculated all entity stats");
        }
        
        #endregion
        
        #region Resource Management
        
        /// <summary>
        /// Damage entity (reduce HP)
        /// </summary>
        public float DamageEntity(string entityId, float damage)
        {
            var collection = GetEntityStats(entityId);
            if (collection == null) return 0f;
            
            float currentHP = collection.GetStat(StatId.KhiHuyet);
            float actualDamage = Mathf.Min(damage, currentHP);
            
            collection.ModifyStat(StatId.KhiHuyet, -actualDamage);
            
            // Check if entity died
            var finalHp = collection.GetStat(StatId.KhiHuyet);
            if (finalHp <= 0)
            {
                EventBus.Dispatch(new EntityDeathEvent(entityId, finalHp));
            }
            
            return actualDamage;
        }
        
        /// <summary>
        /// Heal entity (restore HP)
        /// </summary>
        public float HealEntity(string entityId, float healAmount)
        {
            var collection = GetEntityStats(entityId);
            if (collection == null) return 0f;
            
            float currentHP = collection.GetStat(StatId.KhiHuyet);
            float maxHP = collection.GetStat(StatId.KhiHuyetMax);
            float actualHeal = Mathf.Min(healAmount, maxHP - currentHP);
            
            collection.ModifyStat(StatId.KhiHuyet, actualHeal);
            return actualHeal;
        }
        
        /// <summary>
        /// Restore entity resources to full
        /// </summary>
        public void RestoreEntity(string entityId)
        {
            var collection = GetEntityStats(entityId);
            if (collection == null) return;
            
            collection.RestoreResource(StatId.KhiHuyet, StatId.KhiHuyetMax);
            collection.RestoreResource(StatId.LinhLuc, StatId.LinhLucMax);
            collection.RestoreResource(StatId.ThoNguyen, StatId.ThoNguyenMax);
            
            DebugUtils.Log($"[StatManager] Restored all resources for {entityId}");
        }
        
        #endregion
        
        #region Persistence
        
        /// <summary>
        /// Auto save all entity stats
        /// </summary>
        private void AutoSaveStats()
        {
            foreach (var kvp in entityStats)
            {
                SaveEntityStats(kvp.Key);
            }
        }
        
        /// <summary>
        /// Save specific entity stats to PlayerPrefs
        /// </summary>
        public void SaveEntityStats(string entityId)
        {
            var collection = GetEntityStats(entityId);
            if (collection == null) return;
            
            var data = collection.GetSerializableData();
            string json = JsonUtility.ToJson(new SerializableStatData(data));
            PlayerPrefs.SetString($"Stats_{entityId}", json);
            
            DebugUtils.Log($"[StatManager] Saved stats for {entityId}");
        }
        
        /// <summary>
        /// Load entity stats from PlayerPrefs
        /// </summary>
        public bool LoadEntityStats(string entityId)
        {
            string key = $"Stats_{entityId}";
            if (!PlayerPrefs.HasKey(key)) return false;
            
            try
            {
                string json = PlayerPrefs.GetString(key);
                var data = JsonUtility.FromJson<SerializableStatData>(json);
                
                var collection = GetEntityStats(entityId) ?? RegisterEntity(entityId);
                collection.LoadFromSerializableData(data.ToDictionary());
                
                DebugUtils.Log($"[StatManager] Loaded stats for {entityId}");
                return true;
            }
            catch (Exception ex)
            {
                DebugUtils.LogError($"[StatManager] Failed to load stats for {entityId}: {ex.Message}");
                return false;
            }
        }
        
        #endregion
        
        #region Events
        
        private void OnStatChanged(StatChangedEvent evt)
        {
            DebugUtils.Log($"[StatManager] Stat changed: {evt.EntityId}.{evt.StatId} = {evt.NewValue}");
        }
        
        private void OnBonusAdded(StatBonusAddedEvent evt)
        {
            DebugUtils.Log($"[StatManager] Bonus added: {evt.EntityId} received {evt.Bonus}");
        }
        
        private void OnBonusRemoved(StatBonusRemovedEvent evt)
        {
            DebugUtils.Log($"[StatManager] Bonus removed: {evt.EntityId} lost {evt.Bonus}");
        }
        
        #endregion
        
        #region Debug & Validation
        
        /// <summary>
        /// Validate all entity stats
        /// </summary>
        public bool ValidateAllStats()
        {
            bool allValid = true;
            
            foreach (var kvp in entityStats)
            {
                if (!kvp.Value.ValidateStats())
                {
                    DebugUtils.LogWarning($"[StatManager] Invalid stats detected for entity: {kvp.Key}");
                    allValid = false;
                }
            }
            
            return allValid;
        }
        
        /// <summary>
        /// Log stats for all entities
        /// </summary>
        public void LogAllEntityStats()
        {
            DebugUtils.Log($"[StatManager] Stats for {entityStats.Count} entities:");
            foreach (var kvp in entityStats)
            {
                kvp.Value.LogAllStats();
            }
        }
        
        /// <summary>
        /// Get debug info
        /// </summary>
        public string GetDebugInfo()
        {
            return $"StatManager: {entityStats.Count} entities registered, Auto-save: {autoSaveInterval}s";
        }
        
        #endregion
        
        #region Helper Classes
        
        [Serializable]
        private class SerializableStatData
        {
            public StatIdValuePair[] stats;
            
            public SerializableStatData(Dictionary<StatId, float> statDict)
            {
                stats = new StatIdValuePair[statDict.Count];
                int i = 0;
                foreach (var kvp in statDict)
                {
                    stats[i] = new StatIdValuePair { statId = kvp.Key, value = kvp.Value };
                    i++;
                }
            }
            
            public Dictionary<StatId, float> ToDictionary()
            {
                var dict = new Dictionary<StatId, float>();
                foreach (var pair in stats)
                {
                    dict[pair.statId] = pair.value;
                }
                return dict;
            }
        }
        
        [Serializable]
        private struct StatIdValuePair
        {
            public StatId statId;
            public float value;
        }
        
        #endregion
    }
}
