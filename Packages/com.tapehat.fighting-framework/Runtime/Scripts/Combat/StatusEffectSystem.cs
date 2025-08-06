using System.Collections.Generic;
using UnityEngine;
using static FightingFramework.Combat.AttackData;

namespace FightingFramework.Combat
{
    [System.Serializable]
    public class StatusEffect
    {
        public StatusEffectType type;
        public float duration;
        public float remainingTime;
        public float intensity;
        public int tickDamage;
        public float tickRate;
        public float lastTickTime;
        public GameObject source;
        public bool isActive = true;
        
        public StatusEffect(StatusEffectData data, GameObject source = null)
        {
            this.type = data.effectType;
            this.duration = data.duration;
            this.remainingTime = data.duration;
            this.intensity = data.intensity;
            this.tickDamage = data.tickDamage;
            this.tickRate = data.tickRate;
            this.source = source;
            this.lastTickTime = Time.time;
            this.isActive = true;
        }
        
        public bool ShouldTick()
        {
            return isActive && tickRate > 0 && Time.time >= lastTickTime + tickRate;
        }
        
        public void Tick()
        {
            lastTickTime = Time.time;
        }
        
        public bool IsExpired()
        {
            return remainingTime <= 0 || !isActive;
        }
        
        public void Update()
        {
            if (isActive)
            {
                remainingTime -= Time.deltaTime;
                if (remainingTime <= 0)
                {
                    isActive = false;
                }
            }
        }
    }
    
    public class StatusEffectSystem : MonoBehaviour
    {
        [Header("Status Effect Settings")]
        [SerializeField] private bool allowMultipleEffects = true;
        [SerializeField] private bool allowStacking = false;
        [SerializeField] private int maxEffectsPerType = 3;
        
        [Header("Visual Feedback")]
        [SerializeField] private GameObject poisonEffect;
        [SerializeField] private GameObject burnEffect;
        [SerializeField] private GameObject freezeEffect;
        [SerializeField] private GameObject stunEffect;
        
        private List<StatusEffect> activeEffects = new List<StatusEffect>();
        private HealthSystem healthSystem;
        
        // Events
        public System.Action<StatusEffect> OnStatusEffectApplied;
        public System.Action<StatusEffect> OnStatusEffectRemoved;
        public System.Action<StatusEffect> OnStatusEffectTicked;
        
        // Properties
        public List<StatusEffect> ActiveEffects => new List<StatusEffect>(activeEffects);
        public bool HasActiveEffects => activeEffects.Count > 0;
        
        private void Awake()
        {
            healthSystem = GetComponent<HealthSystem>();
        }
        
        private void Update()
        {
            UpdateStatusEffects();
        }
        
        public void ApplyStatusEffect(StatusEffectData effectData, GameObject source = null)
        {
            var newEffect = new StatusEffect(effectData, source);
            ApplyStatusEffect(newEffect);
        }
        
        public void ApplyStatusEffect(StatusEffect effect)
        {
            if (effect == null) return;
            
            // Check if we already have this type of effect
            var existingEffects = GetStatusEffectsOfType(effect.type);
            
            if (!allowStacking && existingEffects.Count > 0)
            {
                // Replace or refresh existing effect
                var existingEffect = existingEffects[0];
                if (effect.duration > existingEffect.remainingTime)
                {
                    // Replace with longer duration effect
                    RemoveStatusEffect(existingEffect);
                    AddNewStatusEffect(effect);
                }
                else
                {
                    // Refresh existing effect
                    existingEffect.remainingTime = effect.duration;
                }
            }
            else if (existingEffects.Count < maxEffectsPerType)
            {
                // Add new effect
                AddNewStatusEffect(effect);
            }
        }
        
        private void AddNewStatusEffect(StatusEffect effect)
        {
            activeEffects.Add(effect);
            ApplyEffectModifiers(effect);
            OnStatusEffectApplied?.Invoke(effect);
            
            // Start visual effects
            StartVisualEffect(effect);
        }
        
        public void RemoveStatusEffect(StatusEffect effect)
        {
            if (activeEffects.Remove(effect))
            {
                RemoveEffectModifiers(effect);
                OnStatusEffectRemoved?.Invoke(effect);
                
                // Stop visual effects
                StopVisualEffect(effect);
            }
        }
        
        public void RemoveStatusEffectsOfType(StatusEffectType type)
        {
            var effectsToRemove = GetStatusEffectsOfType(type);
            foreach (var effect in effectsToRemove)
            {
                RemoveStatusEffect(effect);
            }
        }
        
        public void RemoveAllStatusEffects()
        {
            var effectsToRemove = new List<StatusEffect>(activeEffects);
            foreach (var effect in effectsToRemove)
            {
                RemoveStatusEffect(effect);
            }
        }
        
        public List<StatusEffect> GetStatusEffectsOfType(StatusEffectType type)
        {
            var effects = new List<StatusEffect>();
            foreach (var effect in activeEffects)
            {
                if (effect.type == type && effect.isActive)
                {
                    effects.Add(effect);
                }
            }
            return effects;
        }
        
        public bool HasStatusEffect(StatusEffectType type)
        {
            return GetStatusEffectsOfType(type).Count > 0;
        }
        
        private void UpdateStatusEffects()
        {
            for (int i = activeEffects.Count - 1; i >= 0; i--)
            {
                var effect = activeEffects[i];
                effect.Update();
                
                // Process ticking effects
                if (effect.ShouldTick())
                {
                    ProcessStatusEffectTick(effect);
                    effect.Tick();
                }
                
                // Remove expired effects
                if (effect.IsExpired())
                {
                    RemoveStatusEffect(effect);
                }
            }
        }
        
        private void ProcessStatusEffectTick(StatusEffect effect)
        {
            switch (effect.type)
            {
                case StatusEffectType.Poison:
                    ApplyTickDamage(effect, DamageType.Poison);
                    break;
                    
                case StatusEffectType.Burn:
                    ApplyTickDamage(effect, DamageType.Fire);
                    break;
                    
                case StatusEffectType.Freeze:
                    // Freeze doesn't typically tick for damage
                    break;
                    
                case StatusEffectType.Stun:
                    // Stun doesn't tick for damage
                    break;
                    
                case StatusEffectType.Slow:
                    // Slow doesn't tick for damage
                    break;
                    
                case StatusEffectType.Vulnerable:
                    // Vulnerable doesn't tick for damage
                    break;
            }
            
            OnStatusEffectTicked?.Invoke(effect);
        }
        
        private void ApplyTickDamage(StatusEffect effect, DamageType damageType)
        {
            if (healthSystem != null && effect.tickDamage > 0)
            {
                healthSystem.TakeDamage(effect.tickDamage, damageType, effect.source);
            }
        }
        
        private void ApplyEffectModifiers(StatusEffect effect)
        {
            switch (effect.type)
            {
                case StatusEffectType.Vulnerable:
                    ApplyVulnerabilityModifier(effect);
                    break;
                    
                case StatusEffectType.Freeze:
                    ApplyFreezeModifier(effect);
                    break;
                    
                case StatusEffectType.Slow:
                    ApplySlowModifier(effect);
                    break;
                    
                case StatusEffectType.Stun:
                    ApplyStunModifier(effect);
                    break;
            }
        }
        
        private void RemoveEffectModifiers(StatusEffect effect)
        {
            switch (effect.type)
            {
                case StatusEffectType.Vulnerable:
                    RemoveVulnerabilityModifier(effect);
                    break;
                    
                case StatusEffectType.Freeze:
                    RemoveFreezeModifier(effect);
                    break;
                    
                case StatusEffectType.Slow:
                    RemoveSlowModifier(effect);
                    break;
                    
                case StatusEffectType.Stun:
                    RemoveStunModifier(effect);
                    break;
            }
        }
        
        private void ApplyVulnerabilityModifier(StatusEffect effect)
        {
            if (healthSystem != null)
            {
                var modifier = new HealthSystem.DamageModifier(
                    $"Vulnerable_{effect.GetHashCode()}", 
                    1f + effect.intensity, 
                    0f, 
                    effect.duration
                );
                healthSystem.AddDamageModifier(modifier);
            }
        }
        
        private void RemoveVulnerabilityModifier(StatusEffect effect)
        {
            // In a full implementation, you'd track and remove the specific modifier
        }
        
        private void ApplyFreezeModifier(StatusEffect effect)
        {
            // Apply movement speed reduction or complete freeze
            var movement = GetComponent<UnityEngine.CharacterController>();
            if (movement != null)
            {
                // Freeze logic would go here
            }
        }
        
        private void RemoveFreezeModifier(StatusEffect effect)
        {
            // Remove freeze effects
        }
        
        private void ApplySlowModifier(StatusEffect effect)
        {
            // Apply movement speed reduction
        }
        
        private void RemoveSlowModifier(StatusEffect effect)
        {
            // Remove slow effects
        }
        
        private void ApplyStunModifier(StatusEffect effect)
        {
            // Prevent actions during stun
        }
        
        private void RemoveStunModifier(StatusEffect effect)
        {
            // Remove stun prevention
        }
        
        private void StartVisualEffect(StatusEffect effect)
        {
            GameObject effectPrefab = GetVisualEffectPrefab(effect.type);
            if (effectPrefab != null)
            {
                var instance = Instantiate(effectPrefab, transform);
                // You might want to store the instance reference to control it later
            }
        }
        
        private void StopVisualEffect(StatusEffect effect)
        {
            // Stop or destroy the visual effect
        }
        
        private GameObject GetVisualEffectPrefab(StatusEffectType type)
        {
            switch (type)
            {
                case StatusEffectType.Poison: return poisonEffect;
                case StatusEffectType.Burn: return burnEffect;
                case StatusEffectType.Freeze: return freezeEffect;
                case StatusEffectType.Stun: return stunEffect;
                default: return null;
            }
        }
        
        // Debug methods
        public void DebugApplyPoison(float duration = 5f, int tickDamage = 2)
        {
            var poisonData = new StatusEffectData
            {
                effectType = StatusEffectType.Poison,
                duration = duration,
                tickDamage = tickDamage,
                tickRate = 1f,
                intensity = 1f
            };
            ApplyStatusEffect(poisonData);
        }
        
        public void DebugApplyBurn(float duration = 3f, int tickDamage = 3)
        {
            var burnData = new StatusEffectData
            {
                effectType = StatusEffectType.Burn,
                duration = duration,
                tickDamage = tickDamage,
                tickRate = 0.5f,
                intensity = 1f
            };
            ApplyStatusEffect(burnData);
        }
    }
}