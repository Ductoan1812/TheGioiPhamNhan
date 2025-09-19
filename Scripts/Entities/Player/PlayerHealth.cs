using UnityEngine;
using GameSystems.Stats;
using Foundation.Events;

namespace Entities.Player
{
    /// <summary>
    /// Handles player health and damage
    /// </summary>
    public class PlayerHealth : MonoBehaviour
    {
        [Header("Health Settings")]
        [SerializeField] private bool isInvulnerable = false;
        [SerializeField] private float invulnerabilityDuration = 1f;

        // Components
        private StatCollection playerStats;
        private Stat healthStat;
        
        // State
        private float currentInvulnerabilityTime;
        private bool isDead;

        // Properties
        public float CurrentHealth => healthStat?.Value ?? 0f;
        public float MaxHealth => healthStat?.BaseValue ?? 0f;
        public float HealthPercentage => MaxHealth > 0 ? CurrentHealth / MaxHealth : 0f;
        public bool IsDead => isDead;
        public bool IsInvulnerable => isInvulnerable || currentInvulnerabilityTime > 0f;

        // Events
        public System.Action<float, float> OnHealthChanged; // current, max
        public System.Action OnDeath;
        public System.Action OnRevived;

        public void Initialize(StatCollection stats)
        {
            playerStats = stats;
            healthStat = playerStats?.GetStat(StatType.Health);
            
            if (healthStat != null)
            {
                healthStat.OnValueChanged += OnHealthStatChanged;
            }
        }

        private void Update()
        {
            UpdateInvulnerability();
        }

        private void OnDestroy()
        {
            if (healthStat != null)
            {
                healthStat.OnValueChanged -= OnHealthStatChanged;
            }
        }

        private void UpdateInvulnerability()
        {
            if (currentInvulnerabilityTime > 0f)
            {
                currentInvulnerabilityTime -= Time.deltaTime;
            }
        }

        private void OnHealthStatChanged(Stat stat, float newValue)
        {
            OnHealthChanged?.Invoke(newValue, MaxHealth);
            
            // Check for death
            if (newValue <= 0f && !isDead)
            {
                Die();
            }
            
            // Publish health changed event
            EventBus.Publish(new PlayerHealthChangedEvent(newValue, MaxHealth));
        }

        /// <summary>
        /// Take damage
        /// </summary>
        public bool TakeDamage(float damage, object source = null)
        {
            if (IsInvulnerable || isDead || damage <= 0f)
            {
                return false;
            }

            // Calculate actual damage (could apply defense modifiers here)
            var actualDamage = CalculateDamage(damage);
            
            // Apply damage
            var newHealth = Mathf.Max(0f, CurrentHealth - actualDamage);
            healthStat?.SetBaseValue(newHealth);

            // Trigger invulnerability
            if (invulnerabilityDuration > 0f)
            {
                currentInvulnerabilityTime = invulnerabilityDuration;
            }

            // Publish damage event
            EventBus.Publish(new PlayerDamagedEvent(actualDamage, source, CurrentHealth));

            return true;
        }

        /// <summary>
        /// Heal player
        /// </summary>
        public bool Heal(float amount)
        {
            if (isDead || amount <= 0f)
            {
                return false;
            }

            var newHealth = Mathf.Min(MaxHealth, CurrentHealth + amount);
            healthStat?.SetBaseValue(newHealth);

            // Publish heal event
            EventBus.Publish(new PlayerHealedEvent(amount, CurrentHealth));

            return true;
        }

        /// <summary>
        /// Set max health
        /// </summary>
        public void SetMaxHealth(float maxHealth)
        {
            if (healthStat != null)
            {
                var currentPercentage = HealthPercentage;
                healthStat.SetBaseValue(maxHealth);
                
                // Maintain health percentage
                var newCurrentHealth = maxHealth * currentPercentage;
                healthStat.SetBaseValue(newCurrentHealth);
            }
        }

        /// <summary>
        /// Fully heal player
        /// </summary>
        public void FullHeal()
        {
            if (!isDead)
            {
                healthStat?.SetBaseValue(MaxHealth);
            }
        }

        /// <summary>
        /// Kill player instantly
        /// </summary>
        public void Kill()
        {
            if (!isDead)
            {
                healthStat?.SetBaseValue(0f);
            }
        }

        /// <summary>
        /// Revive player
        /// </summary>
        public void Revive(float healthPercentage = 0.5f)
        {
            if (isDead)
            {
                isDead = false;
                var reviveHealth = MaxHealth * Mathf.Clamp01(healthPercentage);
                healthStat?.SetBaseValue(reviveHealth);
                
                OnRevived?.Invoke();
                EventBus.Publish(new PlayerRevivedEvent(reviveHealth));
            }
        }

        /// <summary>
        /// Set invulnerability state
        /// </summary>
        public void SetInvulnerable(bool invulnerable, float duration = 0f)
        {
            isInvulnerable = invulnerable;
            if (duration > 0f)
            {
                currentInvulnerabilityTime = duration;
            }
        }

        private float CalculateDamage(float baseDamage)
        {
            // Apply defense calculation
            var defense = playerStats?.GetStatValue(StatType.Defense) ?? 0f;
            var damageReduction = defense / (defense + 100f); // Simple damage reduction formula
            
            return baseDamage * (1f - damageReduction);
        }

        private void Die()
        {
            if (isDead) return;
            
            isDead = true;
            OnDeath?.Invoke();
            EventBus.Publish(new PlayerDeathEvent(transform.position));
        }
    }

    /// <summary>
    /// Player health events
    /// </summary>
    public class PlayerHealthChangedEvent : GameEvent<PlayerHealthData>
    {
        public float CurrentHealth => Data.CurrentHealth;
        public float MaxHealth => Data.MaxHealth;
        public float HealthPercentage => Data.HealthPercentage;

        public PlayerHealthChangedEvent(float currentHealth, float maxHealth) 
            : base(new PlayerHealthData(currentHealth, maxHealth))
        {
        }
    }

    public class PlayerDamagedEvent : GameEvent<PlayerDamageData>
    {
        public float Damage => Data.Damage;
        public object Source => Data.Source;
        public float RemainingHealth => Data.RemainingHealth;

        public PlayerDamagedEvent(float damage, object source, float remainingHealth) 
            : base(new PlayerDamageData(damage, source, remainingHealth))
        {
        }
    }

    public class PlayerHealedEvent : GameEvent<PlayerHealData>
    {
        public float HealAmount => Data.HealAmount;
        public float CurrentHealth => Data.CurrentHealth;

        public PlayerHealedEvent(float healAmount, float currentHealth) 
            : base(new PlayerHealData(healAmount, currentHealth))
        {
        }
    }

    public class PlayerDeathEvent : GameEvent<Vector3>
    {
        public Vector3 DeathPosition => Data;

        public PlayerDeathEvent(Vector3 position) : base(position)
        {
        }
    }

    public class PlayerRevivedEvent : GameEvent<float>
    {
        public float ReviveHealth => Data;

        public PlayerRevivedEvent(float health) : base(health)
        {
        }
    }

    // Data classes
    [System.Serializable]
    public class PlayerHealthData
    {
        public float CurrentHealth { get; }
        public float MaxHealth { get; }
        public float HealthPercentage => MaxHealth > 0 ? CurrentHealth / MaxHealth : 0f;

        public PlayerHealthData(float currentHealth, float maxHealth)
        {
            CurrentHealth = currentHealth;
            MaxHealth = maxHealth;
        }
    }

    [System.Serializable]
    public class PlayerDamageData
    {
        public float Damage { get; }
        public object Source { get; }
        public float RemainingHealth { get; }

        public PlayerDamageData(float damage, object source, float remainingHealth)
        {
            Damage = damage;
            Source = source;
            RemainingHealth = remainingHealth;
        }
    }

    [System.Serializable]
    public class PlayerHealData
    {
        public float HealAmount { get; }
        public float CurrentHealth { get; }

        public PlayerHealData(float healAmount, float currentHealth)
        {
            HealAmount = healAmount;
            CurrentHealth = currentHealth;
        }
    }
}
