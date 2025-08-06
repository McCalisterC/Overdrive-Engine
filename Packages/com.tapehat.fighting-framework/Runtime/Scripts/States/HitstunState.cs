using UnityEngine;
using FightingFramework.Combat;

namespace FightingFramework.States
{
    [CreateAssetMenu(fileName = "New Hitstun State", menuName = "Fighting Framework/States/Hitstun")]
    public class HitstunState : CharacterState
    {
        [Header("Hitstun Properties")]
        public int hitstunDuration = 15; // frames
        public bool allowCounterAttack = false;
        public int counterAttackWindow = 5; // frames
        
        [Header("Knockback")]
        public bool applyKnockback = true;
        public float knockbackDecay = 0.9f;
        
        [Header("Recovery")]
        public CharacterState recoveryState; // State to transition to after hitstun
        
        private Vector2 knockbackVelocity;
        private int hitstunFramesRemaining;
        private HitInfo lastHitInfo;
        
        public void Initialize(HitInfo hitInfo)
        {
            lastHitInfo = hitInfo;
            hitstunFramesRemaining = hitInfo.hitstun;
            
            if (applyKnockback)
            {
                knockbackVelocity = hitInfo.knockbackDirection * hitInfo.knockback;
            }
        }
        
        public override void Enter(CharacterController character)
        {
            if (character == null) return;
            
            var animator = character.GetComponent<Animator>();
            if (animator != null && animation != null)
            {
                animator.Play(animation.name);
            }
            
            // Set hitstun duration if not already set
            if (hitstunFramesRemaining <= 0)
            {
                hitstunFramesRemaining = hitstunDuration;
            }
            
            Debug.Log($"Entered hitstun state: {stateName} for {hitstunFramesRemaining} frames");
        }
        
        public override void UpdateState(CharacterController character)
        {
            if (character == null) return;
            
            var stateMachine = character.GetComponent<CharacterStateMachine>();
            if (stateMachine == null) return;
            
            // Apply knockback
            if (applyKnockback && knockbackVelocity.magnitude > 0.1f)
            {
                character.Move(knockbackVelocity * Time.deltaTime);
                knockbackVelocity *= knockbackDecay;
            }
            
            // Apply gravity
            if (!character.isGrounded)
            {
                knockbackVelocity.y -= 9.81f * Time.deltaTime;
            }
            
            // Reduce hitstun frames
            hitstunFramesRemaining--;
            
            // Check for counter attack input
            if (allowCounterAttack && hitstunFramesRemaining <= counterAttackWindow)
            {
                if (UnityEngine.Input.GetButtonDown("Attack"))
                {
                    var counterState = stateMachine.FindStateByName("Counter");
                    if (counterState != null && stateMachine.TryChangeState(counterState))
                    {
                        return;
                    }
                }
            }
            
            // Transition out when hitstun ends
            if (hitstunFramesRemaining <= 0)
            {
                TransitionToRecovery(stateMachine);
            }
        }
        
        public override void Exit(CharacterController character)
        {
            if (character == null) return;
            
            knockbackVelocity = Vector2.zero;
            hitstunFramesRemaining = 0;
            
            Debug.Log($"Exited hitstun state: {stateName}");
        }
        
        public override bool CanTransitionTo(CharacterState newState)
        {
            // During hitstun, only allow specific transitions
            if (newState == null) return false;
            
            // Allow transition to recovery state
            if (newState == recoveryState) return true;
            
            // Allow counter attacks during counter window
            if (allowCounterAttack && hitstunFramesRemaining <= counterAttackWindow)
            {
                if (newState.stateName.Contains("Counter") || newState.priority >= this.priority)
                {
                    return true;
                }
            }
            
            // Don't allow other transitions during hitstun
            return false;
        }
        
        private void TransitionToRecovery(CharacterStateMachine stateMachine)
        {
            if (recoveryState != null)
            {
                stateMachine.ChangeState(recoveryState);
            }
            else
            {
                // Default to idle if no recovery state is set
                var idleState = stateMachine.FindStateByName("Idle");
                if (idleState != null)
                {
                    stateMachine.ChangeState(idleState);
                }
            }
        }
        
        public int GetRemainingFrames()
        {
            return hitstunFramesRemaining;
        }
        
        public bool IsInCounterWindow()
        {
            return allowCounterAttack && hitstunFramesRemaining <= counterAttackWindow;
        }
    }
}