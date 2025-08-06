using System.Collections.Generic;
using UnityEngine;

namespace FightingFramework.Animation
{
    public class FrameBasedAnimator : MonoBehaviour
    {
        [Header("Animation Components")]
        [SerializeField] private AnimationData currentAnimation;
        [SerializeField] private SpriteRenderer spriteRenderer;
        [SerializeField] private HitboxManager hitboxManager;
        
        [Header("Animation Settings")]
        [SerializeField] private bool playOnStart = false;
        [SerializeField] private bool useGlobalFrameRate = true;
        [SerializeField] private float customFrameRate = 60f;
        
        [Header("Debug")]
        [SerializeField] private bool debugMode = false;
        [SerializeField] private bool showBoxes = true;
        
        // Animation state
        private int currentFrame;
        private float frameTimer;
        private bool isPlaying;
        private bool isPaused;
        
        // Event system
        public System.Action<AnimationData> OnAnimationStart;
        public System.Action<AnimationData> OnAnimationComplete;
        public System.Action<AnimationData> OnAnimationLoop;
        public System.Action<FrameData> OnFrameChanged;
        public System.Action<FrameEvent> OnFrameEvent;
        
        // Properties
        public AnimationData CurrentAnimation => currentAnimation;
        public int CurrentFrame => currentFrame;
        public bool IsPlaying => isPlaying && !isPaused;
        public bool IsPaused => isPaused;
        public float CurrentFrameRate => useGlobalFrameRate ? (currentAnimation?.frameRate ?? 60f) : customFrameRate;
        public float NormalizedTime => currentAnimation != null ? (float)currentFrame / currentAnimation.totalFrames : 0f;
        
        private void Awake()
        {
            // Auto-find components if not assigned
            if (spriteRenderer == null)
                spriteRenderer = GetComponent<SpriteRenderer>();
            
            if (hitboxManager == null)
                hitboxManager = GetComponent<HitboxManager>();
            
            // Ensure we have a sprite renderer
            if (spriteRenderer == null)
                spriteRenderer = gameObject.AddComponent<SpriteRenderer>();
            
            // Ensure we have a hitbox manager
            if (hitboxManager == null)
                hitboxManager = gameObject.AddComponent<HitboxManager>();
        }
        
        private void Start()
        {
            if (playOnStart && currentAnimation != null)
            {
                PlayAnimation(currentAnimation);
            }
        }
        
        private void Update()
        {
            if (!isPlaying || isPaused || currentAnimation == null) return;
            
            UpdateAnimation();
        }
        
        private void UpdateAnimation()
        {
            frameTimer += Time.deltaTime;
            float frameDuration = 1f / CurrentFrameRate;
            
            if (frameTimer >= frameDuration)
            {
                AdvanceFrame();
                frameTimer -= frameDuration; // Maintain sub-frame precision
            }
        }
        
        private void AdvanceFrame()
        {
            currentFrame++;
            
            // Handle animation completion/looping
            if (currentFrame >= currentAnimation.totalFrames)
            {
                if (currentAnimation.looping)
                {
                    currentFrame = currentAnimation.loopStartFrame;
                    OnAnimationLoop?.Invoke(currentAnimation);
                }
                else
                {
                    currentFrame = currentAnimation.totalFrames - 1;
                    isPlaying = false;
                    OnAnimationComplete?.Invoke(currentAnimation);
                    return;
                }
            }
            
            ApplyFrame();
        }
        
        private void ApplyFrame()
        {
            if (currentAnimation == null) return;
            
            var frameData = currentAnimation.GetFrame(currentFrame);
            
            // Update sprite
            if (spriteRenderer != null && frameData.sprite != null)
            {
                spriteRenderer.sprite = frameData.sprite;
                spriteRenderer.color = frameData.spriteColor;
                
                // Apply sprite transformations
                var localPos = transform.localPosition;
                localPos.x = frameData.spriteOffset.x;
                localPos.y = frameData.spriteOffset.y;
                transform.localPosition = localPos;
                
                var scale = transform.localScale;
                scale.x = frameData.spriteScale.x;
                scale.y = frameData.spriteScale.y;
                transform.localScale = scale;
                
                transform.localRotation = Quaternion.Euler(0, 0, frameData.spriteRotation);
            }
            
            // Update hitboxes
            UpdateHitboxes(frameData);
            
            // Apply root motion
            if (!frameData.lockPosition)
            {
                ApplyRootMotion(frameData.rootMotion);
            }
            
            // Process frame events
            ProcessFrameEvents(frameData);
            
            // Fire frame changed event
            OnFrameChanged?.Invoke(frameData);
            
            if (debugMode)
            {
                Debug.Log($"Frame {currentFrame}: {frameData.sprite?.name ?? "No Sprite"}");
            }
        }
        
        private void UpdateHitboxes(FrameData frameData)
        {
            if (hitboxManager == null) return;
            
            // Convert frame data boxes to world space and update hitbox manager
            var worldHitboxes = new List<Rectangle>();
            var worldHurtboxes = new List<Rectangle>();
            var worldCollisionBoxes = new List<Rectangle>();
            
            bool facingRight = transform.localScale.x > 0;
            Vector2 characterPosition = transform.position;
            
            // Transform hitboxes to world space
            foreach (var hitbox in frameData.hitboxes ?? new List<Rectangle>())
            {
                var worldHitbox = TransformRectangleToWorldSpace(hitbox, characterPosition, facingRight);
                worldHitboxes.Add(worldHitbox);
            }
            
            foreach (var hurtbox in frameData.hurtboxes ?? new List<Rectangle>())
            {
                var worldHurtbox = TransformRectangleToWorldSpace(hurtbox, characterPosition, facingRight);
                worldHurtboxes.Add(worldHurtbox);
            }
            
            foreach (var collision in frameData.collisionBoxes ?? new List<Rectangle>())
            {
                var worldCollision = TransformRectangleToWorldSpace(collision, characterPosition, facingRight);
                worldCollisionBoxes.Add(worldCollision);
            }
            
            hitboxManager.UpdateBoxes(worldHitboxes, worldHurtboxes, worldCollisionBoxes);
        }
        
        private Rectangle TransformRectangleToWorldSpace(Rectangle localRect, Vector2 worldPosition, bool facingRight)
        {
            var worldRect = localRect;
            
            // Apply facing direction
            if (!facingRight)
            {
                worldRect.center.x = -worldRect.center.x;
                if (worldRect.knockbackDirection != Vector2.zero)
                {
                    worldRect.knockbackDirection.x = -worldRect.knockbackDirection.x;
                }
            }
            
            // Transform to world space
            worldRect.center += worldPosition;
            
            return worldRect;
        }
        
        private void ApplyRootMotion(Vector2 rootMotion)
        {
            if (rootMotion == Vector2.zero) return;
            
            // Apply facing direction to root motion
            bool facingRight = transform.localScale.x > 0;
            if (!facingRight)
            {
                rootMotion.x = -rootMotion.x;
            }
            
            // Apply root motion
            transform.position += (Vector3)rootMotion;
        }
        
        private void ProcessFrameEvents(FrameData frameData)
        {
            if (frameData.events == null) return;
            
            foreach (var frameEvent in frameData.events)
            {
                ProcessFrameEvent(frameEvent);
                OnFrameEvent?.Invoke(frameEvent);
            }
            
            // Process global events for this frame
            var globalEvents = currentAnimation.GetEventsForFrame(currentFrame);
            foreach (var globalEvent in globalEvents)
            {
                ProcessFrameEvent(globalEvent);
                OnFrameEvent?.Invoke(globalEvent);
            }
        }
        
        private void ProcessFrameEvent(FrameEvent frameEvent)
        {
            switch (frameEvent.eventType)
            {
                case FrameEventType.PlaySound:
                    PlaySound(frameEvent.stringParameter);
                    break;
                    
                case FrameEventType.SpawnEffect:
                    SpawnEffect(frameEvent.stringParameter, frameEvent.vector2Parameter);
                    break;
                    
                case FrameEventType.CameraShake:
                    TriggerCameraShake(frameEvent.floatParameter);
                    break;
                    
                case FrameEventType.Custom:
                    ProcessCustomEvent(frameEvent);
                    break;
            }
        }
        
        private void PlaySound(string soundName)
        {
            if (string.IsNullOrEmpty(soundName)) return;
            
            var audioSource = GetComponent<AudioSource>();
            if (audioSource != null)
            {
                // This would typically load from a sound manager
                Debug.Log($"Playing sound: {soundName}");
            }
        }
        
        private void SpawnEffect(string effectName, Vector2 localPosition)
        {
            if (string.IsNullOrEmpty(effectName)) return;
            
            Vector2 worldPosition = (Vector2)transform.position + localPosition;
            Debug.Log($"Spawning effect: {effectName} at {worldPosition}");
        }
        
        private void TriggerCameraShake(float intensity)
        {
            Debug.Log($"Camera shake with intensity: {intensity}");
            // Camera shake implementation would go here
        }
        
        private void ProcessCustomEvent(FrameEvent frameEvent)
        {
            Debug.Log($"Custom event: {frameEvent.eventName}");
            // Custom event processing would go here
        }
        
        // Public API
        public void PlayAnimation(AnimationData animation, int startFrame = 0)
        {
            if (animation == null) return;
            
            currentAnimation = animation;
            currentFrame = Mathf.Clamp(startFrame, 0, animation.totalFrames - 1);
            frameTimer = 0f;
            isPlaying = true;
            isPaused = false;
            
            ApplyFrame();
            OnAnimationStart?.Invoke(animation);
        }
        
        public void StopAnimation()
        {
            isPlaying = false;
            isPaused = false;
            currentFrame = 0;
            frameTimer = 0f;
        }
        
        public void PauseAnimation()
        {
            isPaused = true;
        }
        
        public void ResumeAnimation()
        {
            isPaused = false;
        }
        
        public void SetFrame(int frameIndex)
        {
            if (currentAnimation == null) return;
            
            currentFrame = Mathf.Clamp(frameIndex, 0, currentAnimation.totalFrames - 1);
            frameTimer = 0f;
            ApplyFrame();
        }
        
        public void SetNormalizedTime(float normalizedTime)
        {
            if (currentAnimation == null) return;
            
            normalizedTime = Mathf.Clamp01(normalizedTime);
            int targetFrame = Mathf.RoundToInt(normalizedTime * (currentAnimation.totalFrames - 1));
            SetFrame(targetFrame);
        }
        
        public FrameData GetCurrentFrameData()
        {
            return currentAnimation?.GetFrame(currentFrame) ?? new FrameData();
        }
        
        // Debug drawing
        private void OnDrawGizmos()
        {
            if (!showBoxes || currentAnimation == null) return;
            
            var frameData = currentAnimation.GetFrame(currentFrame);
            DrawFrameGizmos(frameData);
        }
        
        private void DrawFrameGizmos(FrameData frameData)
        {
            bool facingRight = transform.localScale.x > 0;
            Vector2 characterPosition = transform.position;
            
            // Draw hitboxes (red)
            Gizmos.color = Color.red;
            foreach (var hitbox in frameData.hitboxes ?? new List<Rectangle>())
            {
                DrawRectangleGizmo(hitbox, characterPosition, facingRight);
            }
            
            // Draw hurtboxes (blue)
            Gizmos.color = Color.blue;
            foreach (var hurtbox in frameData.hurtboxes ?? new List<Rectangle>())
            {
                DrawRectangleGizmo(hurtbox, characterPosition, facingRight);
            }
            
            // Draw collision boxes (green)
            Gizmos.color = Color.green;
            foreach (var collision in frameData.collisionBoxes ?? new List<Rectangle>())
            {
                DrawRectangleGizmo(collision, characterPosition, facingRight);
            }
        }
        
        private void DrawRectangleGizmo(Rectangle rect, Vector2 worldPosition, bool facingRight)
        {
            var center = rect.center;
            if (!facingRight)
            {
                center.x = -center.x;
            }
            center += worldPosition;
            
            Gizmos.DrawWireCube(center, rect.size);
        }
    }
}