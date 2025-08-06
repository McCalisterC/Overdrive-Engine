using System.Collections.Generic;
using UnityEngine;

namespace FightingFramework.Animation
{
    public struct HitResult
    {
        public GameObject attacker;
        public GameObject victim;
        public Rectangle hitbox;
        public Rectangle hurtbox;
        public Vector2 hitPoint;
        public float distance;
        public bool wasBlocked;
        
        public HitResult(GameObject attacker, GameObject victim, Rectangle hitbox, Rectangle hurtbox, Vector2 hitPoint)
        {
            this.attacker = attacker;
            this.victim = victim;
            this.hitbox = hitbox;
            this.hurtbox = hurtbox;
            this.hitPoint = hitPoint;
            this.distance = Vector2.Distance(hitbox.center, hurtbox.center);
            this.wasBlocked = false;
        }
    }
    
    public class HitboxManager : MonoBehaviour
    {
        [Header("Collision Settings")]
        [SerializeField] private LayerMask targetLayers = -1;
        [SerializeField] private bool enableProximityDetection = true;
        [SerializeField] private float proximityRange = 2f;
        
        [Header("Debug")]
        [SerializeField] private bool debugMode = false;
        [SerializeField] private bool drawGizmos = true;
        [SerializeField] private float gizmoAlpha = 0.3f;
        
        // Active collision boxes
        private List<Rectangle> activeHitboxes = new List<Rectangle>();
        private List<Rectangle> activeHurtboxes = new List<Rectangle>();
        private List<Rectangle> activeCollisionBoxes = new List<Rectangle>();
        private List<Rectangle> activeProximityBoxes = new List<Rectangle>();
        
        // Hit tracking (prevents multiple hits per frame)
        private HashSet<GameObject> hitThisFrame = new HashSet<GameObject>();
        private int lastHitFrame = -1;
        
        // Events
        public System.Action<HitResult> OnHitDetected;
        public System.Action<GameObject, GameObject> OnProximityEntered;
        public System.Action<GameObject, GameObject> OnProximityExited;
        
        // Static collision manager for global hit detection
        private static List<HitboxManager> allManagers = new List<HitboxManager>();
        
        // Properties
        public List<Rectangle> ActiveHitboxes => new List<Rectangle>(activeHitboxes);
        public List<Rectangle> ActiveHurtboxes => new List<Rectangle>(activeHurtboxes);
        public List<Rectangle> ActiveCollisionBoxes => new List<Rectangle>(activeCollisionBoxes);
        public bool HasActiveHitboxes => activeHitboxes.Count > 0;
        public bool HasActiveHurtboxes => activeHurtboxes.Count > 0;
        
        private void Awake()
        {
            allManagers.Add(this);
        }
        
        private void OnDestroy()
        {
            allManagers.Remove(this);
        }
        
        private void LateUpdate()
        {
            // Reset hit tracking each frame
            if (Time.frameCount != lastHitFrame)
            {
                hitThisFrame.Clear();
                lastHitFrame = Time.frameCount;
            }
            
            // Check collisions with other hitbox managers
            CheckCollisionsWithOthers();
            
            // Check proximity if enabled
            if (enableProximityDetection)
            {
                CheckProximity();
            }
        }
        
        public void UpdateBoxes(List<Rectangle> hitboxes, List<Rectangle> hurtboxes, List<Rectangle> collisionBoxes = null)
        {
            activeHitboxes.Clear();
            activeHurtboxes.Clear();
            activeCollisionBoxes.Clear();
            
            if (hitboxes != null) activeHitboxes.AddRange(hitboxes);
            if (hurtboxes != null) activeHurtboxes.AddRange(hurtboxes);
            if (collisionBoxes != null) activeCollisionBoxes.AddRange(collisionBoxes);
            
            if (debugMode)
            {
                Debug.Log($"{gameObject.name}: Updated boxes - Hitboxes: {activeHitboxes.Count}, Hurtboxes: {activeHurtboxes.Count}");
            }
        }
        
        public void UpdateProximityBoxes(List<Rectangle> proximityBoxes)
        {
            activeProximityBoxes.Clear();
            if (proximityBoxes != null)
            {
                activeProximityBoxes.AddRange(proximityBoxes);
            }
        }
        
        private void CheckCollisionsWithOthers()
        {
            foreach (var other in allManagers)
            {
                if (other == this || other == null) continue;
                
                // Skip if we already hit this target this frame
                if (hitThisFrame.Contains(other.gameObject)) continue;
                
                // Check if we should collide based on layer mask
                if (!IsInLayerMask(other.gameObject.layer, targetLayers)) continue;
                
                // Check for hits
                var hitResult = CheckCollision(other);
                if (hitResult.HasValue)
                {
                    ProcessHit(hitResult.Value);
                    hitThisFrame.Add(other.gameObject);
                }
            }
        }
        
        public HitResult? CheckCollision(HitboxManager other)
        {
            if (other == null) return null;
            
            // Check our hitboxes against their hurtboxes
            foreach (var hitbox in activeHitboxes)
            {
                foreach (var hurtbox in other.activeHurtboxes)
                {
                    if (RectangleOverlap(hitbox, hurtbox))
                    {
                        Vector2 hitPoint = GetHitPoint(hitbox, hurtbox);
                        return new HitResult(gameObject, other.gameObject, hitbox, hurtbox, hitPoint);
                    }
                }
            }
            
            return null;
        }
        
        private bool RectangleOverlap(Rectangle rect1, Rectangle rect2)
        {
            var r1 = rect1.ToRect();
            var r2 = rect2.ToRect();
            return r1.Overlaps(r2);
        }
        
        private Vector2 GetHitPoint(Rectangle hitbox, Rectangle hurtbox)
        {
            // Calculate the intersection point (center of overlap)
            var r1 = hitbox.ToRect();
            var r2 = hurtbox.ToRect();
            
            float overlapLeft = Mathf.Max(r1.xMin, r2.xMin);
            float overlapRight = Mathf.Min(r1.xMax, r2.xMax);
            float overlapBottom = Mathf.Max(r1.yMin, r2.yMin);
            float overlapTop = Mathf.Min(r1.yMax, r2.yMax);
            
            return new Vector2(
                (overlapLeft + overlapRight) * 0.5f,
                (overlapBottom + overlapTop) * 0.5f
            );
        }
        
        private void ProcessHit(HitResult hitResult)
        {
            if (debugMode)
            {
                Debug.Log($"Hit: {hitResult.attacker.name} hit {hitResult.victim.name} at {hitResult.hitPoint}");
            }
            
            // Fire hit event
            OnHitDetected?.Invoke(hitResult);
            
            // Try to get the victim's hitbox manager to notify them of the hit
            var victimManager = hitResult.victim.GetComponent<HitboxManager>();
            if (victimManager != null)
            {
                victimManager.OnReceiveHit(hitResult);
            }
            
            // Apply hit effects
            ApplyHitEffects(hitResult);
        }
        
        private void OnReceiveHit(HitResult hitResult)
        {
            // This is called when this object receives a hit from another
            if (debugMode)
            {
                Debug.Log($"{gameObject.name} received hit from {hitResult.attacker.name}");
            }
            
            // Could trigger hit reactions, damage, etc.
            // This would typically interface with health systems, state machines, etc.
        }
        
        private void ApplyHitEffects(HitResult hitResult)
        {
            var hitbox = hitResult.hitbox;
            
            // Spawn hit effect if specified
            if (hitbox.debugColor != Color.clear)
            {
                // Visual hit effect would go here
            }
            
            // Apply hitstun/blockstun
            ApplyHitStun(hitResult);
            
            // Apply knockback
            ApplyKnockback(hitResult);
        }
        
        private void ApplyHitStun(HitResult hitResult)
        {
            // This would typically interface with the state machine system
            // For now, just log the effect
            if (debugMode)
            {
                Debug.Log($"Applying {hitResult.hitbox.hitstun} frames of hitstun to {hitResult.victim.name}");
            }
        }
        
        private void ApplyKnockback(HitResult hitResult)
        {
            var hitbox = hitResult.hitbox;
            if (hitbox.knockback <= 0) return;
            
            var rigidbody = hitResult.victim.GetComponent<Rigidbody2D>();
            if (rigidbody != null)
            {
                Vector2 knockbackForce = hitbox.knockbackDirection.normalized * hitbox.knockback;
                rigidbody.AddForce(knockbackForce, ForceMode2D.Impulse);
            }
        }
        
        private void CheckProximity()
        {
            foreach (var other in allManagers)
            {
                if (other == this || other == null) continue;
                
                float distance = Vector2.Distance(transform.position, other.transform.position);
                if (distance <= proximityRange)
                {
                    OnProximityEntered?.Invoke(gameObject, other.gameObject);
                }
                else
                {
                    OnProximityExited?.Invoke(gameObject, other.gameObject);
                }
            }
        }
        
        private bool IsInLayerMask(int layer, LayerMask layerMask)
        {
            return (layerMask.value & (1 << layer)) != 0;
        }
        
        // Utility methods
        public void ClearAllBoxes()
        {
            activeHitboxes.Clear();
            activeHurtboxes.Clear();
            activeCollisionBoxes.Clear();
            activeProximityBoxes.Clear();
        }
        
        public bool HasBoxOfType(BoxType boxType)
        {
            switch (boxType)
            {
                case BoxType.Hitbox:
                    return activeHitboxes.Count > 0;
                case BoxType.Hurtbox:
                    return activeHurtboxes.Count > 0;
                case BoxType.Collision:
                    return activeCollisionBoxes.Count > 0;
                case BoxType.Proximity:
                    return activeProximityBoxes.Count > 0;
                default:
                    return false;
            }
        }
        
        public List<Rectangle> GetBoxesByType(BoxType boxType)
        {
            switch (boxType)
            {
                case BoxType.Hitbox:
                    return new List<Rectangle>(activeHitboxes);
                case BoxType.Hurtbox:
                    return new List<Rectangle>(activeHurtboxes);
                case BoxType.Collision:
                    return new List<Rectangle>(activeCollisionBoxes);
                case BoxType.Proximity:
                    return new List<Rectangle>(activeProximityBoxes);
                default:
                    return new List<Rectangle>();
            }
        }
        
        // Static utility methods
        public static List<HitboxManager> GetAllManagers()
        {
            return new List<HitboxManager>(allManagers);
        }
        
        public static List<HitboxManager> GetManagersWithActiveHitboxes()
        {
            var result = new List<HitboxManager>();
            foreach (var manager in allManagers)
            {
                if (manager != null && manager.HasActiveHitboxes)
                {
                    result.Add(manager);
                }
            }
            return result;
        }
        
        // Debug drawing
        private void OnDrawGizmos()
        {
            if (!drawGizmos) return;
            
            DrawBoxes(activeHitboxes, Color.red);
            DrawBoxes(activeHurtboxes, Color.blue);
            DrawBoxes(activeCollisionBoxes, Color.green);
            DrawBoxes(activeProximityBoxes, Color.yellow);
            
            // Draw proximity range
            if (enableProximityDetection)
            {
                Gizmos.color = new Color(1f, 1f, 0f, 0.1f);
                Gizmos.DrawSphere(transform.position, proximityRange);
            }
        }
        
        private void DrawBoxes(List<Rectangle> boxes, Color color)
        {
            Gizmos.color = new Color(color.r, color.g, color.b, gizmoAlpha);
            
            foreach (var box in boxes)
            {
                Gizmos.DrawCube(box.center, box.size);
                
                // Draw wireframe
                Gizmos.color = color;
                Gizmos.DrawWireCube(box.center, box.size);
                Gizmos.color = new Color(color.r, color.g, color.b, gizmoAlpha);
            }
        }
        
        private void OnDrawGizmosSelected()
        {
            if (!drawGizmos) return;
            
            // Draw more detailed info when selected
            Gizmos.color = Color.white;
            
            // Draw hit tracking info
            if (hitThisFrame.Count > 0)
            {
                foreach (var hitTarget in hitThisFrame)
                {
                    if (hitTarget != null)
                    {
                        Gizmos.DrawLine(transform.position, hitTarget.transform.position);
                    }
                }
            }
        }
    }
}