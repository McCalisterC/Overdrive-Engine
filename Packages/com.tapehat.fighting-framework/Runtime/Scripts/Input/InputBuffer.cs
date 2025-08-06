using System.Collections.Generic;
using UnityEngine;
using FightingFramework.Utilities;

namespace FightingFramework.Input
{
    public class InputBuffer : MonoBehaviour
    {
        [Header("Buffer Settings")]
        [SerializeField] private int bufferSize = 60; // 1 second at 60 FPS
        [SerializeField] private bool debugMode = false;
        
        private CircularBuffer<InputCommand> buffer;
        private int lastFrameProcessed = -1;
        
        public int BufferSize => bufferSize;
        public int CurrentInputCount => buffer?.Count ?? 0;
        
        private void Awake()
        {
            buffer = new CircularBuffer<InputCommand>(bufferSize);
        }
        
        private void OnValidate()
        {
            if (bufferSize <= 0)
                bufferSize = 60;
                
            if (Application.isPlaying && buffer != null && buffer.Capacity != bufferSize)
            {
                // Recreate buffer with new size
                var oldInputs = buffer.GetAllItems();
                buffer = new CircularBuffer<InputCommand>(bufferSize);
                
                // Add back as many inputs as possible
                int startIndex = Mathf.Max(0, oldInputs.Count - bufferSize);
                for (int i = startIndex; i < oldInputs.Count; i++)
                {
                    buffer.Add(oldInputs[i]);
                }
            }
        }
        
        public void AddInput(InputCommand command)
        {
            // Update timestamp and frame if not set
            if (command.timestamp <= 0)
                command.timestamp = Time.time;
            if (command.frame <= 0)
                command.frame = Time.frameCount;
                
            buffer.Add(command);
            
            if (debugMode)
            {
                Debug.Log($"Input Added: {command}");
            }
        }
        
        public void AddDirectionInput(InputDirection direction, Vector2 directionVector)
        {
            var command = InputCommand.CreateDirectionInput(direction, directionVector);
            AddInput(command);
        }
        
        public void AddButtonInput(InputButton button, bool pressed = true)
        {
            var command = InputCommand.CreateButtonInput(button, pressed);
            AddInput(command);
        }
        
        public void AddNeutralInput()
        {
            var command = InputCommand.CreateNeutralInput();
            AddInput(command);
        }
        
        public bool CheckMotion(MotionInput motion, int frameWindow = -1)
        {
            if (motion == null) return false;
            
            int window = frameWindow > 0 ? frameWindow : motion.maxFrameWindow;
            var recentInputs = GetRecentInputs(window);
            
            return MotionRecognizer.CheckSequence(recentInputs, motion);
        }
        
        public List<InputCommand> GetRecentInputs(int frameWindow)
        {
            if (buffer.IsEmpty) return new List<InputCommand>();
            
            var allInputs = buffer.GetAllItems();
            var result = new List<InputCommand>();
            int currentFrame = Time.frameCount;
            
            // Get inputs within the frame window
            for (int i = allInputs.Count - 1; i >= 0; i--)
            {
                var input = allInputs[i];
                if (currentFrame - input.frame <= frameWindow)
                {
                    result.Insert(0, input); // Insert at beginning to maintain chronological order
                }
                else
                {
                    break; // Inputs are too old
                }
            }
            
            return result;
        }
        
        public List<InputCommand> GetInputsInTimeWindow(float timeWindow)
        {
            if (buffer.IsEmpty) return new List<InputCommand>();
            
            var allInputs = buffer.GetAllItems();
            var result = new List<InputCommand>();
            float currentTime = Time.time;
            
            for (int i = allInputs.Count - 1; i >= 0; i--)
            {
                var input = allInputs[i];
                if (currentTime - input.timestamp <= timeWindow)
                {
                    result.Insert(0, input);
                }
                else
                {
                    break;
                }
            }
            
            return result;
        }
        
        public InputCommand? GetMostRecentInput()
        {
            return buffer.IsEmpty ? null : buffer.GetMostRecent();
        }
        
        public InputCommand? GetMostRecentInputOfType(InputType inputType)
        {
            var recentInputs = buffer.GetRecentItems(bufferSize);
            
            for (int i = 0; i < recentInputs.Count; i++)
            {
                if (recentInputs[i].type == inputType)
                {
                    return recentInputs[i];
                }
            }
            
            return null;
        }
        
        public bool HasInputInFrameWindow(InputType inputType, int frameWindow)
        {
            var recentInputs = GetRecentInputs(frameWindow);
            
            foreach (var input in recentInputs)
            {
                if (input.type == inputType)
                    return true;
            }
            
            return false;
        }
        
        public bool HasButtonInFrameWindow(InputButton button, int frameWindow, bool mustBePressed = true)
        {
            var recentInputs = GetRecentInputs(frameWindow);
            
            foreach (var input in recentInputs)
            {
                if (input.button == button && (!mustBePressed || input.isPressed))
                    return true;
            }
            
            return false;
        }
        
        public void ClearBuffer()
        {
            buffer.Clear();
            
            if (debugMode)
            {
                Debug.Log("Input buffer cleared");
            }
        }
        
        public void ResizeBuffer(int newSize)
        {
            if (newSize <= 0) return;
            
            bufferSize = newSize;
            
            if (Application.isPlaying)
            {
                OnValidate(); // This will recreate the buffer
            }
        }
        
        private void OnGUI()
        {
            if (!debugMode) return;
            
            GUILayout.BeginArea(new Rect(10, 10, 300, 200));
            GUILayout.Label($"Input Buffer ({CurrentInputCount}/{BufferSize})");
            
            var recentInputs = buffer.GetRecentItems(10);
            foreach (var input in recentInputs)
            {
                GUILayout.Label(input.ToString());
            }
            
            GUILayout.EndArea();
        }
    }
}