using System.Collections.Generic;
using UnityEngine;
using FightingFramework.Animation;

namespace FightingFramework.States
{
    [CreateAssetMenu(fileName = "New Attack State", menuName = "Fighting Framework/States/Attack")]
    public class AttackState : AnimatedCharacterState
    {
        [Header("Combat Data")]
        public int damage = 10;
        public float knockback = 5f;
        public Vector2 knockbackDirection = Vector2.right;
        public List<HitboxData> hitboxes = new List<HitboxData>();
        
        [Header("Attack Properties")]
        public AttackType attackType = AttackType.Mid;
        public bool autoComboEnabled = false;
        public CharacterState comboFollowupState;
        
        [Header("Movement")]
        public Vector2 movementVector = Vector2.zero;
        public AnimationCurve movementCurve = AnimationCurve.Linear(0, 0, 1, 1);
        
        [Header("Audio & Effects")]
        public AudioClip attackSound;
        public GameObject attackEffect;
        
        private List<GameObject> hitTargets = new List<GameObject>();
        private bool hasPlayedSound = false;
        
        public override void Enter(CharacterController character)
        {
            if (character == null) return;
            
            // Initialize frame-based animation if this is an animated state
            
            hitTargets.Clear();
            hasPlayedSound = false;
            
            if (attackEffect != null)
            {
                var effect = Instantiate(attackEffect, character.transform.position, character.transform.rotation);
                var autoDestroy = effect.GetComponent<ParticleSystem>();
                if (autoDestroy != null && autoDestroy.main.stopAction == ParticleSystemStopAction.Destroy)
                {
                    Destroy(effect, autoDestroy.main.duration);
                }
            }
            
            Debug.Log($"Entered attack state: {stateName}");
        }
        
        public override void UpdateState(CharacterController character)
        {
            if (character == null) return;
            
            // Handle frame-based animation sync if this is an animated state
            
            var stateMachine = character.GetComponent<CharacterStateMachine>();
            if (stateMachine == null) return;
            
            int currentFrame = stateMachine.CurrentFrame;
            
            // Only apply legacy movement if not using frame-based animation
            if (!useFrameBasedAnimation)
            {
                ApplyMovement(character, currentFrame);
                UpdateHitboxes(character, currentFrame);
            }
            
            if (!hasPlayedSound && IsActive(currentFrame) && attackSound != null)
            {
                var audioSource = character.GetComponent<AudioSource>();
                if (audioSource != null)
                {
                    audioSource.PlayOneShot(attackSound);
                }
                hasPlayedSound = true;
            }
        }
        
        public override void Exit(CharacterController character)
        {
            if (character == null) return;
            
            // Cleanup frame-based animation if this is an animated state
            
            hitTargets.Clear();
            hasPlayedSound = false;
            
            Debug.Log($"Exited attack state: {stateName}");
        }
        
        // Frame-based animation integration methods (only used if AttackState inherits from AnimatedCharacterState)
        protected virtual void OnFrameChanged(FrameData frameData)
        {
            // Process frame-based hitboxes if using frame animation
            if (frameData.hitboxes != null && frameData.hitboxes.Count > 0)
            {
                ProcessFrameBasedHitboxes(frameData);
            }
        }
        
        protected virtual void OnFrameEvent(FrameEvent frameEvent)
        {
            // Handle attack-specific frame events
            if (frameEvent.eventName == "AttackHit")
            {
                // Custom attack hit event
                Debug.Log("Attack hit frame event triggered!");
            }
        }
        
        protected virtual void ProcessFrameBasedHitboxes(FrameData frameData)
        {
            // Convert frame animation rectangles to HitboxData format
            foreach (var rect in frameData.hitboxes)
            {
                // Process hit detection using the frame-based hitbox system
                // This integrates the old HitboxData system with the new Rectangle system
                var hitboxData = new HitboxData
                {
                    startFrame = frameData.frameNumber,
                    endFrame = frameData.frameNumber,
                    damage = rect.damage > 0 ? rect.damage : damage,
                    knockback = rect.knockback > 0 ? rect.knockback : knockback,
                    knockbackDirection = rect.knockbackDirection != Vector2.zero ? rect.knockbackDirection : knockbackDirection,
                    offset = rect.center,
                    size = rect.size,
                    attackType = attackType
                };
                
                // You could process this hitbox data or trigger hit events
                Debug.Log($"Processing frame-based hitbox at {rect.center} with size {rect.size}");
            }
        }
        
        public override bool CanTransitionTo(CharacterState newState)
        {
            if (!canBeInterrupted)
            {
                var stateMachine = FindFirstObjectByType<CharacterStateMachine>();
                if (stateMachine != null && !IsInRecovery(stateMachine.CurrentFrame))
                {
                    return false;
                }
            }
            
            if (newState == null) return false;
            
            if (newState.priority < priority && !canBeInterrupted)
            {
                return false;
            }
            
            return true;
        }
        
        private void ApplyMovement(CharacterController character, int currentFrame)
        {
            if (movementVector == Vector2.zero || GetTotalFrames() <= 0) return;
            
            float normalizedTime = (float)currentFrame / GetTotalFrames();
            float curveValue = movementCurve.Evaluate(normalizedTime);
            
            Vector2 frameMovement = movementVector * curveValue * Time.deltaTime;
            
            if (character.transform.localScale.x < 0)
            {
                frameMovement.x = -frameMovement.x;
            }
            
            character.Move(frameMovement);
        }
        
        private void UpdateHitboxes(CharacterController character, int currentFrame)
        {
            foreach (var hitbox in hitboxes)
            {
                if (hitbox.IsActiveOnFrame(currentFrame))
                {
                    CheckHitboxCollision(character, hitbox);
                }
            }
        }
        
        private void CheckHitboxCollision(CharacterController character, HitboxData hitbox)
        {
            bool facingRight = character.transform.localScale.x > 0;
            Rect hitboxRect = hitbox.GetHitboxRect(character.transform.position, facingRight);
            
            Collider2D[] colliders = Physics2D.OverlapAreaAll(
                new Vector2(hitboxRect.xMin, hitboxRect.yMin),
                new Vector2(hitboxRect.xMax, hitboxRect.yMax),
                hitbox.targetLayers
            );
            
            foreach (var collider in colliders)
            {
                if (collider.gameObject == character.gameObject) continue;
                if (hitTargets.Contains(collider.gameObject)) continue;
                
                ProcessHit(character, collider.gameObject, hitbox);
                hitTargets.Add(collider.gameObject);
            }
        }
        
        private void ProcessHit(CharacterController attacker, GameObject target, HitboxData hitbox)
        {
            var targetController = target.GetComponent<CharacterController>();
            if (targetController == null) return;
            
            bool facingRight = attacker.transform.localScale.x > 0;
            Vector2 knockbackDir = hitbox.GetKnockbackDirection(facingRight);
            
            var hitInfo = new HitInfo
            {
                attacker = attacker.gameObject,
                damage = hitbox.damage,
                knockback = hitbox.knockback,
                knockbackDirection = knockbackDir,
                hitstun = hitbox.hitstun,
                blockstun = hitbox.blockstun,
                hitpause = hitbox.hitpause,
                attackType = hitbox.attackType,
                canBeBlocked = hitbox.canBeBlocked
            };
            
            var targetStateMachine = target.GetComponent<CharacterStateMachine>();
            if (targetStateMachine != null)
            {
                // Apply hitstun or blockstun state
                // This would typically trigger a hit or block state on the target
                Debug.Log($"Hit detected: {attacker.name} hit {target.name} with {hitbox.hitboxName}");
            }
            
            // Spawn hit effects
            if (hitbox.hitEffect != null)
            {
                var effect = Instantiate(hitbox.hitEffect, target.transform.position, Quaternion.identity);
                Destroy(effect, 2f);
            }
            
            // Play hit sound
            if (hitbox.hitSound != null)
            {
                var audioSource = attacker.GetComponent<AudioSource>();
                if (audioSource != null)
                {
                    audioSource.PlayOneShot(hitbox.hitSound);
                }
            }
        }
        
        // Note: OnDrawGizmosSelected removed as ScriptableObjects don't have transform
        // Gizmo drawing is handled by the CharacterStateMachine or FrameBasedAnimator components
    }
    
    [System.Serializable]
    public struct HitInfo
    {
        public GameObject attacker;
        public int damage;
        public float knockback;
        public Vector2 knockbackDirection;
        public int hitstun;
        public int blockstun;
        public int hitpause;
        public AttackType attackType;
        public bool canBeBlocked;
    }
}