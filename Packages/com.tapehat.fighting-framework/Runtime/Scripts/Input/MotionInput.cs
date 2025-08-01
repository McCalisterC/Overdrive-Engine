using System.Collections.Generic;
using UnityEngine;

namespace FightingFramework.Input
{
    [CreateAssetMenu(fileName = "Motion Input", menuName = "Fighting Framework/Input/Motion")]
    public class MotionInput : ScriptableObject
    {
        [Header("Motion Configuration")]
        public string motionName = "New Motion";
        [TextArea(2, 4)]
        public string description = "";
        
        [Header("Input Sequence")]
        public List<InputDirection> directions = new List<InputDirection>();
        public List<InputButton> buttons = new List<InputButton>();
        
        [Header("Timing Settings")]
        [Range(5, 60)]
        public int maxFrameWindow = 15;
        [Range(0.1f, 2f)]
        public float maxTimeWindow = 0.25f;
        
        [Header("Recognition Settings")]
        [Tooltip("Allow extra inputs between required inputs")]
        public bool allowDirtyInputs = true;
        [Tooltip("Require exact input order")]
        public bool requireExactOrder = false;
        [Tooltip("Allow input overlap (button held while direction input)")]
        public bool allowInputOverlap = true;
        
        [Header("Advanced Settings")]
        [Tooltip("Minimum frames between similar inputs to avoid spam")]
        [Range(0, 10)]
        public int inputCooldown = 2;
        [Tooltip("Priority when multiple motions match (higher = more priority)")]
        [Range(0, 100)]
        public int priority = 50;
        
        private void OnValidate()
        {
            if (string.IsNullOrEmpty(motionName))
                motionName = name;
                
            if (maxFrameWindow <= 0)
                maxFrameWindow = 15;
                
            if (maxTimeWindow <= 0)
                maxTimeWindow = 0.25f;
        }
        
        public bool IsValid()
        {
            return directions.Count > 0 || buttons.Count > 0;
        }
        
        public int GetTotalInputCount()
        {
            return directions.Count + buttons.Count;
        }
        
        public List<object> GetCombinedSequence()
        {
            var combined = new List<object>();
            
            // Add directions first, then buttons (or interleave based on requireExactOrder)
            if (requireExactOrder)
            {
                // For exact order, we need a more sophisticated approach
                // This is a simplified version - could be expanded for complex sequences
                foreach (var dir in directions)
                    combined.Add(dir);
                foreach (var btn in buttons)
                    combined.Add(btn);
            }
            else
            {
                foreach (var dir in directions)
                    combined.Add(dir);
                foreach (var btn in buttons)
                    combined.Add(btn);
            }
            
            return combined;
        }
        
        public override string ToString()
        {
            var result = $"{motionName}: ";
            
            if (directions.Count > 0)
            {
                result += "Dirs[";
                for (int i = 0; i < directions.Count; i++)
                {
                    result += directions[i];
                    if (i < directions.Count - 1) result += ",";
                }
                result += "] ";
            }
            
            if (buttons.Count > 0)
            {
                result += "Btns[";
                for (int i = 0; i < buttons.Count; i++)
                {
                    result += buttons[i];
                    if (i < buttons.Count - 1) result += ",";
                }
                result += "]";
            }
            
            return result;
        }
    }
    
    [System.Serializable]
    public class MotionInputSequence
    {
        [System.Serializable]
        public struct InputElement
        {
            public InputType type;
            public InputDirection direction;
            public InputButton button;
            public bool isOptional;
            public float timingTolerance;
            
            public InputElement(InputDirection dir, bool optional = false, float tolerance = 0f)
            {
                type = InputType.Direction;
                direction = dir;
                button = InputButton.None;
                isOptional = optional;
                timingTolerance = tolerance;
            }
            
            public InputElement(InputButton btn, bool optional = false, float tolerance = 0f)
            {
                type = InputType.Button;
                direction = InputDirection.Neutral;
                button = btn;
                isOptional = optional;
                timingTolerance = tolerance;
            }
        }
        
        public List<InputElement> sequence = new List<InputElement>();
        public bool allowPartialMatch = false;
        public float maxSequenceTime = 0.5f;
        
        public bool IsEmpty => sequence.Count == 0;
        
        public void AddDirection(InputDirection direction, bool optional = false)
        {
            sequence.Add(new InputElement(direction, optional));
        }
        
        public void AddButton(InputButton button, bool optional = false)
        {
            sequence.Add(new InputElement(button, optional));
        }
        
        public void Clear()
        {
            sequence.Clear();
        }
    }
}