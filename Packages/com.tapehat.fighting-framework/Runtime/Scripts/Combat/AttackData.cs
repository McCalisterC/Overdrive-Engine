using UnityEngine;
using System.Collections.Generic;

namespace FightingFramework.Combat
{
    public enum DamageType
    {
        Normal,
        Pierce, // Ignores armor
        Poison, // Damage over time
        Fire,   // Burning effect
        Ice,    // Freezing effect
        Electric, // Stunning effect
        Heal    // Negative damage
    }
    
    public enum AttackProperty
    {
        Overhead,    // Must be blocked high
        Low,         // Must be blocked low
        Unblockable, // Cannot be blocked
        ArmorBreaker, // Breaks through armor
        Launcher,    // Causes air juggle state
        WallBounce,  // Causes wall bounce
        GroundBounce, // Causes ground bounce
        CounterHit,  // Extra properties on counter hit
        Projectile   // Projectile properties
    }
    
    [System.Serializable]
    public struct DamageScaling
    {
        [Header("Base Values")]
        public int baseDamage;
        public float damageMultiplier;
        
        [Header("Scaling")]
        public float comboScaling; // Damage reduction per combo hit
        public float counterHitBonus; // Damage bonus on counter hit
        public float distanceScaling; // Damage based on distance
        
        [Header("Conditional")]
        public float lowHealthBonus; // Bonus when target is low health
        public float firstHitBonus; // Bonus for first hit of round
        
        public int CalculateDamage(int comboCount, bool isCounterHit, float distance, float targetHealthPercent)
        {
            float damage = baseDamage * damageMultiplier;
            
            // Apply combo scaling
            if (comboCount > 1)
            {
                float scaling = Mathf.Pow(1f - comboScaling, comboCount - 1);
                damage *= scaling;
            }
            
            // Apply counter hit bonus
            if (isCounterHit)
            {
                damage *= (1f + counterHitBonus);
            }
            
            // Apply distance scaling
            damage *= Mathf.Lerp(1f, distanceScaling, distance);
            
            // Apply low health bonus
            if (targetHealthPercent < 0.25f)
            {
                damage *= (1f + lowHealthBonus);
            }
            
            // Apply first hit bonus
            if (comboCount == 1)
            {
                damage *= (1f + firstHitBonus);
            }
            
            return Mathf.RoundToInt(damage);
        }
    }
    
    [CreateAssetMenu(fileName = "New Attack Data", menuName = "Fighting Framework/Combat/Attack Data")]
    public class AttackData : ScriptableObject
    {
        [Header("Basic Properties")]
        public string attackName;
        public int attackId;
        
        [Header("Damage")]
        public DamageScaling damageScaling;
        public int chipDamage = 1;
        public DamageType damageType = DamageType.Normal;
        
        [Header("Stun Values")]
        public int hitstun = 15;
        public int blockstun = 8;
        public int hitpause = 3; // Freeze frames on hit
        
        [Header("Knockback")]
        public Vector2 knockbackForce = Vector2.right * 5f;
        public AnimationCurve knockbackCurve = AnimationCurve.EaseInOut(0, 1, 1, 0);
        public float knockbackDuration = 0.5f;
        
        [Header("Attack Properties")]
        public List<AttackProperty> properties = new List<AttackProperty>();
        public int armorBreaker = 0; // Amount of armor this breaks
        public int priority = 1; // Higher priority wins in clashes
        
        [Header("Hit Effects")]
        public GameObject hitEffect;
        public GameObject blockEffect;
        public AudioClip hitSound;
        public AudioClip blockSound;
        public float screenShake = 0.1f;
        
        [Header("Status Effects")]
        public List<StatusEffectData> statusEffects = new List<StatusEffectData>();
        
        [Header("Meter Gain")]
        public int meterGainOnHit = 10;
        public int meterGainOnBlock = 5;
        public int meterGainOnWhiff = 1;
        
        // Properties for easy access
        public bool IsOverhead => properties.Contains(AttackProperty.Overhead);
        public bool IsLow => properties.Contains(AttackProperty.Low);
        public bool IsUnblockable => properties.Contains(AttackProperty.Unblockable);
        public bool IsArmorBreaker => properties.Contains(AttackProperty.ArmorBreaker) || armorBreaker > 0;
        public bool IsLauncher => properties.Contains(AttackProperty.Launcher);
        public bool CausesWallBounce => properties.Contains(AttackProperty.WallBounce);
        public bool CausesGroundBounce => properties.Contains(AttackProperty.GroundBounce);
        public bool IsProjectile => properties.Contains(AttackProperty.Projectile);
        
        public int CalculateDamage(int comboCount = 1, bool isCounterHit = false, float distance = 0f, float targetHealthPercent = 1f)
        {
            return damageScaling.CalculateDamage(comboCount, isCounterHit, distance, targetHealthPercent);
        }
        
        public int CalculateChipDamage(int comboCount = 1)
        {
            // Chip damage doesn't scale as much as regular damage
            float scaling = Mathf.Pow(0.9f, comboCount - 1);
            return Mathf.RoundToInt(chipDamage * scaling);
        }
        
        public bool CanBeBlocked(bool isBlockingHigh, bool isBlockingLow)
        {
            if (IsUnblockable) return false;
            
            if (IsOverhead && !isBlockingHigh) return false;
            if (IsLow && !isBlockingLow) return false;
            
            return true;
        }
        
        public void ApplyStatusEffects(GameObject target)
        {
            var statusSystem = target.GetComponent<StatusEffectSystem>();
            if (statusSystem != null)
            {
                foreach (var effect in statusEffects)
                {
                    statusSystem.ApplyStatusEffect(effect);
                }
            }
        }
        
        [System.Serializable]
        public struct StatusEffectData
        {
            public StatusEffectType effectType;
            public float duration;
            public float intensity;
            public int tickDamage;
            public float tickRate;
        }
        
        public enum StatusEffectType
        {
            Poison,
            Burn,
            Freeze,
            Stun,
            Slow,
            Vulnerable // Takes extra damage
        }
        
        private void OnValidate()
        {
            // Ensure reasonable values
            hitstun = Mathf.Clamp(hitstun, 1, 999);
            blockstun = Mathf.Clamp(blockstun, 1, 999);
            hitpause = Mathf.Clamp(hitpause, 0, 60);
            chipDamage = Mathf.Clamp(chipDamage, 0, damageScaling.baseDamage);
            
            // Auto-set attack ID based on name
            if (attackId == 0 && !string.IsNullOrEmpty(attackName))
            {
                attackId = attackName.GetHashCode();
            }
        }
    }
}