using UnityEngine;

namespace FightingFramework.States
{
    [CreateAssetMenu(fileName = "New Movement State", menuName = "Fighting Framework/States/Movement")]
    public class MovementState : CharacterState
    {
        [Header("Movement Properties")]
        public float moveSpeed = 5f;
        public bool allowAirMovement = false;
        public bool maintainMomentum = false;
        
        [Header("Input")]
        public string horizontalInputAxis = "Horizontal";
        public string verticalInputAxis = "Vertical";
        
        [Header("Movement Constraints")]
        public bool constrainToGround = true;
        public float maxVelocity = 10f;
        public float acceleration = 20f;
        public float deceleration = 15f;
        
        private Vector2 currentVelocity;
        
        public override void Enter(CharacterController character)
        {
            if (character == null) return;
            
            var animator = character.GetComponent<Animator>();
            if (animator != null && animation != null)
            {
                animator.Play(animation.name);
            }
            
            if (!maintainMomentum)
            {
                currentVelocity = Vector2.zero;
            }
            else
            {
                currentVelocity = character.velocity;
            }
            
            Debug.Log($"Entered movement state: {stateName}");
        }
        
        public override void UpdateState(CharacterController character)
        {
            if (character == null) return;
            
            // Check ground constraint
            if (constrainToGround && !character.isGrounded && !allowAirMovement)
            {
                return;
            }
            
            // Get input
            float horizontalInput = UnityEngine.Input.GetAxis(horizontalInputAxis);
            float verticalInput = UnityEngine.Input.GetAxis(verticalInputAxis);
            
            Vector2 inputVector = new Vector2(horizontalInput, verticalInput);
            
            // Apply movement
            ApplyMovement(character, inputVector);
            
            // Update facing direction
            UpdateFacing(character, horizontalInput);
        }
        
        public override void Exit(CharacterController character)
        {
            if (character == null) return;
            
            Debug.Log($"Exited movement state: {stateName}");
        }
        
        public override bool CanTransitionTo(CharacterState newState)
        {
            // Movement states are generally interruptible
            return canBeInterrupted;
        }
        
        private void ApplyMovement(CharacterController character, Vector2 inputVector)
        {
            Vector2 targetVelocity = inputVector * moveSpeed;
            
            // Clamp to max velocity
            if (targetVelocity.magnitude > maxVelocity)
            {
                targetVelocity = targetVelocity.normalized * maxVelocity;
            }
            
            // Apply acceleration/deceleration
            float currentAcceleration = inputVector.magnitude > 0.1f ? acceleration : deceleration;
            currentVelocity = Vector2.MoveTowards(currentVelocity, targetVelocity, currentAcceleration * Time.deltaTime);
            
            // Constrain vertical movement if on ground
            if (constrainToGround && character.isGrounded)
            {
                currentVelocity.y = Mathf.Min(currentVelocity.y, 0f);
            }
            
            // Apply movement
            character.Move(currentVelocity * Time.deltaTime);
        }
        
        private void UpdateFacing(CharacterController character, float horizontalInput)
        {
            if (Mathf.Abs(horizontalInput) > 0.1f)
            {
                bool shouldFaceRight = horizontalInput > 0;
                Vector3 scale = character.transform.localScale;
                scale.x = shouldFaceRight ? Mathf.Abs(scale.x) : -Mathf.Abs(scale.x);
                character.transform.localScale = scale;
            }
        }
    }
}