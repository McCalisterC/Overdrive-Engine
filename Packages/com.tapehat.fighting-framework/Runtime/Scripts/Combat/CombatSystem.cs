using UnityEngine;
using System.Collections.Generic;
using FightingFramework.Events;
using FightingFramework.States;
using FightingFramework.Animation;

namespace FightingFramework.Combat
{
    public enum HitResult
    {
        Miss,
        Hit,
        Block,
        Counter,
        Trade,
        Clash
    }
    
    [System.Serializable]
    public struct CombatResult
    {
        public HitResult result;
        public GameObject attacker;
        public GameObject defender;
        public AttackData attackData;
        public int damageDealt;
        public bool wasCounterHit;
        public bool wasCounterAttack;
        public Vector2 hitPosition;
        public float distance;
        
        public CombatResult(HitResult result, GameObject attacker, GameObject defender, AttackData attackData)
        {
            this.result = result;
            this.attacker = attacker;
            this.defender = defender;
            this.attackData = attackData;
            this.damageDealt = 0;
            this.wasCounterHit = false;
            this.wasCounterAttack = false;
            this.hitPosition = Vector2.zero;
            this.distance = 0f;
        }
    }
    
    public class CombatSystem : MonoBehaviour
    {
        [Header("Combat Events")]
        [SerializeField] private GameEvent onHitEvent;
        [SerializeField] private GameEvent onBlockEvent;
        [SerializeField] private GameEvent onCounterEvent;
        [SerializeField] private GameEvent onClashEvent;
        
        [Header("Combat Settings")]
        [SerializeField] private bool enableCounterHits = true;
        [SerializeField] private bool enableClashing = true;
        [SerializeField] private float clashWindow = 3f; // frames
        [SerializeField] private LayerMask combatLayers = -1;
        
        [Header("Debug")]
        [SerializeField] private bool debugMode = false;
        
        // Singleton instance
        private static CombatSystem instance;
        public static CombatSystem Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = FindFirstObjectByType<CombatSystem>();
                    if (instance == null)
                    {
                        var go = new GameObject("Combat System");
                        instance = go.AddComponent<CombatSystem>();
                    }
                }
                return instance;
            }
        }
        
        // Combat tracking
        private Dictionary<GameObject, ComboTracker> comboTrackers = new Dictionary<GameObject, ComboTracker>();
        private List<PendingAttack> pendingAttacks = new List<PendingAttack>();
        
        // Events
        public System.Action<CombatResult> OnCombatResult;
        public System.Action<GameObject, GameObject, AttackData> OnHit;
        public System.Action<GameObject, GameObject, AttackData> OnBlock;
        public System.Action<GameObject, GameObject, AttackData> OnCounter;
        public System.Action<GameObject, GameObject> OnClash;
        
        private void Awake()
        {
            if (instance == null)
            {
                instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else if (instance != this)
            {
                Destroy(gameObject);
            }
        }
        
        private void Update()
        {
            ProcessPendingAttacks();
            UpdateComboTrackers();
        }
        
        public CombatResult ProcessAttack(AttackData attack, GameObject attacker, GameObject defender)
        {
            if (attack == null || attacker == null || defender == null)
                return new CombatResult(HitResult.Miss, attacker, defender, attack);
            
            var result = new CombatResult(HitResult.Miss, attacker, defender, attack);
            
            // Calculate distance
            result.distance = Vector2.Distance(attacker.transform.position, defender.transform.position);
            result.hitPosition = (attacker.transform.position + defender.transform.position) * 0.5f;
            
            // Check for valid hit
            if (!CanHit(attacker, defender))
            {
                return result;
            }
            
            // Check for clash first
            if (enableClashing && CheckForClash(attacker, defender))
            {
                result.result = HitResult.Clash;
                ProcessClash(attacker, defender);
                return result;
            }
            
            // Check if defender is blocking
            bool isBlocking = IsBlocking(defender, attack);
            bool canBlock = attack.CanBeBlocked(IsBlockingHigh(defender), IsBlockingLow(defender));
            
            if (isBlocking && canBlock)
            {
                result.result = HitResult.Block;
                result = ProcessBlock(result);
            }
            else
            {
                // Check for counter hit
                if (enableCounterHits && IsInAttackStartup(defender))
                {
                    result.wasCounterHit = true;
                }
                
                result.result = HitResult.Hit;
                result = ProcessHit(result);
            }
            
            // Fire appropriate events
            FireCombatEvents(result);
            
            // Update combo tracking
            UpdateComboTracking(result);
            
            if (debugMode)
            {
                Debug.Log($"Combat Result: {result.result} - {attacker.name} -> {defender.name} ({result.damageDealt} damage)");
            }
            
            return result;
        }
        
        private CombatResult ProcessHit(CombatResult result)
        {
            var defenderHealth = result.defender.GetComponent<HealthSystem>();
            if (defenderHealth == null) return result;
            
            // Get combo count
            int comboCount = GetComboCount(result.attacker);
            
            // Calculate damage
            result.damageDealt = result.attackData.CalculateDamage(
                comboCount, 
                result.wasCounterHit, 
                result.distance, 
                defenderHealth.HealthPercentage
            );
            
            // Apply damage
            defenderHealth.TakeDamage(result.damageDealt, result.attackData.damageType, result.attacker);
            
            // Apply status effects
            result.attackData.ApplyStatusEffects(result.defender);
            
            // Apply hitstun
            ApplyHitstun(result.defender, result.attackData, result.wasCounterHit);
            
            // Apply knockback
            ApplyKnockback(result.defender, result.attackData, result.attacker);
            
            // Apply visual/audio effects
            ApplyHitEffects(result);
            
            // Grant meter
            GrantMeter(result.attacker, result.attackData.meterGainOnHit);
            
            return result;
        }
        
        private CombatResult ProcessBlock(CombatResult result)
        {
            var defenderHealth = result.defender.GetComponent<HealthSystem>();
            if (defenderHealth == null) return result;
            
            // Calculate chip damage
            int chipDamage = result.attackData.CalculateChipDamage(GetComboCount(result.attacker));
            result.damageDealt = chipDamage;
            
            // Apply chip damage
            if (chipDamage > 0)
            {
                defenderHealth.TakeDamage(chipDamage, result.attackData.damageType, result.attacker);
            }
            
            // Apply blockstun
            ApplyBlockstun(result.defender, result.attackData);
            
            // Apply pushback
            ApplyBlockPushback(result.defender, result.attackData, result.attacker);
            
            // Apply visual/audio effects
            ApplyBlockEffects(result);
            
            // Grant meter
            GrantMeter(result.attacker, result.attackData.meterGainOnBlock);
            GrantMeter(result.defender, result.attackData.meterGainOnBlock / 2); // Defender gets less
            
            return result;
        }
        
        private bool CanHit(GameObject attacker, GameObject defender)
        {
            // Check if both objects are on combat layers
            if (!IsOnCombatLayer(attacker) || !IsOnCombatLayer(defender))
                return false;
            
            // Check if defender is invincible
            var defenderHealth = defender.GetComponent<HealthSystem>();
            if (defenderHealth != null && defenderHealth.IsInvincible)
                return false;
            
            // Check if attacker and defender are the same
            if (attacker == defender)
                return false;
            
            // Additional team/faction checks could go here
            
            return true;
        }
        
        private bool CheckForClash(GameObject attacker, GameObject defender)
        {
            // Simple clash detection - both characters attacking at the same time
            var attackerStateMachine = attacker.GetComponent<CharacterStateMachine>();
            var defenderStateMachine = defender.GetComponent<CharacterStateMachine>();
            
            if (attackerStateMachine?.CurrentState is AttackState && 
                defenderStateMachine?.CurrentState is AttackState)
            {
                // Check if both are in active frames within clash window
                var attackerState = attackerStateMachine.CurrentState;
                var defenderState = defenderStateMachine.CurrentState;
                
                bool attackerActive = attackerState.IsActive(attackerStateMachine.CurrentFrame);
                bool defenderActive = defenderState.IsActive(defenderStateMachine.CurrentFrame);
                
                return attackerActive && defenderActive;
            }
            
            return false;
        }
        
        private void ProcessClash(GameObject attacker, GameObject defender)
        {
            // Handle clash logic - both attacks cancel out
            var attackerStateMachine = attacker.GetComponent<CharacterStateMachine>();
            var defenderStateMachine = defender.GetComponent<CharacterStateMachine>();
            
            // Apply clash effects (pushback, etc.)
            ApplyClashEffects(attacker, defender);
            
            OnClash?.Invoke(attacker, defender);
            onClashEvent?.Raise();
        }
        
        private bool IsBlocking(GameObject character, AttackData attack)
        {
            var stateMachine = character.GetComponent<CharacterStateMachine>();
            return stateMachine?.CurrentState is BlockState;
        }
        
        private bool IsBlockingHigh(GameObject character)
        {
            // Check if character is in high block state
            return true; // Simplified for now
        }
        
        private bool IsBlockingLow(GameObject character)
        {
            // Check if character is in low block state
            return false; // Simplified for now
        }
        
        private bool IsInAttackStartup(GameObject character)
        {
            var stateMachine = character.GetComponent<CharacterStateMachine>();
            if (stateMachine?.CurrentState is AttackState attackState)
            {
                return attackState.IsInStartup(stateMachine.CurrentFrame);
            }
            return false;
        }
        
        private void ApplyHitstun(GameObject target, AttackData attack, bool isCounterHit)
        {
            var stateMachine = target.GetComponent<CharacterStateMachine>();
            if (stateMachine == null) return;
            
            var hitstunState = stateMachine.FindStateByName("Hitstun");
            if (hitstunState is HitstunState hitstun)
            {
                // Initialize hitstun with attack data
                var hitInfo = CreateHitInfo(attack, isCounterHit);
                hitstun.Initialize(hitInfo);
                stateMachine.ForceChangeState(hitstun);
            }
        }
        
        private void ApplyBlockstun(GameObject target, AttackData attack)
        {
            var stateMachine = target.GetComponent<CharacterStateMachine>();
            if (stateMachine == null) return;
            
            var blockState = stateMachine.FindStateByName("Block");
            if (blockState is BlockState block)
            {
                var hitInfo = CreateHitInfo(attack, false);
                block.Initialize(hitInfo);
                // Block state should already be active if blocking
            }
        }
        
        private void ApplyKnockback(GameObject target, AttackData attack, GameObject attacker)
        {
            var rigidbody = target.GetComponent<Rigidbody2D>();
            if (rigidbody != null)
            {
                Vector2 direction = (target.transform.position - attacker.transform.position).normalized;
                Vector2 knockback = Vector2.Scale(attack.knockbackForce, direction);
                
                rigidbody.AddForce(knockback, ForceMode2D.Impulse);
            }
        }
        
        private void ApplyBlockPushback(GameObject target, AttackData attack, GameObject attacker)
        {
            var rigidbody = target.GetComponent<Rigidbody2D>();
            if (rigidbody != null)
            {
                Vector2 direction = (target.transform.position - attacker.transform.position).normalized;
                Vector2 pushback = Vector2.Scale(attack.knockbackForce * 0.3f, direction); // Reduced pushback for blocks
                
                rigidbody.AddForce(pushback, ForceMode2D.Impulse);
            }
        }
        
        private void ApplyHitEffects(CombatResult result)
        {
            // Spawn hit effects
            if (result.attackData.hitEffect != null)
            {
                Instantiate(result.attackData.hitEffect, result.hitPosition, Quaternion.identity);
            }
            
            // Play hit sound
            if (result.attackData.hitSound != null)
            {
                AudioSource.PlayClipAtPoint(result.attackData.hitSound, result.hitPosition);
            }
            
            // Apply screen shake
            if (result.attackData.screenShake > 0)
            {
                ApplyScreenShake(result.attackData.screenShake);
            }
        }
        
        private void ApplyBlockEffects(CombatResult result)
        {
            // Spawn block effects
            if (result.attackData.blockEffect != null)
            {
                Instantiate(result.attackData.blockEffect, result.hitPosition, Quaternion.identity);
            }
            
            // Play block sound
            if (result.attackData.blockSound != null)
            {
                AudioSource.PlayClipAtPoint(result.attackData.blockSound, result.hitPosition);
            }
        }
        
        private void ApplyClashEffects(GameObject attacker, GameObject defender)
        {
            // Apply pushback to both characters
            var attackerRb = attacker.GetComponent<Rigidbody2D>();
            var defenderRb = defender.GetComponent<Rigidbody2D>();
            
            if (attackerRb != null && defenderRb != null)
            {
                Vector2 direction = (defender.transform.position - attacker.transform.position).normalized;
                Vector2 pushback = direction * 5f;
                
                attackerRb.AddForce(-pushback, ForceMode2D.Impulse);
                defenderRb.AddForce(pushback, ForceMode2D.Impulse);
            }
        }
        
        private void ApplyScreenShake(float intensity)
        {
            // Screen shake implementation would go here
            Debug.Log($"Screen shake: {intensity}");
        }
        
        private void GrantMeter(GameObject character, int amount)
        {
            var meterSystem = character.GetComponent<MeterSystem>();
            if (meterSystem != null)
            {
                meterSystem.AddMeter(amount);
            }
        }
        
        private HitInfo CreateHitInfo(AttackData attack, bool isCounterHit)
        {
            return new HitInfo
            {
                damage = attack.CalculateDamage(1, isCounterHit),
                knockback = attack.knockbackForce.magnitude,
                knockbackDirection = attack.knockbackForce.normalized,
                hitstun = attack.hitstun,
                blockstun = attack.blockstun,
                hitpause = attack.hitpause,
                attackType = attack.IsOverhead ? States.AttackType.Overhead : 
                            attack.IsLow ? States.AttackType.Low : States.AttackType.Mid,
                canBeBlocked = !attack.IsUnblockable
            };
        }
        
        private void FireCombatEvents(CombatResult result)
        {
            OnCombatResult?.Invoke(result);
            
            switch (result.result)
            {
                case HitResult.Hit:
                    OnHit?.Invoke(result.attacker, result.defender, result.attackData);
                    onHitEvent?.Raise();
                    break;
                case HitResult.Block:
                    OnBlock?.Invoke(result.attacker, result.defender, result.attackData);
                    onBlockEvent?.Raise();
                    break;
                case HitResult.Counter:
                    OnCounter?.Invoke(result.attacker, result.defender, result.attackData);
                    onCounterEvent?.Raise();
                    break;
                case HitResult.Clash:
                    onClashEvent?.Raise();
                    break;
            }
        }
        
        private int GetComboCount(GameObject attacker)
        {
            if (comboTrackers.TryGetValue(attacker, out var tracker))
            {
                return tracker.ComboCount;
            }
            return 1;
        }
        
        private void UpdateComboTracking(CombatResult result)
        {
            if (result.result == HitResult.Hit)
            {
                if (!comboTrackers.TryGetValue(result.attacker, out var tracker))
                {
                    tracker = new ComboTracker();
                    comboTrackers[result.attacker] = tracker;
                }
                
                tracker.AddHit(result.attackData);
            }
            else if (result.result == HitResult.Miss)
            {
                // Reset combo on miss
                if (comboTrackers.ContainsKey(result.attacker))
                {
                    comboTrackers.Remove(result.attacker);
                }
            }
        }
        
        private void UpdateComboTrackers()
        {
            var expiredTrackers = new List<GameObject>();
            
            foreach (var kvp in comboTrackers)
            {
                kvp.Value.Update();
                if (kvp.Value.IsExpired())
                {
                    expiredTrackers.Add(kvp.Key);
                }
            }
            
            foreach (var expired in expiredTrackers)
            {
                comboTrackers.Remove(expired);
            }
        }
        
        private void ProcessPendingAttacks()
        {
            for (int i = pendingAttacks.Count - 1; i >= 0; i--)
            {
                var pending = pendingAttacks[i];
                pending.remainingTime -= Time.deltaTime;
                
                if (pending.remainingTime <= 0)
                {
                    ProcessAttack(pending.attackData, pending.attacker, pending.defender);
                    pendingAttacks.RemoveAt(i);
                }
            }
        }
        
        private bool IsOnCombatLayer(GameObject obj)
        {
            return (combatLayers.value & (1 << obj.layer)) != 0;
        }
        
        // Utility classes
        [System.Serializable]
        public class ComboTracker
        {
            private int comboCount = 0;
            private float lastHitTime;
            private const float comboTimeWindow = 2f;
            
            public int ComboCount => comboCount;
            public bool IsExpired() => Time.time > lastHitTime + comboTimeWindow;
            
            public void AddHit(AttackData attack)
            {
                comboCount++;
                lastHitTime = Time.time;
            }
            
            public void Update()
            {
                // Combo tracker update logic
            }
            
            public void Reset()
            {
                comboCount = 0;
            }
        }
        
        [System.Serializable]
        public struct PendingAttack
        {
            public AttackData attackData;
            public GameObject attacker;
            public GameObject defender;
            public float remainingTime;
            
            public PendingAttack(AttackData attack, GameObject attacker, GameObject defender, float delay)
            {
                this.attackData = attack;
                this.attacker = attacker;
                this.defender = defender;
                this.remainingTime = delay;
            }
        }
        
        // Public API for delayed attacks
        public void ScheduleAttack(AttackData attack, GameObject attacker, GameObject defender, float delay)
        {
            pendingAttacks.Add(new PendingAttack(attack, attacker, defender, delay));
        }
    }
    
    // Simple meter system placeholder
    public class MeterSystem : MonoBehaviour
    {
        [SerializeField] private int maxMeter = 100;
        private int currentMeter = 0;
        
        public int CurrentMeter => currentMeter;
        public float MeterPercentage => (float)currentMeter / maxMeter;
        
        public void AddMeter(int amount)
        {
            currentMeter = Mathf.Clamp(currentMeter + amount, 0, maxMeter);
        }
        
        public bool ConsumeMeter(int amount)
        {
            if (currentMeter >= amount)
            {
                currentMeter -= amount;
                return true;
            }
            return false;
        }
    }
}