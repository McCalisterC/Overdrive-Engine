using System.Collections.Generic;
using UnityEngine;

namespace FightingFramework.Animation
{
    [CreateAssetMenu(fileName = "New Animation Data", menuName = "Fighting Framework/Animation/Animation Data")]
    public class AnimationData : ScriptableObject
    {
        [Header("Animation Properties")]
        public string animationName;
        public List<FrameData> frames = new List<FrameData>();
        public bool looping = false;
        public int loopStartFrame = 0;
        
        [Header("Timing")]
        [Range(1f, 120f)]
        public float frameRate = 60f;
        public bool useFixedFrameRate = true;
        
        [Header("Animation Events")]
        public List<FrameEvent> globalEvents = new List<FrameEvent>();
        
        [Header("Metadata")]
        [TextArea(3, 5)]
        public string description;
        public Sprite previewSprite;
        
        public int totalFrames => frames.Count;
        public float duration => totalFrames / frameRate;
        
        private void OnValidate()
        {
            // Ensure loop start frame is valid
            loopStartFrame = Mathf.Clamp(loopStartFrame, 0, Mathf.Max(0, totalFrames - 1));
            
            // Update frame numbers
            for (int i = 0; i < frames.Count; i++)
            {
                var frame = frames[i];
                frame.frameNumber = i;
                frames[i] = frame;
            }
        }
        
        public FrameData GetFrame(int frameIndex)
        {
            if (frames.Count == 0)
            {
                return new FrameData(frameIndex);
            }
            
            if (frameIndex < 0)
            {
                return frames[0];
            }
            
            if (frameIndex >= frames.Count)
            {
                if (looping && frames.Count > loopStartFrame)
                {
                    int loopLength = frames.Count - loopStartFrame;
                    int loopIndex = (frameIndex - loopStartFrame) % loopLength;
                    return frames[loopStartFrame + loopIndex];
                }
                else
                {
                    return frames[frames.Count - 1];
                }
            }
            
            return frames[frameIndex];
        }
        
        public FrameData GetFrameAtTime(float time)
        {
            int frameIndex = Mathf.FloorToInt(time * frameRate);
            return GetFrame(frameIndex);
        }
        
        public bool IsValidFrameIndex(int frameIndex)
        {
            return frameIndex >= 0 && (frameIndex < frames.Count || looping);
        }
        
        public void AddFrame(FrameData frameData)
        {
            frameData.frameNumber = frames.Count;
            frames.Add(frameData);
        }
        
        public void InsertFrame(int index, FrameData frameData)
        {
            if (index < 0) index = 0;
            if (index > frames.Count) index = frames.Count;
            
            frameData.frameNumber = index;
            frames.Insert(index, frameData);
            
            // Update frame numbers for subsequent frames
            for (int i = index + 1; i < frames.Count; i++)
            {
                var frame = frames[i];
                frame.frameNumber = i;
                frames[i] = frame;
            }
        }
        
        public void RemoveFrame(int index)
        {
            if (index >= 0 && index < frames.Count)
            {
                frames.RemoveAt(index);
                
                // Update frame numbers for subsequent frames
                for (int i = index; i < frames.Count; i++)
                {
                    var frame = frames[i];
                    frame.frameNumber = i;
                    frames[i] = frame;
                }
            }
        }
        
        public void DuplicateFrame(int sourceIndex)
        {
            if (sourceIndex >= 0 && sourceIndex < frames.Count)
            {
                var sourceFrame = frames[sourceIndex];
                var duplicatedFrame = sourceFrame;
                InsertFrame(sourceIndex + 1, duplicatedFrame);
            }
        }
        
        public List<FrameEvent> GetEventsForFrame(int frameIndex)
        {
            var events = new List<FrameEvent>();
            
            if (frameIndex >= 0 && frameIndex < frames.Count)
            {
                var frame = frames[frameIndex];
                if (frame.events != null)
                {
                    events.AddRange(frame.events);
                }
            }
            
            // Add global events that should trigger on this frame
            foreach (var globalEvent in globalEvents)
            {
                if (globalEvent.intParameter == frameIndex)
                {
                    events.Add(globalEvent);
                }
            }
            
            return events;
        }
        
        public List<Rectangle> GetHitboxesForFrame(int frameIndex)
        {
            if (frameIndex >= 0 && frameIndex < frames.Count)
            {
                return frames[frameIndex].hitboxes ?? new List<Rectangle>();
            }
            return new List<Rectangle>();
        }
        
        public List<Rectangle> GetHurtboxesForFrame(int frameIndex)
        {
            if (frameIndex >= 0 && frameIndex < frames.Count)
            {
                return frames[frameIndex].hurtboxes ?? new List<Rectangle>();
            }
            return new List<Rectangle>();
        }
        
        public Vector2 GetRootMotionForFrame(int frameIndex)
        {
            if (frameIndex >= 0 && frameIndex < frames.Count)
            {
                return frames[frameIndex].rootMotion;
            }
            return Vector2.zero;
        }
        
        public Vector2 GetTotalRootMotion()
        {
            Vector2 totalMotion = Vector2.zero;
            foreach (var frame in frames)
            {
                totalMotion += frame.rootMotion;
            }
            return totalMotion;
        }
        
        public int GetLoopFrame(int frameIndex)
        {
            if (!looping || frames.Count <= loopStartFrame)
            {
                return frameIndex;
            }
            
            if (frameIndex < loopStartFrame)
            {
                return frameIndex;
            }
            
            int loopLength = frames.Count - loopStartFrame;
            if (loopLength <= 0) return loopStartFrame;
            
            return loopStartFrame + ((frameIndex - loopStartFrame) % loopLength);
        }
        
        [ContextMenu("Generate Test Frames")]
        public void GenerateTestFrames()
        {
            frames.Clear();
            
            for (int i = 0; i < 10; i++)
            {
                var frameData = new FrameData(i);
                
                // Add some test hitboxes for even frames
                if (i % 2 == 0 && i > 2 && i < 8)
                {
                    frameData.hitboxes = new List<Rectangle>
                    {
                        Rectangle.CreateHitbox(Vector2.right * (i * 0.5f), Vector2.one)
                    };
                }
                
                // Add hurtbox for all frames
                frameData.hurtboxes = new List<Rectangle>
                {
                    Rectangle.CreateHurtbox(Vector2.zero, new Vector2(1f, 2f))
                };
                
                frames.Add(frameData);
            }
        }
    }
}