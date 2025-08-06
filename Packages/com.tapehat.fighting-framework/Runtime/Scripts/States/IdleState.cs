using UnityEngine;

namespace FightingFramework.States
{
    [CreateAssetMenu(fileName = "New Idle State", menuName = "Fighting Framework/States/Idle")]
    public class IdleState : CharacterState
    {
        [Header("Idle Properties")]
        public bool allowInputBuffering = true;
        public float idleAnimationSpeed = 1f;
        
        [Header("Auto-transitions")]
        public bool autoTransitionToMovement = true;
        public string movementInputAxis = "Horizontal";
        public float movementThreshold = 0.1f;
        
        public override void Enter(CharacterController character)
        {
            if (character == null) return;
            
            var animator = character.GetComponent<Animator>();
            if (animator != null)
            {
                if (animation != null)
                {
                    animator.Play(animation.name);
                }
                animator.speed = idleAnimationSpeed;
            }
            
            // Reset velocity when entering idle
            character.Move(Vector2.zero);
            
            Debug.Log($"Entered idle state: {stateName}");
        }
        
        public override void UpdateState(CharacterController character)
        {
            if (character == null) return;
            
            // Apply gravity if not grounded
            if (!character.isGrounded)
            {
                character.Move(Vector2.down * 9.81f * Time.deltaTime);
            }
            
            // Check for movement input if auto-transition is enabled
            if (autoTransitionToMovement)
            {
                float movementInput = UnityEngine.Input.GetAxis(movementInputAxis);
                if (Mathf.Abs(movementInput) > movementThreshold)
                {
                    var stateMachine = character.GetComponent<CharacterStateMachine>();
                    if (stateMachine != null)
                    {
                        var movementState = stateMachine.FindStateByName("Walk");
                        if (movementState != null)
                        {
                            stateMachine.TryChangeState(movementState);
                        }
                    }
                }
            }
        }
        
        public override void Exit(CharacterController character)
        {
            if (character == null) return;
            
            var animator = character.GetComponent<Animator>();
            if (animator != null)
            {
                animator.speed = 1f; // Reset animation speed
            }
            
            Debug.Log($"Exited idle state: {stateName}");
        }
        
        public override bool CanTransitionTo(CharacterState newState)
        {
            // Idle state should be easily interruptible by most states
            return true;
        }
    }
}