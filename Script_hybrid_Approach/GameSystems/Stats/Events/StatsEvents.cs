using System;
using UnityEngine;
using Foundation.Events;
using GameSystems.Stats.Core;

namespace GameSystems.Stats
{
    /// <summary>
    /// Event khi một stat thay đổi value
    /// </summary>
    [Serializable]
    public class StatChangedEvent : GameEvent
    {
        public string EntityId { get; private set; }
        public StatId StatId { get; private set; }
        public float OldValue { get; private set; }
        public float NewValue { get; private set; }
        public float Delta { get; private set; }
        
        public StatChangedEvent(string entityId, StatId statId, float oldValue, float newValue, string source = null) 
            : base(source ?? "StatSystem")
        {
            EntityId = entityId;
            StatId = statId;
            OldValue = oldValue;
            NewValue = newValue;
            Delta = newValue - oldValue;
        }
    }
    
    /// <summary>
    /// Event khi bonus được thêm vào stat
    /// </summary>
    [Serializable]
    public class StatBonusAddedEvent : GameEvent
    {
        public string EntityId { get; private set; }
        public StatId StatId { get; private set; }
        public StatBonus Bonus { get; private set; }
        
        public StatBonusAddedEvent(string entityId, StatId statId, StatBonus bonus, string source = null) 
            : base(source ?? "StatSystem")
        {
            EntityId = entityId;
            StatId = statId;
            Bonus = bonus;
        }
    }
    
    /// <summary>
    /// Event khi bonus được remove khỏi stat
    /// </summary>
    [Serializable]
    public class StatBonusRemovedEvent : GameEvent
    {
        public string EntityId { get; private set; }
        public StatId StatId { get; private set; }
        public StatBonus Bonus { get; private set; }
        
        public StatBonusRemovedEvent(string entityId, StatId statId, StatBonus bonus, string source = null) 
            : base(source ?? "StatSystem")
        {
            EntityId = entityId;
            StatId = statId;
            Bonus = bonus;
        }
    }
    
    /// <summary>
    /// Event khi tất cả bonus từ một source được remove
    /// </summary>
    [Serializable]
    public class StatBonusSourceRemovedEvent : GameEvent
    {
        public string EntityId { get; private set; }
        public string BonusSource { get; private set; }
        public int RemovedCount { get; private set; }
        
        public StatBonusSourceRemovedEvent(string entityId, string bonusSource, int removedCount, string source = null) 
            : base(source ?? "StatSystem")
        {
            EntityId = entityId;
            BonusSource = bonusSource;
            RemovedCount = removedCount;
        }
    }
    
    /// <summary>
    /// Event khi stats được recalculated
    /// </summary>
    [Serializable]
    public class StatsRecalculatedEvent : GameEvent
    {
        public string EntityId { get; private set; }
        public int StatCount { get; private set; }
        
        public StatsRecalculatedEvent(string entityId, int statCount, string source = null) 
            : base(source ?? "StatSystem")
        {
            EntityId = entityId;
            StatCount = statCount;
        }
    }
    
    /// <summary>
    /// Event khi entity stats được register
    /// </summary>
    [Serializable]
    public class EntityStatsRegisteredEvent : GameEvent
    {
        public string EntityId { get; private set; }
        public int InitialStatCount { get; private set; }
        
        public EntityStatsRegisteredEvent(string entityId, int initialStatCount, string source = null) 
            : base(source ?? "StatManager")
        {
            EntityId = entityId;
            InitialStatCount = initialStatCount;
        }
    }
    
    /// <summary>
    /// Event khi entity stats được unregister
    /// </summary>
    [Serializable]
    public class EntityStatsUnregisteredEvent : GameEvent
    {
        public string EntityId { get; private set; }
        
        public EntityStatsUnregisteredEvent(string entityId, string source = null) 
            : base(source ?? "StatManager")
        {
            EntityId = entityId;
        }
    }
    
    /// <summary>
    /// Event khi entity chết (HP = 0)
    /// </summary>
    [Serializable]
    public class EntityDeathEvent : GameEvent
    {
        public string EntityId { get; private set; }
        public float FinalHpValue { get; private set; }
        
        public EntityDeathEvent(string entityId, float finalHpValue, string source = null) 
            : base(source ?? "StatManager")
        {
            EntityId = entityId;
            FinalHpValue = finalHpValue;
        }
    }
}
