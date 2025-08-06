using UnityEngine;
using FightingFramework.States;
using FightingFramework.Animation;

namespace FightingFramework.Combat
{
    [RequireComponent(typeof(CharacterStateMachine))]
    [RequireComponent(typeof(HealthSystem))]
    public class CombatCharacter : MonoBehaviour
    {
        [Header("Combat Configuration")]
        [SerializeField] private AttackData[] availableAttacks;
        [SerializeField] private bool enableCounterAttacks = true;
        [SerializeField] private float counterWindow = 0.2f; // seconds
        
        [Header("Team Settings")]
        [SerializeField] private int teamId = 0;
        [SerializeField] private bool friendlyFire = false;
        
        [Header("Combat Stats")]
        [SerializeField] private float attackDamageMultiplier = 1f;
        [SerializeField] private float defenseMultiplier = 1f;
        [SerializeField] private int comboCounterThreshold = 5;
        
        // Components
        private CharacterStateMachine stateMachine;
        private HealthSystem healthSystem;
        private HitboxManager hitboxManager;
        private FrameBasedAnimator frameAnimator;
        private StatusEffectSystem statusEffects;
        private MeterSystem meterSystem;
        
        // Combat state
        private AttackData currentAttack;
        private GameObject lastAttacker;
        private float lastHitTime;
        private int consecutiveHits;
        
        // Events
        public System.Action<CombatResult> OnCombatResultReceived;
        public System.Action<AttackData> OnAttackStarted;
        public System.Action<AttackData> OnAttackEnded;
        public System.Action<GameObject> OnCounterOpportunity;
        
        // Properties
        public AttackData CurrentAttack => currentAttack;
        public bool IsInHitstun => stateMachine.CurrentState is HitstunState;
        public bool IsBlocking => stateMachine.CurrentState is BlockState;
        public bool IsAttacking => stateMachine.CurrentState is AttackState;
        public bool CanCounter => enableCounterAttacks && Time.time < lastHitTime + counterWindow;
        public int TeamId => teamId;
        
        private void Awake()
        {
            InitializeComponents();
            SubscribeToEvents();
        }
        
        private void OnDestroy()
        {
            UnsubscribeFromEvents();
        }
        
        private void InitializeComponents()
        {
            stateMachine = GetComponent<CharacterStateMachine>();
            healthSystem = GetComponent<HealthSystem>();
            hitboxManager = GetComponent<HitboxManager>();
            frameAnimator = GetComponent<FrameBasedAnimator>();
            statusEffects = GetComponent<StatusEffectSystem>();
            meterSystem = GetComponent<MeterSystem>();
            
            // Create missing components if needed
            if (hitboxManager == null)
                hitboxManager = gameObject.AddComponent<HitboxManager>();
            
            if (statusEffects == null)
                statusEffects = gameObject.AddComponent<StatusEffectSystem>();
            
            if (meterSystem == null)
                meterSystem = gameObject.AddComponent<MeterSystem>();
        }
        
        private void SubscribeToEvents()
        {
            if (healthSystem != null)
            {
                healthSystem.OnDamageTaken += HandleDamageTaken;
                healthSystem.OnDeath += HandleDeath;
            }
            
            if (hitboxManager != null)
            {
                hitboxManager.OnHitDetected += HandleHitDetected;
            }
            
            if (stateMachine != null)
            {
                stateMachine.OnStateChanged += HandleStateChanged;
            }
            
            // Subscribe to combat system events
            if (CombatSystem.Instance != null)
            {
                CombatSystem.Instance.OnCombatResult += HandleCombatResult;
            }
        }
        
        private void UnsubscribeFromEvents()
        {
            if (healthSystem != null)
            {
                healthSystem.OnDamageTaken -= HandleDamageTaken;
                healthSystem.OnDeath -= HandleDeath;
            }
            
            if (hitboxManager != null)
            {
                hitboxManager.OnHitDetected -= HandleHitDetected;
            }
            
            if (stateMachine != null)
            {
                stateMachine.OnStateChanged -= HandleStateChanged;
            }
            
            if (CombatSystem.Instance != null)
            {
                CombatSystem.Instance.OnCombatResult -= HandleCombatResult;
            }
        }
        
        public void ExecuteAttack(AttackData attack)
        {
            if (attack == null || !CanExecuteAttack(attack))
                return;
            
            currentAttack = attack;
            OnAttackStarted?.Invoke(attack);
            
            // Find and transition to appropriate attack state
            var attackStateName = GetAttackStateName(attack);
            var attackState = stateMachine.FindStateByName(attackStateName);
            
            if (attackState is AttackState combatAttackState)
            {
                // Configure attack state with attack data
                ConfigureAttackState(combatAttackState, attack);
                stateMachine.TryChangeState(combatAttackState);
            }
        }
        
        public void ExecuteBlock(bool highBlock = true)
        {
            if (!CanBlock())
                return;
            
            var blockState = stateMachine.FindStateByName("Block");
            if (blockState is BlockState blockStateInstance)
            {
                blockStateInstance.canBlockOverheads = highBlock;
                blockStateInstance.canBlockLows = !highBlock;
                stateMachine.TryChangeState(blockState);
            }
        }
        
        public void ExecuteCounter()
        {
            if (!CanCounter)
                return;
            
            var counterState = stateMachine.FindStateByName("Counter");
            if (counterState != null)
            {
                stateMachine.TryChangeState(counterState);
            }
        }
        
        public bool CanExecuteAttack(AttackData attack)
        {
            if (attack == null) return false;
            if (IsInHitstun) return false;
            if (healthSystem != null && !healthSystem.IsAlive) return false;
            
            // Check if we have enough meter for special attacks
            if (meterSystem != null && attack.meterGainOnHit < 0) // Negative meter gain means it costs meter
            {
                if (!meterSystem.ConsumeMeter(-attack.meterGainOnHit))
                    return false;
            }
            
            return true;
        }
        
        public bool CanBlock()
        {
            if (IsInHitstun) return false;
            if (IsAttacking && !((AttackState)stateMachine.CurrentState).canBeInterrupted) return false;
            if (healthSystem != null && !healthSystem.IsAlive) return false;
            
            return true;
        }
        
        private void HandleDamageTaken(int damage, DamageType damageType)
        {
            lastHitTime = Time.time;
            consecutiveHits++;
            
            // Trigger counter opportunity
            if (enableCounterAttacks)
            {
                OnCounterOpportunity?.Invoke(lastAttacker);
            }
            
            // Check for combo breaker opportunity
            if (consecutiveHits >= comboCounterThreshold && meterSystem?.CurrentMeter >= 50)
            {
                TriggerComboBreakerOpportunity();
            }
        }
        
        private void HandleDeath()
        {
            // Transition to death state
            var deathState = stateMachine.FindStateByName("Death");
            if (deathState != null)
            {
                stateMachine.ForceChangeState(deathState);
            }
        }
        
        private void HandleHitDetected(Animation.HitResult hitResult)
        {
            if (hitResult.attacker != gameObject) return;
            
            // Process our attack hitting something
            var target = hitResult.victim;
            var combatCharacter = target.GetComponent<CombatCharacter>();
            
            if (combatCharacter != null && CanHitTarget(combatCharacter))
            {
                // Use combat system to process the attack
                var result = CombatSystem.Instance.ProcessAttack(currentAttack, gameObject, target);
                
                // Handle post-hit logic
                HandleAttackConnected(result);
            }
        }
        
        private void HandleStateChanged(CharacterState previousState, CharacterState newState)
        {
            // Reset attack when leaving attack state
            if (previousState is AttackState && currentAttack != null)
            {
                OnAttackEnded?.Invoke(currentAttack);
                currentAttack = null;
            }
            
            // Reset consecutive hits when not in hitstun
            if (!(newState is HitstunState))
            {
                consecutiveHits = 0;
            }
        }
        
        private void HandleCombatResult(CombatResult result)
        {
            // Handle combat results that involve this character
            if (result.defender == gameObject)
            {
                lastAttacker = result.attacker;
                OnCombatResultReceived?.Invoke(result);
            }
        }
        
        private void HandleAttackConnected(CombatResult result)
        {
            // Grant meter for successful hit
            if (result.result == Combat.HitResult.Hit && meterSystem != null)
            {
                meterSystem.AddMeter(result.attackData.meterGainOnHit);
            }
            
            // Check for combo opportunities
            if (result.result == Combat.HitResult.Hit)
            {
                CheckComboOpportunities(result);
            }
        }
        
        private bool CanHitTarget(CombatCharacter target)
        {
            if (target == null) return false;
            if (target == this) return false;
            if (!friendlyFire && target.TeamId == TeamId) return false;
            
            return true;
        }
        
        private void ConfigureAttackState(AttackState attackState, AttackData attackData)
        {
            // Sync attack state properties with attack data
            attackState.damage = attackData.CalculateDamage();
            attackState.knockback = attackData.knockbackForce.magnitude;
            attackState.knockbackDirection = attackData.knockbackForce.normalized;
            attackState.attackType = GetStateAttackType(attackData);
            
            // Apply damage multiplier
            attackState.damage = Mathf.RoundToInt(attackState.damage * attackDamageMultiplier);
        }
        
        private States.AttackType GetStateAttackType(AttackData attackData)
        {
            if (attackData.IsOverhead) return States.AttackType.Overhead;
            if (attackData.IsLow) return States.AttackType.Low;
            if (attackData.IsUnblockable) return States.AttackType.Unblockable;
            return States.AttackType.Mid;
        }
        
        private string GetAttackStateName(AttackData attack)
        {
            // Map attack data to state names
            if (attack.attackName.Contains("Light"))
                return "Light Attack";
            if (attack.attackName.Contains("Heavy"))
                return "Heavy Attack";
            if (attack.attackName.Contains("Special"))
                return "Special Attack";
            
            return "Attack";
        }
        
        private void CheckComboOpportunities(CombatResult result)
        {
            // Check if this attack can combo into another
            if (result.attackData.IsLauncher)
            {
                // Air combo opportunity
                Debug.Log("Air combo opportunity!");
            }
        }
        
        private void TriggerComboBreakerOpportunity()
        {
            Debug.Log($"{gameObject.name} has combo breaker opportunity!");
            // Visual indicator or UI notification could go here
        }
        
        // Public utility methods
        public AttackData GetAttackByName(string attackName)
        {
            foreach (var attack in availableAttacks)
            {
                if (attack.attackName == attackName)
                    return attack;
            }
            return null;
        }
        
        public AttackData[] GetAvailableAttacks()
        {
            return availableAttacks;
        }
        
        public void SetTeam(int newTeamId)
        {
            teamId = newTeamId;
        }
        
        public void ModifyDamageMultiplier(float multiplier)
        {
            attackDamageMultiplier = multiplier;
        }
        
        public void ModifyDefenseMultiplier(float multiplier)
        {
            defenseMultiplier = multiplier;
        }
        
        // Debug methods
        [ContextMenu("Debug: Execute Light Attack")]
        public void DebugExecuteLightAttack()
        {
            var lightAttack = GetAttackByName("Light Punch");
            if (lightAttack != null)
                ExecuteAttack(lightAttack);
        }
        
        [ContextMenu("Debug: Execute Block")]
        public void DebugExecuteBlock()
        {
            ExecuteBlock(true);
        }
        
        [ContextMenu("Debug: Apply Test Damage")]
        public void DebugApplyTestDamage()
        {
            if (healthSystem != null)
                healthSystem.TakeDamage(10, DamageType.Normal);
        }
    }
}