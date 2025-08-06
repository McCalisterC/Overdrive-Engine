# Fighting Framework - Core Architecture

## Overview

The Fighting Framework implements two key architectural patterns to provide a robust, maintainable foundation for 2D/2.5D fighting games:

1. **ScriptableObject-Based Architecture** - For data-driven design and decoupled systems
2. **Service Locator Pattern** - For dependency management and system communication

## ScriptableObject Architecture

### Event System

The framework uses a ScriptableObject-based event system for decoupled communication between game systems.

#### GameEvent
- **Purpose**: Provides a way to broadcast events without tight coupling between systems
- **Location**: `Runtime/Scripts/Events/GameEvent.cs`
- **Usage**: Create via menu `Fighting Framework/Events/Game Event`

```csharp
// Raise an event
myGameEvent.Raise();

// Listen to events via GameEventListener component
// or register/unregister programmatically
gameEvent.RegisterListener(listener);
```

#### GameEventListener
- **Purpose**: MonoBehaviour component that responds to GameEvents
- **Location**: `Runtime/Scripts/Events/GameEventListener.cs`
- **Features**: Automatic registration/unregistration on Enable/Disable

### Variable System

ScriptableObject-based variables provide persistent, inspector-editable data that can be shared across systems.

#### Base Architecture
- **BaseVariable<T>**: Generic base class for all variable types
- **Features**: Runtime vs default values, automatic reset on enable

#### Variable Types
- **FloatVariable**: Numeric operations (add, subtract, multiply, divide, clamp)
- **IntVariable**: Integer operations with clamping
- **BoolVariable**: Boolean operations (toggle, set true/false)
- **StringVariable**: String operations (append, prepend, clear)

#### Usage Examples
```csharp
// In Inspector
[SerializeField] private FloatVariable playerHealth;
[SerializeField] private BoolVariable isGamePaused;

// In Code
playerHealth.Value = 100f;
playerHealth.Subtract(damage);
playerHealth.Clamp(0f, maxHealth);

// Implicit conversion
float currentHealth = playerHealth; // Automatically converts
```

## Service Locator Pattern

### ServiceScope
- **Purpose**: Base class for managing service registration and retrieval
- **Location**: `Runtime/Scripts/Core/ServiceScope.cs`
- **Features**: Singleton pattern, type-safe service access, automatic cleanup

#### Key Methods
```csharp
// Registration (in derived classes)
protected void Register<T>(T service) where T : class;
protected void RegisterInterface<TInterface, TImplementation>(TImplementation service);

// Retrieval (static access)
public static T Get<T>() where T : class;
public static bool TryGet<T>(out T service) where T : class;
public static bool IsRegistered<T>() where T : class;
```

### FightingFrameworkServiceScope
- **Purpose**: Concrete implementation for fighting game services
- **Location**: `Runtime/Scripts/Core/FightingFrameworkServiceScope.cs`
- **Usage**: Attach to a GameObject in your scene and configure services

#### Example Service Registration
```csharp
protected override void ConfigureServices()
{
    Register<IInputManager>(new InputManager());
    Register<IHealthSystem>(new HealthSystem());
    RegisterInterface<ICombatSystem, CombatSystem>(new CombatSystem());
}
```

#### Service Access
```csharp
// Get service (throws exception if not found)
var inputManager = ServiceScope.Get<IInputManager>();

// Try get service (returns false if not found)
if (ServiceScope.TryGet<IHealthSystem>(out var healthSystem))
{
    healthSystem.ApplyDamage(damage);
}
```

## Architecture Benefits

### ScriptableObject Benefits
- **Data-Driven Design**: Game configuration through Unity Inspector
- **Memory Efficiency**: Shared data references instead of copies
- **Designer Friendly**: Non-programmers can adjust game parameters
- **Runtime Persistence**: Values maintain state during play sessions

### Service Locator Benefits
- **Decoupling**: Systems don't need direct references to each other
- **Testability**: Easy to mock services for unit testing
- **Flexibility**: Services can be swapped or configured at runtime
- **Organization**: Clear separation of concerns

## Best Practices

### Events
- Use descriptive names for events (e.g., "PlayerDied", "RoundStarted")
- Keep event payloads simple or use typed events for complex data
- Unregister listeners properly to avoid memory leaks

### Variables
- Use appropriate variable types for your data
- Consider using events to notify when important variables change
- Set meaningful default values for variables

### Services
- Define interfaces for your services to improve testability
- Register services in dependency order
- Use TryGet for optional services
- Keep service interfaces focused and cohesive

## Integration Notes

This architecture provides the foundation for the fighting framework's other systems:
- Combat system will use events for hit notifications
- State machines will reference ScriptableObject variables
- Input system will be accessible via service locator
- UI will bind to ScriptableObject variables for reactive updates