# Fighting Framework - Input System

## Overview

The Fighting Framework Input System provides sophisticated input buffering, motion recognition, and virtual button abstraction specifically designed for fighting games. It handles complex input sequences, timing-sensitive commands, and provides a clean interface for both developers and players.

## Core Components

### 1. InputBuffer
**Location**: `Runtime/Scripts/Input/InputBuffer.cs`

The InputBuffer is a circular buffer that stores recent input commands with frame-perfect timing information.

#### Key Features
- **Configurable buffer size** (default: 60 frames = 1 second at 60 FPS)
- **Frame-accurate timing** for precise input recognition
- **Debug visualization** for development
- **Multiple query methods** for different use cases

#### Usage Example
```csharp
public class PlayerController : MonoBehaviour
{
    [SerializeField] private InputBuffer inputBuffer;
    [SerializeField] private MotionInput fireball;
    
    private void Update()
    {
        // Check for special move
        if (inputBuffer.CheckMotion(fireball, 15))
        {
            ExecuteFireball();
        }
    }
}
```

#### Methods
- `AddInput(InputCommand)` - Add input to buffer
- `CheckMotion(MotionInput, frameWindow)` - Check for motion pattern
- `GetRecentInputs(frameWindow)` - Get inputs within frame window
- `HasButtonInFrameWindow(button, frameWindow)` - Check for specific button
- `ClearBuffer()` - Clear all stored inputs

### 2. InputMapper
**Location**: `Runtime/Scripts/Input/InputMapper.cs`

Virtual button abstraction layer that maps physical inputs to game actions.

#### Key Features
- **Keyboard and controller support**
- **Dual key binding** (primary + secondary keys)
- **8-way directional input**
- **Runtime remapping**
- **Event-driven architecture**

#### Usage Example
```csharp
public class InputHandler : MonoBehaviour
{
    [SerializeField] private InputMapper inputMapper;
    [SerializeField] private InputBuffer inputBuffer;
    
    private void Start()
    {
        inputMapper.OnButtonPressed += OnButtonPressed;
        inputMapper.OnMovementChanged += OnMovementChanged;
    }
    
    private void OnButtonPressed(VirtualButton button)
    {
        var inputButton = inputMapper.VirtualButtonToInputButton(button);
        inputBuffer.AddButtonInput(inputButton, true);
    }
    
    private void OnMovementChanged(Vector2 movement)
    {
        var direction = inputMapper.GetInputDirection();
        inputBuffer.AddDirectionInput(direction, movement);
    }
}
```

#### Virtual Buttons
```csharp
public enum VirtualButton
{
    Light, Medium, Heavy, Special,    // Attack buttons
    Up, Down, Left, Right,           // Directions
    Block, Grab, Super, Dash,        // Special actions
    Taunt, Start, Select             // Utility buttons
}
```

### 3. MotionInput (ScriptableObject)
**Location**: `Runtime/Scripts/Input/MotionInput.cs`

Configurable motion patterns for special moves and combos.

#### Creation
Create via Unity menu: `Fighting Framework/Input/Motion`

#### Configuration Options
- **Motion Name**: Display name for the motion
- **Directions**: Required directional inputs (e.g., Down, DownRight, Right)
- **Buttons**: Required button inputs (e.g., Light, Medium)
- **Frame Window**: Maximum frames between inputs (5-60)
- **Allow Dirty Inputs**: Accept extra inputs between required ones
- **Priority**: Used when multiple motions match

#### Common Motion Examples
```csharp
// Quarter Circle Forward + Light (Fireball)
Directions: [Down, DownRight, Right]
Buttons: [Light]
Frame Window: 15
Allow Dirty Inputs: true

// Dragon Punch (Z-motion + Heavy)
Directions: [Right, Down, DownRight]
Buttons: [Heavy]
Frame Window: 12
Allow Dirty Inputs: false

// Double Tap Forward (Dash)
Directions: [Right, Right]
Buttons: []
Frame Window: 10
Allow Dirty Inputs: false
```

### 4. MotionRecognizer
**Location**: `Runtime/Scripts/Input/MotionRecognizer.cs`

Static class that handles motion pattern recognition.

#### Key Methods
- `CheckSequence(inputHistory, motion)` - Check if motion exists in input history
- `CheckMultipleMotions(inputHistory, motions)` - Check multiple motions, return matches
- `GetBestMotionMatch(inputHistory, motions)` - Get highest priority match
- `CheckAdvancedSequence(inputHistory, sequence)` - Advanced sequence matching

#### Built-in Motion Creators
```csharp
// Create common fighting game motions
var qcf = MotionRecognizer.CreateQuarterCircleForward(InputButton.Light);
var qcb = MotionRecognizer.CreateQuarterCircleBack(InputButton.Medium);
var dp = MotionRecognizer.CreateDragonPunch(InputButton.Heavy);
```

### 5. CircularBuffer<T>
**Location**: `Runtime/Scripts/Utilities/CircularBuffer.cs`

Generic circular buffer implementation optimized for input storage.

#### Features
- **Fixed capacity** with automatic overwrite
- **O(1) insertion** and recent item access
- **Configurable size** for different use cases
- **Thread-safe operations**

## Input Types and Enums

### InputType
```csharp
public enum InputType
{
    None,           // No input
    Direction,      // Directional input
    Button,         // Button press
    ButtonRelease,  // Button release
    Neutral         // Return to neutral
}
```

### InputDirection (8-way)
```csharp
public enum InputDirection
{
    Neutral = 0,    // 5 (center)
    Up = 1,         // 8 (up)
    UpRight = 2,    // 9 (up-right)
    Right = 3,      // 6 (right)
    DownRight = 4,  // 3 (down-right)
    Down = 5,       // 2 (down)
    DownLeft = 6,   // 1 (down-left)
    Left = 7,       // 4 (left)
    UpLeft = 8      // 7 (up-left)
}
```

### InputButton
```csharp
public enum InputButton
{
    None, Light, Medium, Heavy, Special,
    Block, Grab, Super, Dash, Taunt
}
```

## Advanced Features

### Input Command Structure
```csharp
[System.Serializable]
public struct InputCommand
{
    public InputType type;              // Type of input
    public Vector2 direction;           // Raw direction vector
    public InputDirection inputDirection; // 8-way direction
    public InputButton button;          // Button pressed
    public float timestamp;             // Time.time when input occurred
    public int frame;                   // Time.frameCount when input occurred
    public bool isPressed;              // Press vs release
}
```

### Motion Input Sequence (Advanced)
For complex multi-part motions:
```csharp
var sequence = new MotionInputSequence();
sequence.AddDirection(InputDirection.Down);
sequence.AddDirection(InputDirection.DownRight);
sequence.AddDirection(InputDirection.Right);
sequence.AddButton(InputButton.Light);
sequence.maxSequenceTime = 0.5f;
sequence.allowPartialMatch = false;

bool matches = MotionRecognizer.CheckAdvancedSequence(inputHistory, sequence);
```

## Integration Examples

### Basic Setup
1. Create GameObject with `InputMapper` component
2. Create GameObject with `InputBuffer` component
3. Connect InputMapper events to InputBuffer
4. Create MotionInput ScriptableObjects for special moves

### Complete Input Handler
```csharp
public class FightingGameInputHandler : MonoBehaviour
{
    [Header("Components")]
    [SerializeField] private InputMapper inputMapper;
    [SerializeField] private InputBuffer inputBuffer;
    
    [Header("Motions")]
    [SerializeField] private List<MotionInput> specialMoves;
    
    [Header("Settings")]
    [SerializeField] private int motionCheckFrameWindow = 15;
    
    public event System.Action<MotionInput> OnMotionExecuted;
    public event System.Action<InputButton> OnButtonPressed;
    public event System.Action<InputDirection> OnDirectionChanged;
    
    private void Start()
    {
        // Subscribe to input events
        inputMapper.OnButtonPressed += HandleButtonPressed;
        inputMapper.OnButtonReleased += HandleButtonReleased;
        inputMapper.OnMovementChanged += HandleMovementChanged;
    }
    
    private void Update()
    {
        CheckForSpecialMoves();
    }
    
    private void HandleButtonPressed(VirtualButton vButton)
    {
        var inputButton = inputMapper.VirtualButtonToInputButton(vButton);
        if (inputButton != InputButton.None)
        {
            inputBuffer.AddButtonInput(inputButton, true);
            OnButtonPressed?.Invoke(inputButton);
        }
    }
    
    private void HandleButtonReleased(VirtualButton vButton)
    {
        var inputButton = inputMapper.VirtualButtonToInputButton(vButton);
        if (inputButton != InputButton.None)
        {
            inputBuffer.AddButtonInput(inputButton, false);
        }
    }
    
    private void HandleMovementChanged(Vector2 movement)
    {
        var direction = inputMapper.GetInputDirection();
        inputBuffer.AddDirectionInput(direction, movement);
        OnDirectionChanged?.Invoke(direction);
    }
    
    private void CheckForSpecialMoves()
    {
        var bestMatch = MotionRecognizer.GetBestMotionMatch(
            inputBuffer.GetRecentInputs(motionCheckFrameWindow), 
            specialMoves);
            
        if (bestMatch != null)
        {
            OnMotionExecuted?.Invoke(bestMatch);
        }
    }
}
```

## Performance Considerations

### Buffer Sizing
- **60 frames (1 second)**: Good for most fighting games
- **30 frames (0.5 seconds)**: Faster-paced games
- **120 frames (2 seconds)**: Slower-paced or combo-heavy games

### Motion Recognition Optimization
- Use `allowDirtyInputs = true` for lenient recognition
- Set appropriate `maxFrameWindow` (10-20 frames typical)
- Sort motions by complexity (simple motions first)
- Use motion priority for overlapping patterns

### Memory Usage
- InputBuffer: ~240 bytes per 60-frame buffer
- CircularBuffer: ~16 bytes per stored element
- MotionInput: ~100 bytes per ScriptableObject

## Testing and Debugging

### Debug Features
1. **InputBuffer Debug Mode**: Shows input history on screen
2. **InputMapper Debug Mode**: Displays current inputs and mappings
3. **Frame Counter**: Shows exact frame timing
4. **Input History Visualization**: Visual representation of recent inputs

### Unit Testing
The framework includes comprehensive unit tests:
- `CircularBufferTests`: Buffer functionality
- `InputCommandTests`: Command creation and serialization
- `MotionInputTests`: Motion validation
- `MotionRecognizerTests`: Pattern recognition
- `InputBufferTests`: Buffer integration

### Common Issues and Solutions

#### Motion Not Recognized
1. Check `maxFrameWindow` setting
2. Verify input sequence order
3. Enable `allowDirtyInputs` for lenient matching
4. Use debug mode to visualize input history

#### Input Lag
1. Reduce buffer size if memory is limited
2. Optimize motion checking frequency
3. Use `GetButtonDown()` instead of `GetButton()` where appropriate

#### False Positives
1. Increase motion `priority` for more specific motions
2. Use `requireExactOrder = true`
3. Set `inputCooldown` to prevent spam
4. Reduce `maxFrameWindow` for tighter timing

## Future Enhancements

Planned features for future versions:
- **Macro Support**: Record and playback input sequences
- **Input Display**: On-screen input visualization for training mode
- **Network Synchronization**: Deterministic input handling for online play
- **Custom Input Events**: User-defined input pattern events
- **Analytics Integration**: Input timing and accuracy metrics