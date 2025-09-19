using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Foundation.Events;

namespace GameSystems.Stats
{
    /// <summary>
    /// Stat types enumeration
    /// </summary>
    public enum StatType
    {
        Health,
        Mana,
        Stamina,
        Strength,
        Defense,
        Speed,
        Intelligence,
        Luck
    }

    /// <summary>
    /// Modifier types for stat calculations
    /// </summary>
    public enum ModifierType
    {
        Flat,           // +10 Health
        PercentAdd,     // +10% Health (additive)
        PercentMult     // +10% Health (multiplicative)
    }

    /// <summary>
    /// Stat modifier for temporary or permanent changes
    /// </summary>
    [Serializable]
    public class StatModifier
    {
        [SerializeField] private float value;
        [SerializeField] private ModifierType type;
        [SerializeField] private int order;
        [SerializeField] private object source;

        public float Value => value;
        public ModifierType Type => type;
        public int Order => order;
        public object Source => source;

        public StatModifier(float value, ModifierType type, int order = 0, object source = null)
        {
            this.value = value;
            this.type = type;
            this.order = order;
            this.source = source;
        }
    }

    /// <summary>
    /// Individual stat with base value and modifiers
    /// </summary>
    [Serializable]
    public class Stat
    {
        [SerializeField] private StatType statType;
        [SerializeField] private float baseValue;
        private readonly List<StatModifier> modifiers = new();

        public StatType Type => statType;
        public float BaseValue => baseValue;
        public float Value => CalculateValue();

        /// <summary>
        /// Events
        /// </summary>
        public event Action<Stat, float> OnValueChanged;

        public Stat(StatType type, float baseValue)
        {
            this.statType = type;
            this.baseValue = baseValue;
        }

        /// <summary>
        /// Set base value
        /// </summary>
        public void SetBaseValue(float value)
        {
            var oldValue = Value;
            baseValue = value;
            
            if (Math.Abs(Value - oldValue) > 0.01f)
            {
                OnValueChanged?.Invoke(this, Value);
                EventBus.Publish(new StatChangedEvent(statType, Value, oldValue));
            }
        }

        /// <summary>
        /// Add modifier
        /// </summary>
        public void AddModifier(StatModifier modifier)
        {
            if (modifier == null) return;

            var oldValue = Value;
            modifiers.Add(modifier);
            modifiers.Sort((x, y) => x.Order.CompareTo(y.Order));

            if (Math.Abs(Value - oldValue) > 0.01f)
            {
                OnValueChanged?.Invoke(this, Value);
                EventBus.Publish(new StatChangedEvent(statType, Value, oldValue));
            }
        }

        /// <summary>
        /// Remove modifier
        /// </summary>
        public bool RemoveModifier(StatModifier modifier)
        {
            var oldValue = Value;
            var removed = modifiers.Remove(modifier);

            if (removed && Math.Abs(Value - oldValue) > 0.01f)
            {
                OnValueChanged?.Invoke(this, Value);
                EventBus.Publish(new StatChangedEvent(statType, Value, oldValue));
            }

            return removed;
        }

        /// <summary>
        /// Remove all modifiers from specific source
        /// </summary>
        public bool RemoveModifiersFromSource(object source)
        {
            var oldValue = Value;
            var removedCount = modifiers.RemoveAll(mod => mod.Source == source);

            if (removedCount > 0 && Math.Abs(Value - oldValue) > 0.01f)
            {
                OnValueChanged?.Invoke(this, Value);
                EventBus.Publish(new StatChangedEvent(statType, Value, oldValue));
            }

            return removedCount > 0;
        }

        /// <summary>
        /// Calculate final stat value with all modifiers
        /// </summary>
        private float CalculateValue()
        {
            var finalValue = baseValue;
            var percentAdd = 0f;

            // Apply flat modifiers first
            foreach (var modifier in modifiers.Where(m => m.Type == ModifierType.Flat))
            {
                finalValue += modifier.Value;
            }

            // Apply percent add modifiers
            foreach (var modifier in modifiers.Where(m => m.Type == ModifierType.PercentAdd))
            {
                percentAdd += modifier.Value;
            }

            if (percentAdd != 0)
            {
                finalValue *= (1 + percentAdd / 100f);
            }

            // Apply percent multiply modifiers
            foreach (var modifier in modifiers.Where(m => m.Type == ModifierType.PercentMult))
            {
                finalValue *= (1 + modifier.Value / 100f);
            }

            return Math.Max(0, finalValue); // Ensure non-negative
        }

        /// <summary>
        /// Get all modifiers
        /// </summary>
        public IReadOnlyList<StatModifier> GetModifiers()
        {
            return modifiers.AsReadOnly();
        }
    }

    /// <summary>
    /// Event when stat value changes
    /// </summary>
    public class StatChangedEvent : GameEvent<StatChangeData>
    {
        public StatChangedEvent(StatType statType, float newValue, float oldValue) 
            : base(new StatChangeData(statType, newValue, oldValue))
        {
        }
    }

    [Serializable]
    public class StatChangeData
    {
        public StatType StatType { get; }
        public float NewValue { get; }
        public float OldValue { get; }
        public float Delta => NewValue - OldValue;

        public StatChangeData(StatType statType, float newValue, float oldValue)
        {
            StatType = statType;
            NewValue = newValue;
            OldValue = oldValue;
        }
    }
}
