using UnityEngine;

namespace FightingFramework.States
{
    [CreateAssetMenu(fileName = "New Block State", menuName = "Fighting Framework/States/Block")]
    public class BlockState : CharacterState
    {
        [Header("Block Properties")]
        public int blockstunDuration = 8; // frames
        public float blockPushback = 2f;
        public bool canBlockOverheads = false;
        public bool canBlockLows = true;
        
        [Header("Perfect Block")]
        public bool allowPerfectBlock = true;
        public int perfectBlockWindow = 3; // frames at start of block
        public float perfectBlockAdvantage = 0.5f; // frame advantage multiplier
        
        [Header("Block Damage")]
        public bool takeChipDamage = true;
        public float chipDamageMultiplier = 0.1f;
        
        [Header("Recovery")]
        public CharacterState recoveryState;
        
        private int blockstunFramesRemaining;
        private bool isPerfectBlock;
        private HitInfo lastBlockedHit;
        
        public void Initialize(HitInfo hitInfo)
        {
            lastBlockedHit = hitInfo;
            blockstunFramesRemaining = hitInfo.blockstun;
            
            // Check for perfect block
            var stateMachine = FindFirstObjectByType<CharacterStateMachine>();
            if (allowPerfectBlock && stateMachine != null && stateMachine.CurrentFrame <= perfectBlockWindow)
            {
                isPerfectBlock = true;
                blockstunFramesRemaining = Mathf.RoundToInt(blockstunFramesRemaining * perfectBlockAdvantage);
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
            
            // Set default blockstun if not initialized
            if (blockstunFramesRemaining <= 0)
            {
                blockstunFramesRemaining = blockstunDuration;
            }
            
            Debug.Log($"Entered block state: {stateName} for {blockstunFramesRemaining} frames {(isPerfectBlock ? "(Perfect Block!)" : "")}");
        }
        
        public override void UpdateState(CharacterController character)
        {
            if (character == null) return;
            
            var stateMachine = character.GetComponent<CharacterStateMachine>();
            if (stateMachine == null) return;
            
            // Apply block pushback
            if (blockPushback > 0)
            {
                bool facingRight = character.transform.localScale.x > 0;
                Vector2 pushDirection = facingRight ? Vector2.left : Vector2.right;
                character.Move(pushDirection * blockPushback * Time.deltaTime);
            }
            
            // Reduce blockstun frames
            blockstunFramesRemaining--;
            
            // Check if we can exit block
            if (blockstunFramesRemaining <= 0)
            {
                // Check if still holding block
                if (UnityEngine.Input.GetButton("Block"))
                {
                    // Continue blocking
                    return;
                }
                else
                {
                    // Exit block
                    TransitionToRecovery(stateMachine);
                }
            }
        }
        
        public override void Exit(CharacterController character)
        {
            if (character == null) return;
            
            blockstunFramesRemaining = 0;
            isPerfectBlock = false;
            
            Debug.Log($"Exited block state: {stateName}");
        }
        
        public override bool CanTransitionTo(CharacterState newState)
        {
            if (newState == null) return false;
            
            // During blockstun, only allow specific transitions
            if (blockstunFramesRemaining > 0)
            {
                // Allow transition to recovery
                if (newState == recoveryState) return true;
                
                // Allow higher priority states during perfect block
                if (isPerfectBlock && newState.priority > this.priority)
                {
                    return true;
                }
                
                return false;
            }
            
            // After blockstun, allow normal transitions
            return true;
        }
        
        public bool CanBlockAttack(AttackType attackType)
        {
            switch (attackType)
            {
                case AttackType.High:
                case AttackType.Mid:
                    return true;
                case AttackType.Low:
                    return canBlockLows;
                case AttackType.Overhead:
                    return canBlockOverheads;
                case AttackType.Unblockable:
                case AttackType.Grab:
                    return false;
                default:
                    return false;
            }
        }
        
        private void TransitionToRecovery(CharacterStateMachine stateMachine)
        {
            if (recoveryState != null)
            {
                stateMachine.ChangeState(recoveryState);
            }
            else
            {
                // Default to idle
                var idleState = stateMachine.FindStateByName("Idle");
                if (idleState != null)
                {
                    stateMachine.ChangeState(idleState);
                }
            }
        }
        
        public int GetRemainingBlockstun()
        {
            return blockstunFramesRemaining;
        }
        
        public bool WasPerfectBlock()
        {
            return isPerfectBlock;
        }
    }
}