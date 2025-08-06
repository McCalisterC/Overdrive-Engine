using System.Collections.Generic;
using UnityEngine;
using System;

namespace FightingFramework.Input
{
    [System.Serializable]
    public class KeyMapping
    {
        public VirtualButton virtualButton;
        public KeyCode primaryKey;
        public KeyCode secondaryKey = KeyCode.None;
        public string controllerButton = "";
        public int joystickAxis = -1;
        public bool isAxisPositive = true;
        
        public bool IsPressed()
        {
            bool keyPressed = UnityEngine.Input.GetKey(primaryKey) || 
                             (secondaryKey != KeyCode.None && UnityEngine.Input.GetKey(secondaryKey));
            
            bool buttonPressed = !string.IsNullOrEmpty(controllerButton) && 
                                UnityEngine.Input.GetButton(controllerButton);
            
            bool axisPressed = false;
            if (joystickAxis >= 0)
            {
                float axisValue = UnityEngine.Input.GetAxis($"Joy{joystickAxis}");
                axisPressed = isAxisPositive ? axisValue > 0.5f : axisValue < -0.5f;
            }
            
            return keyPressed || buttonPressed || axisPressed;
        }
        
        public bool IsPressedDown()
        {
            bool keyDown = UnityEngine.Input.GetKeyDown(primaryKey) || 
                          (secondaryKey != KeyCode.None && UnityEngine.Input.GetKeyDown(secondaryKey));
            
            bool buttonDown = !string.IsNullOrEmpty(controllerButton) && 
                             UnityEngine.Input.GetButtonDown(controllerButton);
            
            return keyDown || buttonDown;
        }
        
        public bool IsReleased()
        {
            bool keyUp = UnityEngine.Input.GetKeyUp(primaryKey) || 
                        (secondaryKey != KeyCode.None && UnityEngine.Input.GetKeyUp(secondaryKey));
            
            bool buttonUp = !string.IsNullOrEmpty(controllerButton) && 
                           UnityEngine.Input.GetButtonUp(controllerButton);
            
            return keyUp || buttonUp;
        }
    }
    
    public class InputMapper : MonoBehaviour
    {
        [Header("Input Mappings")]
        [SerializeField] private List<KeyMapping> keyMappings = new List<KeyMapping>();
        
        [Header("Movement Settings")]
        [SerializeField] private float deadZone = 0.2f;
        [SerializeField] private bool useRawInput = true;
        [SerializeField] private string horizontalAxis = "Horizontal";
        [SerializeField] private string verticalAxis = "Vertical";
        
        [Header("Controller Settings")]
        [SerializeField] private int playerNumber = 1;
        [SerializeField] private bool enableControllerInput = true;
        
        [Header("Debug")]
        [SerializeField] private bool debugMode = false;
        
        private Dictionary<VirtualButton, KeyMapping> mappingLookup;
        private Dictionary<VirtualButton, bool> currentButtonStates;
        private Dictionary<VirtualButton, bool> previousButtonStates;
        private Vector2 currentMovement;
        private Vector2 previousMovement;
        
        public event Action<VirtualButton> OnButtonPressed;
        public event Action<VirtualButton> OnButtonReleased;
        public event Action<Vector2> OnMovementChanged;
        
        private void Awake()
        {
            InitializeMappings();
            InitializeDefaultMappings();
        }
        
        private void InitializeMappings()
        {
            mappingLookup = new Dictionary<VirtualButton, KeyMapping>();
            currentButtonStates = new Dictionary<VirtualButton, bool>();
            previousButtonStates = new Dictionary<VirtualButton, bool>();
            
            foreach (var mapping in keyMappings)
            {
                mappingLookup[mapping.virtualButton] = mapping;
                currentButtonStates[mapping.virtualButton] = false;
                previousButtonStates[mapping.virtualButton] = false;
            }
        }
        
        private void InitializeDefaultMappings()
        {
            if (keyMappings.Count == 0)
            {
                // Default keyboard mappings
                AddMapping(VirtualButton.Light, KeyCode.J, KeyCode.None, "Fire1");
                AddMapping(VirtualButton.Medium, KeyCode.K, KeyCode.None, "Fire2");
                AddMapping(VirtualButton.Heavy, KeyCode.L, KeyCode.None, "Fire3");
                AddMapping(VirtualButton.Special, KeyCode.I, KeyCode.None, "Jump");
                
                AddMapping(VirtualButton.Up, KeyCode.W, KeyCode.UpArrow);
                AddMapping(VirtualButton.Down, KeyCode.S, KeyCode.DownArrow);
                AddMapping(VirtualButton.Left, KeyCode.A, KeyCode.LeftArrow);
                AddMapping(VirtualButton.Right, KeyCode.D, KeyCode.RightArrow);
                
                AddMapping(VirtualButton.Block, KeyCode.LeftShift, KeyCode.RightShift);
                AddMapping(VirtualButton.Grab, KeyCode.Space);
                AddMapping(VirtualButton.Super, KeyCode.Q);
                AddMapping(VirtualButton.Dash, KeyCode.E);
                
                InitializeMappings();
            }
        }
        
        private void AddMapping(VirtualButton button, KeyCode primary, KeyCode secondary = KeyCode.None, string controllerButton = "")
        {
            keyMappings.Add(new KeyMapping
            {
                virtualButton = button,
                primaryKey = primary,
                secondaryKey = secondary,
                controllerButton = controllerButton
            });
        }
        
        private void Update()
        {
            UpdateButtonStates();
            UpdateMovement();
            
            if (debugMode)
            {
                DebugInputs();
            }
        }
        
        private void UpdateButtonStates()
        {
            foreach (var kvp in mappingLookup)
            {
                var button = kvp.Key;
                var mapping = kvp.Value;
                
                previousButtonStates[button] = currentButtonStates[button];
                currentButtonStates[button] = mapping.IsPressed();
                
                // Fire events
                if (GetButtonDown(button))
                {
                    OnButtonPressed?.Invoke(button);
                }
                else if (GetButtonUp(button))
                {
                    OnButtonReleased?.Invoke(button);
                }
            }
        }
        
        private void UpdateMovement()
        {
            previousMovement = currentMovement;
            
            float horizontal = 0f;
            float vertical = 0f;
            
            // Check directional buttons first
            if (GetButton(VirtualButton.Right)) horizontal += 1f;
            if (GetButton(VirtualButton.Left)) horizontal -= 1f;
            if (GetButton(VirtualButton.Up)) vertical += 1f;
            if (GetButton(VirtualButton.Down)) vertical -= 1f;
            
            // If no directional buttons, use axis input
            if (Mathf.Approximately(horizontal, 0f) && Mathf.Approximately(vertical, 0f))
            {
                horizontal = useRawInput ? UnityEngine.Input.GetAxisRaw(horizontalAxis) : UnityEngine.Input.GetAxis(horizontalAxis);
                vertical = useRawInput ? UnityEngine.Input.GetAxisRaw(verticalAxis) : UnityEngine.Input.GetAxis(verticalAxis);
            }
            
            // Apply deadzone
            var movement = new Vector2(horizontal, vertical);
            if (movement.magnitude < deadZone)
            {
                movement = Vector2.zero;
            }
            else
            {
                // Normalize to remove deadzone
                movement = movement.normalized * ((movement.magnitude - deadZone) / (1f - deadZone));
            }
            
            currentMovement = movement;
            
            // Fire movement event if changed
            if (Vector2.Distance(currentMovement, previousMovement) > 0.01f)
            {
                OnMovementChanged?.Invoke(currentMovement);
            }
        }
        
        public bool GetButton(VirtualButton button)
        {
            return currentButtonStates.TryGetValue(button, out bool state) && state;
        }
        
        public bool GetButtonDown(VirtualButton button)
        {
            return GetButton(button) && !GetPreviousButtonState(button);
        }
        
        public bool GetButtonUp(VirtualButton button)
        {
            return !GetButton(button) && GetPreviousButtonState(button);
        }
        
        private bool GetPreviousButtonState(VirtualButton button)
        {
            return previousButtonStates.TryGetValue(button, out bool state) && state;
        }
        
        public Vector2 GetMovementInput()
        {
            return currentMovement;
        }
        
        public InputDirection GetInputDirection()
        {
            var movement = GetMovementInput();
            
            if (movement.magnitude < deadZone)
                return InputDirection.Neutral;
            
            // Convert to 8-way direction
            float angle = Mathf.Atan2(movement.y, movement.x) * Mathf.Rad2Deg;
            if (angle < 0) angle += 360;
            
            // Map angle to direction (45-degree segments)
            if (angle >= 337.5f || angle < 22.5f) return InputDirection.Right;
            if (angle >= 22.5f && angle < 67.5f) return InputDirection.UpRight;
            if (angle >= 67.5f && angle < 112.5f) return InputDirection.Up;
            if (angle >= 112.5f && angle < 157.5f) return InputDirection.UpLeft;
            if (angle >= 157.5f && angle < 202.5f) return InputDirection.Left;
            if (angle >= 202.5f && angle < 247.5f) return InputDirection.DownLeft;
            if (angle >= 247.5f && angle < 292.5f) return InputDirection.Down;
            if (angle >= 292.5f && angle < 337.5f) return InputDirection.DownRight;
            
            return InputDirection.Neutral;
        }
        
        public InputButton VirtualButtonToInputButton(VirtualButton vButton)
        {
            return vButton switch
            {
                VirtualButton.Light => InputButton.Light,
                VirtualButton.Medium => InputButton.Medium,
                VirtualButton.Heavy => InputButton.Heavy,
                VirtualButton.Special => InputButton.Special,
                VirtualButton.Block => InputButton.Block,
                VirtualButton.Grab => InputButton.Grab,
                VirtualButton.Super => InputButton.Super,
                VirtualButton.Dash => InputButton.Dash,
                VirtualButton.Taunt => InputButton.Taunt,
                _ => InputButton.None
            };
        }
        
        public void RemapButton(VirtualButton button, KeyCode newKey)
        {
            if (mappingLookup.TryGetValue(button, out var mapping))
            {
                mapping.primaryKey = newKey;
            }
        }
        
        public void RemapButton(VirtualButton button, KeyCode primary, KeyCode secondary)
        {
            if (mappingLookup.TryGetValue(button, out var mapping))
            {
                mapping.primaryKey = primary;
                mapping.secondaryKey = secondary;
            }
        }
        
        public KeyMapping GetMapping(VirtualButton button)
        {
            return mappingLookup.TryGetValue(button, out var mapping) ? mapping : null;
        }
        
        private void DebugInputs()
        {
            foreach (var kvp in currentButtonStates)
            {
                if (kvp.Value)
                {
                    Debug.Log($"Button {kvp.Key} is pressed");
                }
            }
            
            if (currentMovement.magnitude > 0.01f)
            {
                Debug.Log($"Movement: {currentMovement}, Direction: {GetInputDirection()}");
            }
        }
        
        private void OnGUI()
        {
            if (!debugMode) return;
            
            GUILayout.BeginArea(new Rect(Screen.width - 250, 10, 240, 300));
            GUILayout.Label("Input Mapper Debug");
            
            GUILayout.Label($"Movement: {currentMovement:F2}");
            GUILayout.Label($"Direction: {GetInputDirection()}");
            
            GUILayout.Space(10);
            GUILayout.Label("Pressed Buttons:");
            
            foreach (var kvp in currentButtonStates)
            {
                if (kvp.Value)
                {
                    GUILayout.Label($"- {kvp.Key}");
                }
            }
            
            GUILayout.EndArea();
        }
    }
}