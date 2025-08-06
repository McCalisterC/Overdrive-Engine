using UnityEngine;
using System.Collections.Generic;
using FightingFramework.Events;

namespace FightingFramework.Combat
{
    [System.Serializable]
    public struct HealthData
    {
        public int currentHealth;
        public int maxHealth;
        public float healthPercentage => maxHealth > 0 ? (float)currentHealth / maxHealth : 0f;
        public bool isAlive => currentHealth > 0;
        
        public HealthData(int maxHealth)
        {
            this.maxHealth = maxHealth;
            this.currentHealth = maxHealth;
        }
    }
    
    public class HealthSystem : MonoBehaviour
    {
        [Header("Health Configuration")]
        [SerializeField] private int maxHealth = 100;
        [SerializeField] private bool canOverheal = false;
        [SerializeField] private int maxOverheal = 20;
        
        [Header("Damage Reduction")]
        [SerializeField] private float baseDamageReduction = 0f;
        [SerializeField] private int armor = 0;
        [SerializeField] private bool hasInvincibility = false;
        [SerializeField] private float invincibilityDuration = 0f;
        
        [Header("Recovery")]
        [SerializeField] private bool naturalRegeneration = false;
        [SerializeField] private float regenRate = 1f; // HP per second
        [SerializeField] private float regenDelay = 3f; // Delay after taking damage
        
        [Header("Events")]
        [SerializeField] private GameEvent onHealthChanged;
        [SerializeField] private GameEvent onDamageTaken;
        [SerializeField] private GameEvent onHealed;
        [SerializeField] private GameEvent onDeath;
        [SerializeField] private GameEvent onRevive;
        
        // State
        private HealthData health;
        private float lastDamageTime;
        private float invincibilityEndTime;
        private List<DamageModifier> damageModifiers = new List<DamageModifier>();
        
        // Events
        public System.Action<int, int> OnHealthChanged; // (newHealth, maxHealth)
        public System.Action<int, DamageType> OnDamageTaken; // (damage, damageType)
        public System.Action<int> OnHealed; // (healAmount)
        public System.Action OnDeath;
        public System.Action OnRevive;
        
        // Properties
        public int CurrentHealth => health.currentHealth;
        public int MaxHealth => health.maxHealth;
        public float HealthPercentage => health.healthPercentage;
        public bool IsAlive => health.isAlive;
        public bool IsInvincible => Time.time < invincibilityEndTime;
        public bool CanRegenerate => naturalRegeneration && Time.time > lastDamageTime + regenDelay;
        
        private void Awake()
        {
            health = new HealthData(maxHealth);
        }
        
        private void Update()
        {
            HandleRegeneration();
        }
        
        public bool TakeDamage(int damage, DamageType damageType = DamageType.Normal, GameObject source = null)
        {
            if (!IsAlive || IsInvincible || damage <= 0)
                return false;
            
            // Apply damage modifiers
            int modifiedDamage = ApplyDamageModifiers(damage, damageType);
            
            // Apply armor and damage reduction
            modifiedDamage = ApplyDamageReduction(modifiedDamage);
            
            if (modifiedDamage <= 0)
                return false;
            
            // Apply damage
            int previousHealth = health.currentHealth;
            health.currentHealth = Mathf.Max(0, health.currentHealth - modifiedDamage);
            lastDamageTime = Time.time;
            
            // Fire events
            OnDamageTaken?.Invoke(modifiedDamage, damageType);
            onDamageTaken?.Raise();
            
            OnHealthChanged?.Invoke(health.currentHealth, health.maxHealth);
            onHealthChanged?.Raise();
            
            // Check for death
            if (health.currentHealth <= 0 && previousHealth > 0)
            {
                Die();
            }
            
            return true;
        }
        
        public void Heal(int healAmount)
        {
            if (!IsAlive || healAmount <= 0)
                return;
            
            int previousHealth = health.currentHealth;
            int maxPossibleHealth = canOverheal ? health.maxHealth + maxOverheal : health.maxHealth;
            
            health.currentHealth = Mathf.Min(maxPossibleHealth, health.currentHealth + healAmount);
            
            int actualHealAmount = health.currentHealth - previousHealth;
            
            if (actualHealAmount > 0)
            {
                OnHealed?.Invoke(actualHealAmount);
                onHealed?.Raise();
                
                OnHealthChanged?.Invoke(health.currentHealth, health.maxHealth);
                onHealthChanged?.Raise();
            }
        }
        
        public void SetMaxHealth(int newMaxHealth)
        {
            if (newMaxHealth <= 0) return;
            
            float healthRatio = HealthPercentage;
            health.maxHealth = newMaxHealth;
            
            // Maintain health percentage
            health.currentHealth = Mathf.RoundToInt(newMaxHealth * healthRatio);
            
            OnHealthChanged?.Invoke(health.currentHealth, health.maxHealth);
            onHealthChanged?.Raise();
        }
        
        public void SetInvincibility(float duration)
        {
            invincibilityEndTime = Time.time + duration;
        }
        
        public void AddArmor(int armorAmount)
        {
            armor = Mathf.Max(0, armor + armorAmount);
        }
        
        public void RemoveArmor(int armorAmount)
        {
            armor = Mathf.Max(0, armor - armorAmount);
        }
        
        public void AddDamageModifier(DamageModifier modifier)
        {
            if (modifier != null)
            {
                damageModifiers.Add(modifier);
            }
        }
        
        public void RemoveDamageModifier(DamageModifier modifier)
        {
            damageModifiers.Remove(modifier);
        }
        
        private int ApplyDamageModifiers(int damage, DamageType damageType)
        {
            float modifiedDamage = damage;
            
            foreach (var modifier in damageModifiers)
            {
                if (modifier.IsValid() && modifier.AppliesTo(damageType))
                {
                    modifiedDamage = modifier.ModifyDamage(modifiedDamage);
                }
            }
            
            return Mathf.RoundToInt(modifiedDamage);
        }
        
        private int ApplyDamageReduction(int damage)
        {
            float reducedDamage = damage;
            
            // Apply base damage reduction
            reducedDamage *= (1f - baseDamageReduction);
            
            // Apply armor (flat reduction)
            reducedDamage = Mathf.Max(1, reducedDamage - armor); // Minimum 1 damage
            
            return Mathf.RoundToInt(reducedDamage);
        }
        
        private void HandleRegeneration()
        {
            if (!CanRegenerate || health.currentHealth >= health.maxHealth)
                return;
            
            float regenAmount = regenRate * Time.deltaTime;
            int healAmount = Mathf.FloorToInt(regenAmount);
            
            if (healAmount > 0)
            {
                Heal(healAmount);
            }
        }
        
        private void Die()
        {
            OnDeath?.Invoke();
            onDeath?.Raise();
        }
        
        public void Revive(int reviveHealth = -1)
        {
            if (IsAlive) return;
            
            if (reviveHealth < 0)
                reviveHealth = health.maxHealth;
            
            health.currentHealth = Mathf.Min(health.maxHealth, reviveHealth);
            
            OnRevive?.Invoke();
            onRevive?.Raise();
            
            OnHealthChanged?.Invoke(health.currentHealth, health.maxHealth);
            onHealthChanged?.Raise();
        }
        
        public void ResetHealth()
        {
            health.currentHealth = health.maxHealth;
            lastDamageTime = 0f;
            invincibilityEndTime = 0f;
            
            OnHealthChanged?.Invoke(health.currentHealth, health.maxHealth);
            onHealthChanged?.Raise();
        }
        
        // Debug and utility methods
        public void SetCurrentHealth(int newHealth)
        {
            health.currentHealth = Mathf.Clamp(newHealth, 0, health.maxHealth);
            OnHealthChanged?.Invoke(health.currentHealth, health.maxHealth);
            onHealthChanged?.Raise();
        }
        
        [System.Serializable]
        public class DamageModifier
        {
            public string modifierName;
            public DamageType applicableDamageType = DamageType.Normal;
            public bool appliesToAllTypes = true;
            public float damageMultiplier = 1f;
            public float flatDamageChange = 0f;
            public float duration = -1f; // -1 for permanent
            public bool stackable = false;
            
            private float startTime;
            
            public DamageModifier(string name, float multiplier = 1f, float flatChange = 0f, float duration = -1f)
            {
                this.modifierName = name;
                this.damageMultiplier = multiplier;
                this.flatDamageChange = flatChange;
                this.duration = duration;
                this.startTime = Time.time;
            }
            
            public bool IsValid()
            {
                return duration < 0 || Time.time < startTime + duration;
            }
            
            public bool AppliesTo(DamageType damageType)
            {
                return appliesToAllTypes || applicableDamageType == damageType;
            }
            
            public float ModifyDamage(float damage)
            {
                return (damage * damageMultiplier) + flatDamageChange;
            }
        }
    }
}