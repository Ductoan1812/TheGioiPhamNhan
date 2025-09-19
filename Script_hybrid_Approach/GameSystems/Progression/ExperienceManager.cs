using System;
using System.Collections.Generic;
using UnityEngine;
using Foundation.Events;
using Foundation.Utils;
using Foundation.Architecture;
using GameSystems.Progression.Core;
using GameSystems.Stats;
using GameSystems.Stats.Core;

namespace GameSystems.Progression
{
    /// <summary>
    /// ExperienceManager - quản lý hệ thống kinh nghiệm và level up.
    /// Cải tiến từ logic level/exp hiện tại với Foundation patterns.
    /// </summary>
    public class ExperienceManager : Singleton<ExperienceManager>
    {
        [Header("Experience Configuration")]
        [SerializeField] private ExperienceConfig experienceConfig;
        [SerializeField] private bool autoLevelUp = true;
        [SerializeField] private bool debugMode = false;
        
        [Header("Integration Settings")]
        [SerializeField] private bool syncWithStatsSystem = true;
        [SerializeField] private bool autoRealmAdvancement = true;
        [SerializeField] private float expGainDelay = 0.1f; // Batch exp gains
        
        // Experience data for all entities
        private Dictionary<string, ProgressionData> entityProgression = new Dictionary<string, ProgressionData>();
        
        // Batched experience gains
        private Dictionary<string, float> pendingExpGains = new Dictionary<string, float>();
        private float nextExpProcessTime;
        
        // Events
        public event Action<string, float, ExperienceType> OnExperienceGained;
        public event Action<string, int, int> OnLevelUp; // (entityId, oldLevel, newLevel)
        public event Action<string, CultivationRealm, CultivationRealm> OnRealmAdvanced;
        public event Action<string, ProgressionMilestone> OnMilestoneReached;
        
        protected override void Awake()
        {
            base.Awake();
            InitializeManager();
        }
        
        private void Update()
        {
            ProcessPendingExperience();
        }
        
        private void InitializeManager()
        {
            if (experienceConfig == null)
            {
                experienceConfig = new ExperienceConfig();
                DebugUtils.LogWarning("[ExperienceManager] No experience config found, using defaults");
            }
            
            nextExpProcessTime = Time.time + expGainDelay;
            
            DebugUtils.Log("[ExperienceManager] Initialized experience system");
        }
        
        #region Experience Management
        
        /// <summary>
        /// Get or create progression data for entity
        /// </summary>
        public ProgressionData GetProgressionData(string entityId, bool createIfNotExists = true)
        {
            if (string.IsNullOrEmpty(entityId)) return null;
            
            if (!entityProgression.TryGetValue(entityId, out var progression))
            {
                if (!createIfNotExists) return null;
                
                progression = new ProgressionData(entityId);
                entityProgression[entityId] = progression;
                
                // Sync with stats system if available
                if (syncWithStatsSystem)
                {
                    SyncProgressionWithStats(entityId, progression);
                }
            }
            
            return progression;
        }
        
        /// <summary>
        /// Add experience to entity
        /// </summary>
        public void AddExperience(string entityId, float amount, ExperienceType expType = ExperienceType.Combat, string source = "")
        {
            if (string.IsNullOrEmpty(entityId) || amount <= 0) return;
            
            // Apply experience type multiplier
            float finalAmount = amount * experienceConfig.GetMultiplier(expType);
            
            // Batch experience gains for performance
            if (!pendingExpGains.ContainsKey(entityId))
            {
                pendingExpGains[entityId] = 0f;
            }
            pendingExpGains[entityId] += finalAmount;
            
            OnExperienceGained?.Invoke(entityId, finalAmount, expType);
            
            EventBus.Dispatch(new ExperienceGainedEvent(entityId, finalAmount, expType, source));
            
            if (debugMode)
            {
                DebugUtils.Log($"[ExperienceManager] {entityId} gained {finalAmount:F1} exp ({expType}) - Source: {source}");
            }
        }
        
        /// <summary>
        /// Process batched experience gains
        /// </summary>
        private void ProcessPendingExperience()
        {
            if (Time.time < nextExpProcessTime || pendingExpGains.Count == 0) return;
            
            var expGains = new Dictionary<string, float>(pendingExpGains);
            pendingExpGains.Clear();
            nextExpProcessTime = Time.time + expGainDelay;
            
            foreach (var kvp in expGains)
            {
                ProcessExperienceGain(kvp.Key, kvp.Value);
            }
        }
        
        /// <summary>
        /// Process actual experience gain and level ups
        /// </summary>
        private void ProcessExperienceGain(string entityId, float expAmount)
        {
            var progression = GetProgressionData(entityId);
            if (progression == null) return;
            
            progression.AddExperience(expAmount);
            
            // Check for level ups
            if (autoLevelUp)
            {
                CheckLevelUp(entityId, progression);
            }
            
            // Sync with stats system
            if (syncWithStatsSystem)
            {
                UpdateStatsFromProgression(entityId, progression);
            }
        }
        
        /// <summary>
        /// Check and process level ups
        /// </summary>
        private void CheckLevelUp(string entityId, ProgressionData progression)
        {
            bool leveledUp = false;
            int oldLevel = progression.Level;
            
            while (true)
            {
                float requiredExp = experienceConfig.GetExpRequiredForLevel(progression.Level + 1, progression.Realm);
                
                if (progression.Experience >= requiredExp)
                {
                    progression.Experience -= requiredExp;
                    progression.Level++;
                    leveledUp = true;
                    
                    if (debugMode)
                    {
                        DebugUtils.Log($"[ExperienceManager] {entityId} leveled up to {progression.Level}!");
                    }
                }
                else
                {
                    break;
                }
            }
            
            if (leveledUp)
            {
                OnLevelUp?.Invoke(entityId, oldLevel, progression.Level);
                EventBus.Dispatch(new LevelUpEvent(entityId, oldLevel, progression.Level));
                
                // Check realm advancement
                if (autoRealmAdvancement)
                {
                    CheckRealmAdvancement(entityId, progression);
                }
                
                // Apply level up bonuses
                ApplyLevelUpBonuses(entityId, oldLevel, progression.Level);
            }
        }
        
        /// <summary>
        /// Check and process realm advancement
        /// </summary>
        private void CheckRealmAdvancement(string entityId, ProgressionData progression)
        {
            var currentRealm = progression.Realm;
            
            if (RealmMetaHelper.CanAdvanceRealm(progression.Level, currentRealm))
            {
                var nextRealm = RealmMetaHelper.GetNextRealm(currentRealm);
                if (nextRealm.HasValue)
                {
                    var oldRealm = progression.Realm;
                    progression.Realm = nextRealm.Value;
                    
                    OnRealmAdvanced?.Invoke(entityId, oldRealm, nextRealm.Value);
                    EventBus.Dispatch(new RealmAdvancedEvent(entityId, oldRealm, nextRealm.Value));
                    
                    // Apply realm advancement bonuses
                    ApplyRealmAdvancementBonuses(entityId, oldRealm, nextRealm.Value);
                    
                    if (debugMode)
                    {
                        DebugUtils.Log($"[ExperienceManager] {entityId} advanced to realm {RealmMetaHelper.GetDisplayName(nextRealm.Value)}!");
                    }
                }
            }
        }
        
        /// <summary>
        /// Apply level up stat bonuses
        /// </summary>
        private void ApplyLevelUpBonuses(string entityId, int oldLevel, int newLevel)
        {
            if (!StatManager.HasInstance) return;
            
            var statManager = StatManager.Instance;
            var statCollection = statManager.GetEntityStats(entityId);
            if (statCollection == null) return;
            
            int levelDiff = newLevel - oldLevel;
            
            // Apply per-level bonuses
            var levelBonuses = new[]
            {
                (StatId.KhiHuyetMax, 20f), // +20 HP per level
                (StatId.LinhLucMax, 10f),  // +10 MP per level  
                (StatId.ThoNguyenMax, 5f), // +5 Stamina per level
                (StatId.CongVatLy, 2f),    // +2 Attack per level
                (StatId.PhongVatLy, 1f),   // +1 Defense per level
            };
            
            foreach (var (statId, bonusPerLevel) in levelBonuses)
            {
                var stat = statCollection.GetStat(statId);
                if (stat != null)
                {
                    float totalBonus = bonusPerLevel * levelDiff;
                    stat.AddStatBonus(statId, new StatBonus
                    {
                        id = $"level_bonus_{newLevel}_{statId}",
                        source = $"Level {newLevel}",
                        bonusType = BonusType.Flat,
                        value = totalBonus,
                        duration = -1f, // Permanent
                        priority = BonusPriority.LEVEL_SCALING
                    });
                }
            }
            
            // Add allocatable stat points
            var pointsStat = statCollection.GetStat(StatId.Points);
            if (pointsStat != null)
            {
                float pointsToAdd = levelDiff * 5f; // 5 points per level
                pointsStat.AddToBase(pointsToAdd);
            }
        }
        
        /// <summary>
        /// Apply realm advancement bonuses
        /// </summary>
        private void ApplyRealmAdvancementBonuses(string entityId, CultivationRealm oldRealm, CultivationRealm newRealm)
        {
            if (!StatManager.HasInstance) return;
            
            var statManager = StatManager.Instance;
            var statCollection = statManager.GetEntityStats(entityId);
            if (statCollection == null) return;
            
            // Realm advancement gives significant bonuses
            var realmBonuses = new[]
            {
                (StatId.KhiHuyetMax, 100f), // +100 HP
                (StatId.LinhLucMax, 50f),   // +50 MP
                (StatId.CongVatLy, 10f),    // +10 Attack
                (StatId.PhongVatLy, 10f),   // +10 Defense
                (StatId.TocDo, 0.1f),       // +0.1 Speed
            };
            
            foreach (var (statId, bonus) in realmBonuses)
            {
                var stat = statCollection.GetStat(statId);
                if (stat != null)
                {
                    stat.AddStatBonus(statId, new StatBonus
                    {
                        id = $"realm_bonus_{newRealm}_{statId}",
                        source = $"Realm: {RealmMetaHelper.GetDisplayName(newRealm)}",
                        bonusType = BonusType.Flat,
                        value = bonus,
                        duration = -1f, // Permanent
                        priority = BonusPriority.REALM_BONUS
                    });
                }
            }
        }
        
        #endregion
        
        #region Stats Integration
        
        /// <summary>
        /// Sync progression data with stats system
        /// </summary>
        private void SyncProgressionWithStats(string entityId, ProgressionData progression)
        {
            if (!StatManager.HasInstance) return;
            
            var statManager = StatManager.Instance;
            var statCollection = statManager.GetEntityStats(entityId);
            if (statCollection == null) return;
            
            // Update cultivation stats
            UpdateStatValue(statCollection, StatId.TuVi, progression.Level);
            UpdateStatValue(statCollection, StatId.DaoHanh, progression.Experience);
            
            // Calculate required exp for next level
            float requiredExp = experienceConfig.GetExpRequiredForLevel(progression.Level + 1, progression.Realm);
            UpdateStatValue(statCollection, StatId.TuViCan, requiredExp);
        }
        
        /// <summary>
        /// Update stats from progression changes
        /// </summary>
        private void UpdateStatsFromProgression(string entityId, ProgressionData progression)
        {
            if (!StatManager.HasInstance) return;
            
            var statManager = StatManager.Instance;
            var statCollection = statManager.GetEntityStats(entityId);
            if (statCollection == null) return;
            
            // Update experience and level stats
            var daoHanhStat = statCollection.GetStat(StatId.DaoHanh);
            if (daoHanhStat != null)
            {
                daoHanhStat.SetBaseValue(progression.Experience);
            }
            
            var tuViStat = statCollection.GetStat(StatId.TuVi);
            if (tuViStat != null)
            {
                tuViStat.SetBaseValue(progression.Level);
            }
            
            // Update required exp
            float requiredExp = experienceConfig.GetExpRequiredForLevel(progression.Level + 1, progression.Realm);
            var tuViCanStat = statCollection.GetStat(StatId.TuViCan);
            if (tuViCanStat != null)
            {
                tuViCanStat.SetBaseValue(requiredExp);
            }
        }
        
        /// <summary>
        /// Helper to update stat value
        /// </summary>
        private void UpdateStatValue(StatCollection statCollection, StatId statId, float value)
        {
            var stat = statCollection.GetStat(statId);
            if (stat != null)
            {
                stat.SetBaseValue(value);
            }
        }
        
        #endregion
        
        #region Queries & Utilities
        
        /// <summary>
        /// Get current level for entity
        /// </summary>
        public int GetLevel(string entityId)
        {
            var progression = GetProgressionData(entityId, false);
            return progression?.Level ?? 1;
        }
        
        /// <summary>
        /// Get current experience for entity
        /// </summary>
        public float GetExperience(string entityId)
        {
            var progression = GetProgressionData(entityId, false);
            return progression?.Experience ?? 0f;
        }
        
        /// <summary>
        /// Get current realm for entity
        /// </summary>
        public CultivationRealm GetRealm(string entityId)
        {
            var progression = GetProgressionData(entityId, false);
            return progression?.Realm ?? CultivationRealm.PhamNhan;
        }
        
        /// <summary>
        /// Get experience required for next level
        /// </summary>
        public float GetExpRequiredForNextLevel(string entityId)
        {
            var progression = GetProgressionData(entityId, false);
            if (progression == null) return experienceConfig.GetExpRequiredForLevel(2, CultivationRealm.PhamNhan);
            
            return experienceConfig.GetExpRequiredForLevel(progression.Level + 1, progression.Realm);
        }
        
        /// <summary>
        /// Get experience progress as percentage (0-1)
        /// </summary>
        public float GetExpProgress(string entityId)
        {
            var progression = GetProgressionData(entityId, false);
            if (progression == null) return 0f;
            
            float required = GetExpRequiredForNextLevel(entityId);
            if (required <= 0) return 1f;
            
            return Mathf.Clamp01(progression.Experience / required);
        }
        
        /// <summary>
        /// Check if entity can level up
        /// </summary>
        public bool CanLevelUp(string entityId)
        {
            var progression = GetProgressionData(entityId, false);
            if (progression == null) return false;
            
            float required = GetExpRequiredForNextLevel(entityId);
            return progression.Experience >= required;
        }
        
        /// <summary>
        /// Force level up (for debugging/admin)
        /// </summary>
        public void ForceLevelUp(string entityId, int levels = 1)
        {
            var progression = GetProgressionData(entityId);
            if (progression == null) return;
            
            int oldLevel = progression.Level;
            progression.Level += levels;
            
            // Clear excess experience
            progression.Experience = 0f;
            
            OnLevelUp?.Invoke(entityId, oldLevel, progression.Level);
            EventBus.Dispatch(new LevelUpEvent(entityId, oldLevel, progression.Level));
            
            // Check realm advancement
            if (autoRealmAdvancement)
            {
                CheckRealmAdvancement(entityId, progression);
            }
            
            // Apply bonuses
            ApplyLevelUpBonuses(entityId, oldLevel, progression.Level);
            
            // Sync with stats
            if (syncWithStatsSystem)
            {
                UpdateStatsFromProgression(entityId, progression);
            }
            
            DebugUtils.Log($"[ExperienceManager] Force leveled {entityId} by {levels} levels (now level {progression.Level})");
        }
        
        /// <summary>
        /// Set experience directly (for admin/debugging)
        /// </summary>
        public void SetExperience(string entityId, float experience)
        {
            var progression = GetProgressionData(entityId);
            if (progression == null) return;
            
            progression.Experience = Mathf.Max(0f, experience);
            
            if (autoLevelUp)
            {
                CheckLevelUp(entityId, progression);
            }
            
            if (syncWithStatsSystem)
            {
                UpdateStatsFromProgression(entityId, progression);
            }
        }
        
        /// <summary>
        /// Get progression summary for entity
        /// </summary>
        public string GetProgressionSummary(string entityId)
        {
            var progression = GetProgressionData(entityId, false);
            if (progression == null) return $"No progression data for {entityId}";
            
            float required = GetExpRequiredForNextLevel(entityId);
            float progress = GetExpProgress(entityId) * 100f;
            string realmName = RealmMetaHelper.GetDisplayName(progression.Realm);
            
            var summary = $"Progression for {entityId}:\n";
            summary += $"  Level: {progression.Level}\n";
            summary += $"  Realm: {realmName}\n";
            summary += $"  Experience: {progression.Experience:F1} / {required:F1} ({progress:F1}%)\n";
            summary += $"  Total Lifetime Exp: {progression.TotalLifetimeExp:F1}\n";
            
            if (CanLevelUp(entityId))
            {
                summary += $"  🎉 Ready to level up!\n";
            }
            
            if (RealmMetaHelper.CanAdvanceRealm(progression.Level, progression.Realm))
            {
                var nextRealm = RealmMetaHelper.GetNextRealm(progression.Realm);
                if (nextRealm.HasValue)
                {
                    summary += $"  ⭐ Can advance to {RealmMetaHelper.GetDisplayName(nextRealm.Value)}!\n";
                }
            }
            
            return summary;
        }
        
        #endregion
        
        #region Data Management
        
        /// <summary>
        /// Save progression data for entity
        /// </summary>
        public ProgressionData SaveProgressionData(string entityId)
        {
            var progression = GetProgressionData(entityId, false);
            return progression; // Return copy for serialization
        }
        
        /// <summary>
        /// Load progression data for entity
        /// </summary>
        public void LoadProgressionData(string entityId, ProgressionData data)
        {
            if (string.IsNullOrEmpty(entityId) || data == null) return;
            
            entityProgression[entityId] = data;
            
            // Sync with stats system
            if (syncWithStatsSystem)
            {
                SyncProgressionWithStats(entityId, data);
            }
            
            DebugUtils.Log($"[ExperienceManager] Loaded progression data for {entityId}: Level {data.Level}, Realm {RealmMetaHelper.GetDisplayName(data.Realm)}");
        }
        
        /// <summary>
        /// Remove progression data for entity
        /// </summary>
        public void RemoveProgressionData(string entityId)
        {
            if (entityProgression.ContainsKey(entityId))
            {
                entityProgression.Remove(entityId);
                DebugUtils.Log($"[ExperienceManager] Removed progression data for {entityId}");
            }
        }
        
        /// <summary>
        /// Get all entities with progression data
        /// </summary>
        public string[] GetAllEntityIds()
        {
            return new List<string>(entityProgression.Keys).ToArray();
        }
        
        #endregion
    }
}
