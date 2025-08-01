using UnityEngine;

namespace FightingFramework.Input
{
    public enum InputType
    {
        None,
        Direction,
        Button,
        ButtonRelease,
        Neutral
    }
    
    public enum InputDirection
    {
        Neutral = 0,
        Up = 1,
        UpRight = 2,
        Right = 3,
        DownRight = 4,
        Down = 5,
        DownLeft = 6,
        Left = 7,
        UpLeft = 8
    }
    
    public enum InputButton
    {
        None,
        Light,
        Medium,
        Heavy,
        Special,
        Block,
        Grab,
        Super,
        Dash,
        Taunt
    }
    
    public enum VirtualButton
    {
        Light,
        Medium,
        Heavy,
        Special,
        Up,
        Down,
        Left,
        Right,
        Block,
        Grab,
        Super,
        Dash,
        Taunt,
        Start,
        Select
    }
    
    [System.Serializable]
    public struct InputCommand
    {
        public InputType type;
        public Vector2 direction;
        public InputDirection inputDirection;
        public InputButton button;
        public float timestamp;
        public int frame;
        public bool isPressed;
        
        public InputCommand(InputType inputType, Vector2 dir = default, InputDirection inputDir = InputDirection.Neutral, 
                          InputButton inputButton = InputButton.None, bool pressed = true)
        {
            type = inputType;
            direction = dir;
            inputDirection = inputDir;
            button = inputButton;
            timestamp = Time.time;
            frame = Time.frameCount;
            isPressed = pressed;
        }
        
        public static InputCommand CreateDirectionInput(InputDirection dir, Vector2 vector)
        {
            return new InputCommand(InputType.Direction, vector, dir);
        }
        
        public static InputCommand CreateButtonInput(InputButton btn, bool pressed = true)
        {
            return new InputCommand(pressed ? InputType.Button : InputType.ButtonRelease, 
                                  default, InputDirection.Neutral, btn, pressed);
        }
        
        public static InputCommand CreateNeutralInput()
        {
            return new InputCommand(InputType.Neutral);
        }
        
        public override string ToString()
        {
            return type switch
            {
                InputType.Direction => $"Dir({inputDirection}) at F{frame}",
                InputType.Button => $"Btn({button}{(isPressed ? "+" : "-")}) at F{frame}",
                InputType.ButtonRelease => $"Btn({button}-) at F{frame}",
                InputType.Neutral => $"Neutral at F{frame}",
                _ => $"Unknown at F{frame}"
            };
        }
    }
}