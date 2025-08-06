using UnityEngine;

namespace FightingFramework.States
{
    [System.Serializable]
    public class HitboxData
    {
        [Header("Hitbox Properties")]
        public string hitboxName = "Attack";
        public HitboxType hitboxType = HitboxType.Attack;
        
        [Header("Timing")]
        public int startFrame;
        public int endFrame;
        
        [Header("Damage & Properties")]
        public int damage = 10;
        public float knockback = 5f;
        public Vector2 knockbackDirection = Vector2.right;
        public int hitstun = 15;
        public int blockstun = 8;
        public int hitpause = 3;
        
        [Header("Spatial Properties")]
        public Vector2 offset = Vector2.zero;
        public Vector2 size = Vector2.one;
        public LayerMask targetLayers = -1;
        
        [Header("Attack Properties")]
        public AttackType attackType = AttackType.Mid;
        public bool canBeBlocked = true;
        public bool causesWallBounce = false;
        public bool causesGroundBounce = false;
        public bool isProjectile = false;
        
        [Header("Visual Effects")]
        public GameObject hitEffect;
        public GameObject blockEffect;
        public AudioClip hitSound;
        public AudioClip blockSound;
        
        public bool IsActiveOnFrame(int frame)
        {
            return frame >= startFrame && frame <= endFrame;
        }
        
        public Rect GetHitboxRect(Vector2 characterPosition, bool facingRight)
        {
            Vector2 adjustedOffset = offset;
            if (!facingRight)
            {
                adjustedOffset.x = -adjustedOffset.x;
            }
            
            Vector2 worldPosition = characterPosition + adjustedOffset;
            return new Rect(worldPosition - size * 0.5f, size);
        }
        
        public Vector2 GetKnockbackDirection(bool attackerFacingRight)
        {
            Vector2 direction = knockbackDirection;
            if (!attackerFacingRight)
            {
                direction.x = -direction.x;
            }
            return direction.normalized;
        }
    }
    
    public enum HitboxType
    {
        Attack,
        Projectile,
        Counter,
        Grab,
        Hazard
    }
    
    public enum AttackType
    {
        High,
        Mid,
        Low,
        Overhead,
        Unblockable,
        Grab
    }
}