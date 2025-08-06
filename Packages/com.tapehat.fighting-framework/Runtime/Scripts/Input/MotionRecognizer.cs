using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace FightingFramework.Input
{
    public static class MotionRecognizer
    {
        public static bool CheckSequence(List<InputCommand> inputHistory, MotionInput motion)
        {
            if (motion == null || !motion.IsValid() || inputHistory == null || inputHistory.Count == 0)
                return false;
            
            // Simple recognition for directions and buttons separately
            bool directionsMatch = CheckDirectionSequence(inputHistory, motion.directions, motion);
            bool buttonsMatch = CheckButtonSequence(inputHistory, motion.buttons, motion);
            
            // Both must match if both are specified
            if (motion.directions.Count > 0 && motion.buttons.Count > 0)
            {
                return directionsMatch && buttonsMatch;
            }
            
            // If only one type is specified, that must match
            if (motion.directions.Count > 0)
                return directionsMatch;
                
            if (motion.buttons.Count > 0)
                return buttonsMatch;
                
            return false;
        }
        
        private static bool CheckDirectionSequence(List<InputCommand> inputHistory, List<InputDirection> requiredDirections, MotionInput motion)
        {
            if (requiredDirections.Count == 0) return true;
            
            // Get only direction inputs from history
            var directionInputs = inputHistory.Where(cmd => cmd.type == InputType.Direction).ToList();
            
            if (directionInputs.Count == 0) return false;
            
            return motion.allowDirtyInputs ? 
                CheckSequenceWithDirtyInputs(directionInputs, requiredDirections, motion) :
                CheckExactSequence(directionInputs, requiredDirections, motion);
        }
        
        private static bool CheckButtonSequence(List<InputCommand> inputHistory, List<InputButton> requiredButtons, MotionInput motion)
        {
            if (requiredButtons.Count == 0) return true;
            
            // Get only button press inputs from history
            var buttonInputs = inputHistory.Where(cmd => cmd.type == InputType.Button && cmd.isPressed).ToList();
            
            if (buttonInputs.Count == 0) return false;
            
            return motion.allowDirtyInputs ?
                CheckSequenceWithDirtyInputs(buttonInputs, requiredButtons, motion) :
                CheckExactSequence(buttonInputs, requiredButtons, motion);
        }
        
        private static bool CheckSequenceWithDirtyInputs<T>(List<InputCommand> inputs, List<T> required, MotionInput motion)
        {
            if (required.Count == 0) return true;
            
            int requiredIndex = 0;
            int lastMatchFrame = -1;
            
            for (int i = 0; i < inputs.Count && requiredIndex < required.Count; i++)
            {
                var input = inputs[i];
                
                // Check cooldown
                if (lastMatchFrame >= 0 && input.frame - lastMatchFrame < motion.inputCooldown)
                    continue;
                
                bool matches = false;
                if (typeof(T) == typeof(InputDirection))
                {
                    matches = input.inputDirection.Equals(required[requiredIndex]);
                }
                else if (typeof(T) == typeof(InputButton))
                {
                    matches = input.button.Equals(required[requiredIndex]);
                }
                
                if (matches)
                {
                    requiredIndex++;
                    lastMatchFrame = input.frame;
                    
                    // Check if we're within the time/frame window
                    if (requiredIndex == 1) // First match
                    {
                        int framesSinceStart = inputs[inputs.Count - 1].frame - input.frame;
                        float timeSinceStart = inputs[inputs.Count - 1].timestamp - input.timestamp;
                        
                        if (framesSinceStart > motion.maxFrameWindow || timeSinceStart > motion.maxTimeWindow)
                            return false;
                    }
                }
            }
            
            return requiredIndex == required.Count;
        }
        
        private static bool CheckExactSequence<T>(List<InputCommand> inputs, List<T> required, MotionInput motion)
        {
            if (inputs.Count < required.Count) return false;
            
            // Check the most recent inputs match the required sequence exactly
            int startIndex = inputs.Count - required.Count;
            
            for (int i = 0; i < required.Count; i++)
            {
                var input = inputs[startIndex + i];
                bool matches = false;
                
                if (typeof(T) == typeof(InputDirection))
                {
                    matches = input.inputDirection.Equals(required[i]);
                }
                else if (typeof(T) == typeof(InputButton))
                {
                    matches = input.button.Equals(required[i]);
                }
                
                if (!matches) return false;
                
                // Check timing constraints
                if (i > 0)
                {
                    var prevInput = inputs[startIndex + i - 1];
                    int frameDiff = input.frame - prevInput.frame;
                    float timeDiff = input.timestamp - prevInput.timestamp;
                    
                    if (frameDiff > motion.maxFrameWindow || timeDiff > motion.maxTimeWindow)
                        return false;
                        
                    if (frameDiff < motion.inputCooldown)
                        return false;
                }
            }
            
            return true;
        }
        
        public static List<MotionInput> CheckMultipleMotions(List<InputCommand> inputHistory, List<MotionInput> motions)
        {
            var matches = new List<MotionInput>();
            
            foreach (var motion in motions)
            {
                if (CheckSequence(inputHistory, motion))
                {
                    matches.Add(motion);
                }
            }
            
            // Sort by priority (highest first)
            matches.Sort((a, b) => b.priority.CompareTo(a.priority));
            
            return matches;
        }
        
        public static MotionInput GetBestMotionMatch(List<InputCommand> inputHistory, List<MotionInput> motions)
        {
            var matches = CheckMultipleMotions(inputHistory, motions);
            return matches.Count > 0 ? matches[0] : null;
        }
        
        public static bool CheckAdvancedSequence(List<InputCommand> inputHistory, MotionInputSequence sequence)
        {
            if (sequence.IsEmpty || inputHistory == null || inputHistory.Count == 0)
                return false;
            
            int sequenceIndex = 0;
            float sequenceStartTime = -1f;
            
            for (int i = 0; i < inputHistory.Count && sequenceIndex < sequence.sequence.Count; i++)
            {
                var input = inputHistory[i];
                var requiredElement = sequence.sequence[sequenceIndex];
                
                bool matches = false;
                
                // Check if input matches required element
                if (requiredElement.type == InputType.Direction && input.type == InputType.Direction)
                {
                    matches = input.inputDirection == requiredElement.direction;
                }
                else if (requiredElement.type == InputType.Button && input.type == InputType.Button)
                {
                    matches = input.button == requiredElement.button && input.isPressed;
                }
                
                if (matches)
                {
                    if (sequenceStartTime < 0)
                        sequenceStartTime = input.timestamp;
                        
                    sequenceIndex++;
                }
                else if (!requiredElement.isOptional)
                {
                    // Required input not found in sequence, reset if we were in the middle of matching
                    if (sequenceIndex > 0)
                    {
                        sequenceIndex = 0;
                        sequenceStartTime = -1f;
                    }
                }
                
                // Check sequence timeout
                if (sequenceStartTime >= 0 && input.timestamp - sequenceStartTime > sequence.maxSequenceTime)
                {
                    if (!sequence.allowPartialMatch)
                    {
                        sequenceIndex = 0;
                        sequenceStartTime = -1f;
                    }
                    else
                    {
                        break; // Allow partial match
                    }
                }
            }
            
            return sequenceIndex == sequence.sequence.Count || 
                   (sequence.allowPartialMatch && sequenceIndex > 0);
        }
        
        // Utility methods for common motion patterns
        public static MotionInput CreateQuarterCircleForward(InputButton button, string name = "Quarter Circle Forward")
        {
            var motion = ScriptableObject.CreateInstance<MotionInput>();
            motion.motionName = name;
            motion.directions = new List<InputDirection> { InputDirection.Down, InputDirection.DownRight, InputDirection.Right };
            motion.buttons = new List<InputButton> { button };
            motion.maxFrameWindow = 15;
            motion.allowDirtyInputs = true;
            return motion;
        }
        
        public static MotionInput CreateQuarterCircleBack(InputButton button, string name = "Quarter Circle Back")
        {
            var motion = ScriptableObject.CreateInstance<MotionInput>();
            motion.motionName = name;
            motion.directions = new List<InputDirection> { InputDirection.Down, InputDirection.DownLeft, InputDirection.Left };
            motion.buttons = new List<InputButton> { button };
            motion.maxFrameWindow = 15;
            motion.allowDirtyInputs = true;
            return motion;
        }
        
        public static MotionInput CreateDragonPunch(InputButton button, string name = "Dragon Punch")
        {
            var motion = ScriptableObject.CreateInstance<MotionInput>();
            motion.motionName = name;
            motion.directions = new List<InputDirection> { InputDirection.Right, InputDirection.Down, InputDirection.DownRight };
            motion.buttons = new List<InputButton> { button };
            motion.maxFrameWindow = 12;
            motion.allowDirtyInputs = false;
            return motion;
        }
    }
}