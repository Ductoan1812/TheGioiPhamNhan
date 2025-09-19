using System;
using UnityEngine;
using Foundation.Events;
using GameSystems.Progression.Core;

namespace GameSystems.Progression
{
    /// <summary>
    /// Event khi experience được thêm
    /// </summary>
    [Serializable]
    public class ExperienceGainedEvent : GameEvent
    {
        public string EntityId { get; private set; }
        public ExperienceType ExperienceType { get; private set; }
        public float Amount { get; private set; }
        public float TotalExperience { get; private set; }
        public string Reason { get; private set; }
        
        public ExperienceGainedEvent(string entityId, ExperienceType experienceType, float amount, float totalExperience, string reason, string source = null) 
            : base(source ?? "ExperienceManager")
        {
            EntityId = entityId;
            ExperienceType = experienceType;
            Amount = amount;
            TotalExperience = totalExperience;
            Reason = reason;
        }
    }
    
    /// <summary>
    /// Event khi level up
    /// </summary>
    [Serializable]
    public class LevelUpEvent : GameEvent
    {
        public string EntityId { get; private set; }
        public int OldLevel { get; private set; }
        public int NewLevel { get; private set; }
        public CultivationRealm CurrentRealm { get; private set; }
        public float StatBonus { get; private set; }
        
        public LevelUpEvent(string entityId, int oldLevel, int newLevel, CultivationRealm currentRealm, float statBonus, string source = null) 
            : base(source ?? "ExperienceManager")
        {
            EntityId = entityId;
            OldLevel = oldLevel;
            NewLevel = newLevel;
            CurrentRealm = currentRealm;
            StatBonus = statBonus;
        }
    }
    
    /// <summary>
    /// Event khi realm advancement
    /// </summary>
    [Serializable]
    public class RealmAdvancedEvent : GameEvent
    {
        public string EntityId { get; private set; }
        public CultivationRealm OldRealm { get; private set; }
        public CultivationRealm NewRealm { get; private set; }
        public int ResetLevel { get; private set; }
        public float StatMultiplier { get; private set; }
        
        public RealmAdvancedEvent(string entityId, CultivationRealm oldRealm, CultivationRealm newRealm, int resetLevel, float statMultiplier, string source = null) 
            : base(source ?? "ExperienceManager")
        {
            EntityId = entityId;
            OldRealm = oldRealm;
            NewRealm = newRealm;
            ResetLevel = resetLevel;
            StatMultiplier = statMultiplier;
        }
    }
    
    /// <summary>
    /// Event khi milestone được unlock
    /// </summary>
    [Serializable]
    public class MilestoneUnlockedEvent : GameEvent
    {
        public string EntityId { get; private set; }
        public ProgressionMilestone Milestone { get; private set; }
        public bool AutoClaimed { get; private set; }
        
        public MilestoneUnlockedEvent(string entityId, ProgressionMilestone milestone, bool autoClaimed, string source = null) 
            : base(source ?? "ProgressionManager")
        {
            EntityId = entityId;
            Milestone = milestone;
            AutoClaimed = autoClaimed;
        }
    }
    
    /// <summary>
    /// Event khi milestone được claim
    /// </summary>
    [Serializable]
    public class MilestoneClaimedEvent : GameEvent
    {
        public string EntityId { get; private set; }
        public ProgressionMilestone Milestone { get; private set; }
        public ProgressionReward[] AppliedRewards { get; private set; }
        
        public MilestoneClaimedEvent(string entityId, ProgressionMilestone milestone, ProgressionReward[] appliedRewards, string source = null) 
            : base(source ?? "ProgressionManager")
        {
            EntityId = entityId;
            Milestone = milestone;
            AppliedRewards = appliedRewards;
        }
    }
    
    /// <summary>
    /// Event khi reward được apply
    /// </summary>
    [Serializable]
    public class RewardAppliedEvent : GameEvent
    {
        public string EntityId { get; private set; }
        public ProgressionReward Reward { get; private set; }
        public bool Success { get; private set; }
        public string FailureReason { get; private set; }
        
        public RewardAppliedEvent(string entityId, ProgressionReward reward, bool success, string failureReason = null, string source = null) 
            : base(source ?? "ProgressionManager")
        {
            EntityId = entityId;
            Reward = reward;
            Success = success;
            FailureReason = failureReason;
        }
    }
}
