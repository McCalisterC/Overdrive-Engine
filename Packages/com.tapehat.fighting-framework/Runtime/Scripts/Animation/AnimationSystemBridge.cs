using UnityEngine;
using FightingFramework.States;

namespace FightingFramework.Animation
{
    public static class AnimationSystemBridge
    {
        public static HitboxData ConvertRectangleToHitboxData(Rectangle rect, int frameNumber)
        {
            return new HitboxData
            {
                hitboxName = rect.label,
                startFrame = frameNumber,
                endFrame = frameNumber,
                damage = rect.damage,
                knockback = rect.knockback,
                knockbackDirection = rect.knockbackDirection,
                hitstun = (int)rect.hitstun,
                blockstun = (int)rect.blockstun,
                offset = rect.center,
                size = rect.size,
                attackType = GetAttackTypeFromBox(rect),
                canBeBlocked = rect.type != BoxType.Proximity
            };
        }
        
        public static Rectangle ConvertHitboxDataToRectangle(HitboxData hitboxData)
        {
            return new Rectangle
            {
                center = hitboxData.offset,
                size = hitboxData.size,
                type = BoxType.Hitbox,
                damage = hitboxData.damage,
                hitstun = hitboxData.hitstun,
                blockstun = hitboxData.blockstun,
                knockback = hitboxData.knockback,
                knockbackDirection = hitboxData.knockbackDirection,
                debugColor = Color.red,
                label = hitboxData.hitboxName
            };
        }
        
        public static AttackType GetAttackTypeFromBox(Rectangle rect)
        {
            // Simple heuristic based on position
            if (rect.center.y > 1f) return AttackType.High;
            if (rect.center.y < -0.5f) return AttackType.Low;
            return AttackType.Mid;
        }
        
        public static void SyncStateWithAnimationData(CharacterState state, AnimationData animData)
        {
            if (state == null || animData == null) return;
            
            // Sync animation clip
            if (state.animation == null && !string.IsNullOrEmpty(animData.animationName))
            {
                // Try to find matching AnimationClip by name
                var clips = Resources.FindObjectsOfTypeAll<AnimationClip>();
                foreach (var clip in clips)
                {
                    if (clip.name.Contains(animData.animationName))
                    {
                        state.animation = clip;
                        break;
                    }
                }
            }
            
            // Sync frame data if not already set
            if (state.startupFrames == 0 && state.activeFrames == 0 && state.recoveryFrames == 0)
            {
                var totalFrames = animData.totalFrames;
                
                // Analyze animation to determine phases
                var (startup, active, recovery) = AnalyzeAnimationPhases(animData);
                
                state.startupFrames = startup;
                state.activeFrames = active;
                state.recoveryFrames = recovery;
            }
        }
        
        public static (int startup, int active, int recovery) AnalyzeAnimationPhases(AnimationData animData)
        {
            if (animData.frames.Count == 0)
                return (0, 0, 0);
            
            int firstHitboxFrame = -1;
            int lastHitboxFrame = -1;
            
            // Find first and last frames with hitboxes
            for (int i = 0; i < animData.frames.Count; i++)
            {
                var frame = animData.frames[i];
                bool hasHitboxes = frame.hitboxes != null && frame.hitboxes.Count > 0;
                
                if (hasHitboxes)
                {
                    if (firstHitboxFrame == -1)
                        firstHitboxFrame = i;
                    lastHitboxFrame = i;
                }
            }
            
            // Calculate phases
            int startup = firstHitboxFrame > 0 ? firstHitboxFrame : animData.totalFrames / 3;
            int active = lastHitboxFrame >= firstHitboxFrame ? (lastHitboxFrame - firstHitboxFrame + 1) : animData.totalFrames / 3;
            int recovery = animData.totalFrames - startup - active;
            
            return (startup, active, recovery);
        }
        
        public static void ConvertLegacyHitboxesToFrameData(AttackState attackState, AnimationData animData)
        {
            if (attackState?.hitboxes == null || animData?.frames == null)
                return;
            
            foreach (var legacyHitbox in attackState.hitboxes)
            {
                // Apply hitbox to relevant frames
                for (int frame = legacyHitbox.startFrame; frame <= legacyHitbox.endFrame && frame < animData.frames.Count; frame++)
                {
                    var frameData = animData.frames[frame];
                    
                    if (frameData.hitboxes == null)
                        frameData.hitboxes = new System.Collections.Generic.List<Rectangle>();
                    
                    var rect = ConvertHitboxDataToRectangle(legacyHitbox);
                    frameData.hitboxes.Add(rect);
                    
                    animData.frames[frame] = frameData;
                }
            }
        }
        
        public static void CreateDefaultAnimationForState(CharacterState state, string animationName, int frameCount = 30)
        {
            var animData = ScriptableObject.CreateInstance<AnimationData>();
            animData.animationName = animationName;
            animData.frameRate = 60f;
            
            // Create frames
            for (int i = 0; i < frameCount; i++)
            {
                var frameData = new FrameData(i);
                
                // Add default hurtbox
                frameData.hurtboxes = new System.Collections.Generic.List<Rectangle>
                {
                    Rectangle.CreateHurtbox(Vector2.zero, new Vector2(1f, 2f))
                };
                
                // Add hitboxes during active frames (if it's an attack state)
                if (state is AttackState && i >= state.startupFrames && i < state.startupFrames + state.activeFrames)
                {
                    frameData.hitboxes = new System.Collections.Generic.List<Rectangle>
                    {
                        Rectangle.CreateHitbox(Vector2.right, Vector2.one, 10)
                    };
                }
                
                animData.AddFrame(frameData);
            }
            
            // If this is an animated state, assign the animation data
            if (state is AnimatedCharacterState animatedState)
            {
                animatedState.frameAnimation = animData;
            }
        }
        
        public static void ValidateAnimationIntegration(CharacterState state)
        {
            if (state is AnimatedCharacterState animatedState)
            {
                if (animatedState.useFrameBasedAnimation && animatedState.frameAnimation == null)
                {
                    Debug.LogWarning($"State '{state.stateName}' is set to use frame-based animation but has no AnimationData assigned!");
                }
                
                if (animatedState.frameAnimation != null)
                {
                    var animData = animatedState.frameAnimation;
                    
                    // Validate frame data
                    bool hasActiveFrames = false;
                    foreach (var frame in animData.frames)
                    {
                        if (frame.hitboxes != null && frame.hitboxes.Count > 0)
                        {
                            hasActiveFrames = true;
                            break;
                        }
                    }
                    
                    if (!hasActiveFrames && state is AttackState)
                    {
                        Debug.LogWarning($"Attack state '{state.stateName}' has no frames with hitboxes!");
                    }
                }
            }
        }
        
        public static void LogAnimationSystemStatus(GameObject character)
        {
            var frameAnimator = character.GetComponent<FrameBasedAnimator>();
            var hitboxManager = character.GetComponent<HitboxManager>();
            var stateMachine = character.GetComponent<CharacterStateMachine>();
            var animator = character.GetComponent<Animator>();
            
            Debug.Log($"Animation System Status for {character.name}:");
            Debug.Log($"  - FrameBasedAnimator: {(frameAnimator != null ? "✓" : "✗")}");
            Debug.Log($"  - HitboxManager: {(hitboxManager != null ? "✓" : "✗")}");
            Debug.Log($"  - StateMachine: {(stateMachine != null ? "✓" : "✗")}");
            Debug.Log($"  - Legacy Animator: {(animator != null ? "✓" : "✗")}");
            
            if (frameAnimator != null)
            {
                Debug.Log($"  - Current Animation: {frameAnimator.CurrentAnimation?.animationName ?? "None"}");
                Debug.Log($"  - Is Playing: {frameAnimator.IsPlaying}");
                Debug.Log($"  - Current Frame: {frameAnimator.CurrentFrame}");
            }
            
            if (hitboxManager != null)
            {
                Debug.Log($"  - Active Hitboxes: {hitboxManager.ActiveHitboxes.Count}");
                Debug.Log($"  - Active Hurtboxes: {hitboxManager.ActiveHurtboxes.Count}");
            }
        }
    }
}