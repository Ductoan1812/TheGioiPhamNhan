using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Foundation.Events;
using Foundation.Utils;
using Foundation.Architecture;
using GameSystems.Progression.Core;
using GameSystems.Stats;

namespace GameSystems.Progression
{
    /// <summary>
    /// ProgressionManager - quản lý tổng hợp hệ thống progression.
    /// Bao gồm milestones, achievements, rewards system.
    /// </summary>
    public class ProgressionManager : Singleton<ProgressionManager>
    {
        [Header("Progression Configuration")]
        [SerializeField] private ProgressionMilestone[] milestones;
        [SerializeField] private bool autoProcessMilestones = true;
        [SerializeField] private bool debugMode = false;
        
        [Header("Rewards Settings")]
        [SerializeField] private bool autoApplyRewards = true;
        [SerializeField] private float rewardProcessDelay = 0.5f;
        
        // Milestone tracking
        private Dictionary<string, List<ProgressionMilestone>> entityMilestones = new Dictionary<string, List<ProgressionMilestone>>();
        private Dictionary<string, HashSet<string>> claimedMilestones = new Dictionary<string, HashSet<string>>();
        
        // Pending rewards
        private Queue<PendingReward> pendingRewards = new Queue<PendingReward>();
        private float nextRewardProcessTime;
        
        // Events
        public event Action<string, ProgressionMilestone> OnMilestoneUnlocked;
        public event Action<string, ProgressionMilestone> OnMilestoneClaimed;
        public event Action<string, ProgressionReward> OnRewardApplied;
        
        protected override void Awake()
        {
            base.Awake();
            InitializeManager();
        }
        
        private void Start()
        {
            SubscribeToEvents();
        }
        
        private void Update()
        {
            ProcessPendingRewards();
        }
        
        protected override void OnDestroy()
        {
            UnsubscribeFromEvents();
            base.OnDestroy();
        }
        
        private void InitializeManager()
        {
            InitializeMilestones();
            nextRewardProcessTime = Time.time + rewardProcessDelay;
            
            DebugUtils.Log($"[ProgressionManager] Initialized with {milestones.Length} milestones");
        }
        
        private void SubscribeToEvents()
        {
            if (ExperienceManager.HasInstance)
            {
                var expManager = ExperienceManager.Instance;
                expManager.OnLevelUp += OnEntityLevelUp;
                expManager.OnRealmAdvanced += OnEntityRealmAdvanced;
            }
            
            EventBus.Subscribe<LevelUpEvent>(OnLevelUpEvent);
            EventBus.Subscribe<RealmAdvancedEvent>(OnRealmAdvancedEvent);
        }
        
        private void UnsubscribeFromEvents()
        {
            if (ExperienceManager.HasInstance)
            {
                var expManager = ExperienceManager.Instance;
                expManager.OnLevelUp -= OnEntityLevelUp;
                expManager.OnRealmAdvanced -= OnEntityRealmAdvanced;
            }
            
            EventBus.Unsubscribe<LevelUpEvent>(OnLevelUpEvent);
            EventBus.Unsubscribe<RealmAdvancedEvent>(OnRealmAdvancedEvent);
        }
        
        #region Milestone Management
        
        /// <summary>
        /// Initialize milestone system
        /// </summary>
        private void InitializeMilestones()
        {
            if (milestones == null || milestones.Length == 0)
            {
                CreateDefaultMilestones();
            }
            
            // Validate milestones
            foreach (var milestone in milestones)
            {
                if (string.IsNullOrEmpty(milestone.Id))
                {
                    DebugUtils.LogWarning($"[ProgressionManager] Milestone with empty ID found: {milestone.Name}");
                }
            }
        }
        
        /// <summary>
        /// Create default milestones if none configured
        /// </summary>
        private void CreateDefaultMilestones()
        {
            var defaultMilestones = new List<ProgressionMilestone>();
            
            // Level milestones
            for (int level = 5; level <= 100; level += 5)
            {
                var milestone = new ProgressionMilestone(
                    $"level_{level}",
                    $"Reach Level {level}",
                    level,
                    RealmMetaHelper.GetRealmForLevel(level)
                );
                defaultMilestones.Add(milestone);
            }
            
            // Realm milestones
            foreach (CultivationRealm realm in Enum.GetValues(typeof(CultivationRealm)))
            {
                if (realm == CultivationRealm.PhamNhan) continue;
                
                var milestone = new ProgressionMilestone(
                    $"realm_{realm}",
                    $"Reach {RealmMetaHelper.GetDisplayName(realm)} Realm",
                    RealmMetaHelper.GetMinLevel(realm),
                    realm
                );
                defaultMilestones.Add(milestone);
            }
            
            milestones = defaultMilestones.ToArray();
            
            DebugUtils.Log($"[ProgressionManager] Created {milestones.Length} default milestones");
        }
        
        /// <summary>
        /// Get available milestones for entity
        /// </summary>
        public ProgressionMilestone[] GetAvailableMilestones(string entityId)
        {
            if (string.IsNullOrEmpty(entityId)) return new ProgressionMilestone[0];
            
            if (!ExperienceManager.HasInstance) return new ProgressionMilestone[0];
            
            var expManager = ExperienceManager.Instance;
            int currentLevel = expManager.GetLevel(entityId);
            var currentRealm = expManager.GetRealm(entityId);
            
            return milestones.Where(m => m.CanClaim(currentLevel, currentRealm)).ToArray();
        }
        
        /// <summary>
        /// Get unclaimed milestones for entity
        /// </summary>
        public ProgressionMilestone[] GetUnclaimedMilestones(string entityId)
        {
            var available = GetAvailableMilestones(entityId);
            var claimed = GetClaimedMilestones(entityId);
            
            return available.Where(m => !claimed.Contains(m.Id)).ToArray();
        }
        
        /// <summary>
        /// Get claimed milestone IDs for entity
        /// </summary>
        public HashSet<string> GetClaimedMilestones(string entityId)
        {
            if (!claimedMilestones.TryGetValue(entityId, out var claimed))
            {
                claimed = new HashSet<string>();
                claimedMilestones[entityId] = claimed;
            }
            return claimed;
        }
        
        /// <summary>
        /// Check milestones for entity
        /// </summary>
        public void CheckMilestones(string entityId)
        {
            if (!autoProcessMilestones) return;
            
            var unclaimed = GetUnclaimedMilestones(entityId);
            
            foreach (var milestone in unclaimed)
            {
                UnlockMilestone(entityId, milestone);
            }
        }
        
        /// <summary>
        /// Unlock milestone for entity
        /// </summary>
        private void UnlockMilestone(string entityId, ProgressionMilestone milestone)
        {
            OnMilestoneUnlocked?.Invoke(entityId, milestone);
            EventBus.Dispatch(new MilestoneUnlockedEvent(entityId, milestone));
            
            if (debugMode)
            {
                DebugUtils.Log($"[ProgressionManager] {entityId} unlocked milestone: {milestone.Name}");
            }
            
            // Auto-claim if enabled
            if (autoApplyRewards)
            {
                ClaimMilestone(entityId, milestone.Id);
            }
        }
        
        /// <summary>
        /// Claim milestone reward
        /// </summary>
        public bool ClaimMilestone(string entityId, string milestoneId)
        {
            var milestone = milestones.FirstOrDefault(m => m.Id == milestoneId);
            if (milestone == null) return false;
            
            var claimed = GetClaimedMilestones(entityId);
            if (claimed.Contains(milestoneId) && !milestone.IsRepeatable) return false;
            
            // Check if can claim
            if (!ExperienceManager.HasInstance) return false;
            
            var expManager = ExperienceManager.Instance;
            int currentLevel = expManager.GetLevel(entityId);
            var currentRealm = expManager.GetRealm(entityId);
            
            if (!milestone.CanClaim(currentLevel, currentRealm)) return false;
            
            // Mark as claimed
            claimed.Add(milestoneId);
            
            // Queue rewards
            if (milestone.Rewards != null)
            {
                foreach (var reward in milestone.Rewards)
                {
                    QueueReward(entityId, reward, $"Milestone: {milestone.Name}");
                }
            }
            
            OnMilestoneClaimed?.Invoke(entityId, milestone);
            EventBus.Dispatch(new MilestoneClaimedEvent(entityId, milestone));
            
            if (debugMode)
            {
                DebugUtils.Log($"[ProgressionManager] {entityId} claimed milestone: {milestone.Name}");
            }
            
            return true;
        }
        
        #endregion
        
        #region Reward System
        
        /// <summary>
        /// Queue reward for processing
        /// </summary>
        private void QueueReward(string entityId, ProgressionReward reward, string source)
        {
            pendingRewards.Enqueue(new PendingReward
            {
                EntityId = entityId,
                Reward = reward,
                Source = source,
                QueueTime = Time.time
            });
        }
        
        /// <summary>
        /// Process pending rewards
        /// </summary>
        private void ProcessPendingRewards()
        {
            if (Time.time < nextRewardProcessTime || pendingRewards.Count == 0) return;
            
            var reward = pendingRewards.Dequeue();
            ApplyReward(reward.EntityId, reward.Reward, reward.Source);
            
            nextRewardProcessTime = Time.time + rewardProcessDelay;
        }
        
        /// <summary>
        /// Apply reward to entity
        /// </summary>
        private void ApplyReward(string entityId, ProgressionReward reward, string source)
        {
            try
            {
                switch (reward.Type)
                {
                    case ProgressionRewardType.StatBonus:
                        ApplyStatBonusReward(entityId, reward, source);
                        break;
                    
                    case ProgressionRewardType.StatPoints:
                        ApplyStatPointsReward(entityId, reward, source);
                        break;
                    
                    case ProgressionRewardType.Item:
                        ApplyItemReward(entityId, reward, source);
                        break;
                    
                    case ProgressionRewardType.Currency:
                        ApplyCurrencyReward(entityId, reward, source);
                        break;
                    
                    case ProgressionRewardType.Skill:
                        ApplySkillReward(entityId, reward, source);
                        break;
                    
                    case ProgressionRewardType.Feature:
                        ApplyFeatureReward(entityId, reward, source);
                        break;
                    
                    case ProgressionRewardType.Title:
                        ApplyTitleReward(entityId, reward, source);
                        break;
                    
                    case ProgressionRewardType.Achievement:
                        ApplyAchievementReward(entityId, reward, source);
                        break;
                    
                    default:
                        DebugUtils.LogWarning($"[ProgressionManager] Unknown reward type: {reward.Type}");
                        break;
                }
                
                OnRewardApplied?.Invoke(entityId, reward);
                EventBus.Dispatch(new RewardAppliedEvent(entityId, reward, source));
                
                if (debugMode)
                {
                    DebugUtils.Log($"[ProgressionManager] Applied {reward.Type} reward to {entityId}: {reward.Description}");
                }
            }
            catch (Exception ex)
            {
                DebugUtils.LogError($"[ProgressionManager] Error applying reward: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Apply stat bonus reward
        /// </summary>
        private void ApplyStatBonusReward(string entityId, ProgressionReward reward, string source)
        {
            if (!StatManager.HasInstance) return;
            
            if (!Enum.TryParse<StatId>(reward.TargetId, out var statId)) return;
            
            var statManager = StatManager.Instance;
            var statCollection = statManager.GetEntityStats(entityId);
            if (statCollection == null) return;
            
            var stat = statCollection.GetStat(statId);
            if (stat != null)
            {
                stat.AddStatBonus(statId, new Stats.Core.StatBonus
                {
                    id = $"progression_reward_{Guid.NewGuid()}",
                    source = source,
                    bonusType = Stats.Core.BonusType.Flat,
                    value = reward.Value,
                    duration = -1f, // Permanent
                    priority = Stats.Core.BonusPriority.PROGRESSION_REWARDS
                });
            }
        }
        
        /// <summary>
        /// Apply stat points reward
        /// </summary>
        private void ApplyStatPointsReward(string entityId, ProgressionReward reward, string source)
        {
            if (!StatManager.HasInstance) return;
            
            var statManager = StatManager.Instance;
            var statCollection = statManager.GetEntityStats(entityId);
            if (statCollection == null) return;
            
            var pointsStat = statCollection.GetStat(StatId.Points);
            if (pointsStat != null)
            {
                pointsStat.AddToBase(reward.Value);
            }
        }
        
        /// <summary>
        /// Apply item reward
        /// </summary>
        private void ApplyItemReward(string entityId, ProgressionReward reward, string source)
        {
            // Integration with inventory system
            if (GameSystems.Inventory.InventoryManager.HasInstance)
            {
                var inventoryManager = GameSystems.Inventory.InventoryManager.Instance;
                inventoryManager.AddItemToInventory(entityId, reward.TargetId, (int)reward.Value);
            }
        }
        
        /// <summary>
        /// Apply currency reward
        /// </summary>
        private void ApplyCurrencyReward(string entityId, ProgressionReward reward, string source)
        {
            // This would integrate with a currency system
            DebugUtils.Log($"[ProgressionManager] {entityId} received {reward.Value} {reward.TargetId} currency");
        }
        
        /// <summary>
        /// Apply skill reward
        /// </summary>
        private void ApplySkillReward(string entityId, ProgressionReward reward, string source)
        {
            // This would integrate with a skill system
            DebugUtils.Log($"[ProgressionManager] {entityId} unlocked skill: {reward.TargetId}");
        }
        
        /// <summary>
        /// Apply feature reward
        /// </summary>
        private void ApplyFeatureReward(string entityId, ProgressionReward reward, string source)
        {
            // This would integrate with a feature unlock system
            DebugUtils.Log($"[ProgressionManager] {entityId} unlocked feature: {reward.TargetId}");
        }
        
        /// <summary>
        /// Apply title reward
        /// </summary>
        private void ApplyTitleReward(string entityId, ProgressionReward reward, string source)
        {
            // This would integrate with a title system
            DebugUtils.Log($"[ProgressionManager] {entityId} unlocked title: {reward.TargetId}");
        }
        
        /// <summary>
        /// Apply achievement reward
        /// </summary>
        private void ApplyAchievementReward(string entityId, ProgressionReward reward, string source)
        {
            // This would integrate with an achievement system
            DebugUtils.Log($"[ProgressionManager] {entityId} unlocked achievement: {reward.TargetId}");
        }
        
        #endregion
        
        #region Event Handlers
        
        private void OnEntityLevelUp(string entityId, int oldLevel, int newLevel)
        {
            CheckMilestones(entityId);
        }
        
        private void OnEntityRealmAdvanced(string entityId, CultivationRealm oldRealm, CultivationRealm newRealm)
        {
            CheckMilestones(entityId);
        }
        
        private void OnLevelUpEvent(LevelUpEvent evt)
        {
            CheckMilestones(evt.EntityId);
        }
        
        private void OnRealmAdvancedEvent(RealmAdvancedEvent evt)
        {
            CheckMilestones(evt.EntityId);
        }
        
        #endregion
        
        #region Queries & Utilities
        
        /// <summary>
        /// Get milestone by ID
        /// </summary>
        public ProgressionMilestone GetMilestone(string milestoneId)
        {
            return milestones.FirstOrDefault(m => m.Id == milestoneId);
        }
        
        /// <summary>
        /// Get all milestones
        /// </summary>
        public ProgressionMilestone[] GetAllMilestones()
        {
            return milestones.ToArray();
        }
        
        /// <summary>
        /// Get progression summary for entity
        /// </summary>
        public string GetProgressionSummary(string entityId)
        {
            var available = GetAvailableMilestones(entityId);
            var unclaimed = GetUnclaimedMilestones(entityId);
            var claimed = GetClaimedMilestones(entityId);
            
            var summary = $"Progression Summary for {entityId}:\n";
            summary += $"  Total Milestones: {milestones.Length}\n";
            summary += $"  Available: {available.Length}\n";
            summary += $"  Unclaimed: {unclaimed.Length}\n";
            summary += $"  Claimed: {claimed.Count}\n";
            
            if (unclaimed.Length > 0)
            {
                summary += $"\n  🎯 Next Milestones:\n";
                foreach (var milestone in unclaimed.Take(3))
                {
                    summary += $"    - {milestone.Name}\n";
                }
            }
            
            return summary;
        }
        
        /// <summary>
        /// Force claim milestone (for admin/debugging)
        /// </summary>
        public void ForceClaimMilestone(string entityId, string milestoneId)
        {
            var milestone = GetMilestone(milestoneId);
            if (milestone == null) return;
            
            var claimed = GetClaimedMilestones(entityId);
            claimed.Add(milestoneId);
            
            // Apply rewards
            if (milestone.Rewards != null)
            {
                foreach (var reward in milestone.Rewards)
                {
                    ApplyReward(entityId, reward, $"Force Claim: {milestone.Name}");
                }
            }
            
            DebugUtils.Log($"[ProgressionManager] Force claimed milestone {milestoneId} for {entityId}");
        }
        
        #endregion
        
        #region Data Management
        
        /// <summary>
        /// Save progression milestone data
        /// </summary>
        public Dictionary<string, HashSet<string>> SaveMilestoneData()
        {
            return new Dictionary<string, HashSet<string>>(claimedMilestones);
        }
        
        /// <summary>
        /// Load progression milestone data
        /// </summary>
        public void LoadMilestoneData(Dictionary<string, HashSet<string>> data)
        {
            if (data != null)
            {
                claimedMilestones = data;
                DebugUtils.Log($"[ProgressionManager] Loaded milestone data for {data.Count} entities");
            }
        }
        
        /// <summary>
        /// Reset progression data for entity
        /// </summary>
        public void ResetProgressionData(string entityId)
        {
            if (claimedMilestones.ContainsKey(entityId))
            {
                claimedMilestones.Remove(entityId);
                DebugUtils.Log($"[ProgressionManager] Reset progression data for {entityId}");
            }
        }
        
        #endregion
    }
    
    /// <summary>
    /// Pending reward structure
    /// </summary>
    internal struct PendingReward
    {
        public string EntityId;
        public ProgressionReward Reward;
        public string Source;
        public float QueueTime;
    }
}
