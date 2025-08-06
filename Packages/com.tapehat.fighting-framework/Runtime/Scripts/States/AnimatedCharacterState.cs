using UnityEngine;
using FightingFramework.Animation;

namespace FightingFramework.States
{
    public abstract class AnimatedCharacterState : CharacterState
    {
        [Header("Frame-Based Animation")]
        public AnimationData frameAnimation;
        public bool useFrameBasedAnimation = true;
        public bool syncWithFrameData = true;
        
        [Header("Animation Events")]
        public bool processFrameEvents = true;
        
        protected FrameBasedAnimator frameAnimator;
        protected HitboxManager hitboxManager;
        
        public override void Enter(CharacterController character)
        {
            if (useFrameBasedAnimation)
            {
                InitializeFrameAnimation(character);
                PlayFrameAnimation();
            }
        }
        
        public override void UpdateState(CharacterController character)
        {
            if (useFrameBasedAnimation && syncWithFrameData)
            {
                SyncWithFrameAnimation(character);
            }
        }
        
        public override void Exit(CharacterController character)
        {
            if (useFrameBasedAnimation && frameAnimator != null)
            {
                frameAnimator.StopAnimation();
            }
        }
        
        protected virtual void InitializeFrameAnimation(CharacterController character)
        {
            // Get or create frame animator
            frameAnimator = character.GetComponent<FrameBasedAnimator>();
            if (frameAnimator == null)
            {
                frameAnimator = character.gameObject.AddComponent<FrameBasedAnimator>();
            }
            
            // Get or create hitbox manager
            hitboxManager = character.GetComponent<HitboxManager>();
            if (hitboxManager == null)
            {
                hitboxManager = character.gameObject.AddComponent<HitboxManager>();
            }
            
            // Subscribe to frame events
            if (processFrameEvents)
            {
                frameAnimator.OnFrameChanged += OnFrameChanged;
                frameAnimator.OnFrameEvent += OnFrameEvent;
                frameAnimator.OnAnimationComplete += OnAnimationComplete;
            }
        }
        
        protected virtual void PlayFrameAnimation()
        {
            if (frameAnimation != null && frameAnimator != null)
            {
                frameAnimator.PlayAnimation(frameAnimation);
            }
        }
        
        protected virtual void SyncWithFrameAnimation(CharacterController character)
        {
            if (frameAnimator == null || !frameAnimator.IsPlaying) return;
            
            // Sync frame data with state properties
            UpdateFrameDataSync();
        }
        
        protected virtual void UpdateFrameDataSync()
        {
            if (frameAnimator?.CurrentAnimation == null) return;
            
            var currentFrameData = frameAnimator.GetCurrentFrameData();
            
            // Update frame counts from animation data
            if (syncWithFrameData && frameAnimation != null)
            {
                // You could sync startup/active/recovery frames based on animation markers
                // For now, we'll use the total frame count
                var totalFrames = frameAnimation.totalFrames;
                
                // Example: divide animation into thirds for startup/active/recovery
                if (startupFrames == 0 && activeFrames == 0 && recoveryFrames == 0)
                {
                    startupFrames = totalFrames / 3;
                    activeFrames = totalFrames / 3;
                    recoveryFrames = totalFrames - startupFrames - activeFrames;
                }
            }
        }
        
        protected virtual void OnFrameChanged(FrameData frameData)
        {
            // Override in derived classes to handle frame changes
        }
        
        protected virtual void OnFrameEvent(FrameEvent frameEvent)
        {
            // Process common frame events
            switch (frameEvent.eventType)
            {
                case FrameEventType.PlaySound:
                    HandlePlaySoundEvent(frameEvent);
                    break;
                case FrameEventType.SpawnEffect:
                    HandleSpawnEffectEvent(frameEvent);
                    break;
                case FrameEventType.CameraShake:
                    HandleCameraShakeEvent(frameEvent);
                    break;
                case FrameEventType.Custom:
                    HandleCustomEvent(frameEvent);
                    break;
            }
        }
        
        protected virtual void OnAnimationComplete(AnimationData animation)
        {
            // Handle animation completion - could trigger state transitions
            if (!animation.looping)
            {
                // Non-looping animation finished, might want to transition to idle
                OnAnimationFinished();
            }
        }
        
        protected virtual void OnAnimationFinished()
        {
            // Override in derived classes
        }
        
        protected virtual void HandlePlaySoundEvent(FrameEvent frameEvent)
        {
            if (string.IsNullOrEmpty(frameEvent.stringParameter)) return;
            
            var audioSource = frameAnimator.GetComponent<AudioSource>();
            if (audioSource != null)
            {
                // This would typically use a sound manager
                Debug.Log($"Playing sound: {frameEvent.stringParameter}");
            }
        }
        
        protected virtual void HandleSpawnEffectEvent(FrameEvent frameEvent)
        {
            if (string.IsNullOrEmpty(frameEvent.stringParameter)) return;
            
            Vector2 spawnPosition = (Vector2)frameAnimator.transform.position + frameEvent.vector2Parameter;
            Debug.Log($"Spawning effect: {frameEvent.stringParameter} at {spawnPosition}");
        }
        
        protected virtual void HandleCameraShakeEvent(FrameEvent frameEvent)
        {
            Debug.Log($"Camera shake intensity: {frameEvent.floatParameter}");
        }
        
        protected virtual void HandleCustomEvent(FrameEvent frameEvent)
        {
            // Override in derived classes for custom event handling
        }
        
        public override int GetTotalFrames()
        {
            if (useFrameBasedAnimation && frameAnimation != null)
            {
                return frameAnimation.totalFrames;
            }
            return base.GetTotalFrames();
        }
        
        public override bool IsInStartup(int currentFrame)
        {
            if (useFrameBasedAnimation && syncWithFrameData)
            {
                // Could use frame markers or animation events to determine phases
                return base.IsInStartup(currentFrame);
            }
            return base.IsInStartup(currentFrame);
        }
        
        public override bool IsActive(int currentFrame)
        {
            if (useFrameBasedAnimation && syncWithFrameData)
            {
                // Check if current frame has active hitboxes
                if (frameAnimator != null)
                {
                    var frameData = frameAnimator.GetCurrentFrameData();
                    return frameData.hitboxes != null && frameData.hitboxes.Count > 0;
                }
            }
            return base.IsActive(currentFrame);
        }
        
        public override bool IsInRecovery(int currentFrame)
        {
            if (useFrameBasedAnimation && syncWithFrameData)
            {
                return base.IsInRecovery(currentFrame);
            }
            return base.IsInRecovery(currentFrame);
        }
        
        protected void CleanupFrameAnimation()
        {
            if (frameAnimator != null && processFrameEvents)
            {
                frameAnimator.OnFrameChanged -= OnFrameChanged;
                frameAnimator.OnFrameEvent -= OnFrameEvent;
                frameAnimator.OnAnimationComplete -= OnAnimationComplete;
            }
        }
        
        private void OnDestroy()
        {
            CleanupFrameAnimation();
        }
    }
}