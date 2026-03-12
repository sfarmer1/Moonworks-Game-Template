# Reset Functionality - ECS Architecture

## Overview

The reset functionality has been refactored to follow idiomatic ECS patterns, using components for state tracking instead of system-local variables.

## Key Components

### DestroyedOnReset
**File:** `src/Components/Components.cs`

Marker component that tags entities to be destroyed during game reset.

**Usage:**
```csharp
World.Set(entity, new DestroyedOnReset());
```

**Applied to:**
- Chess board squares (64 entities)
- Chess pieces (32 entities)
- Cursor entity (1 entity)
- Game state entity (1 entity)
- Game in progress entity (1 entity)

### Input State Tracking Components
**File:** `src/Components/ChessComponents.cs`

These components replace boolean fields in ChessInputSystem, making input state part of the ECS world state.

#### ConfirmButtonWasPressed
Tracks whether the confirm button (Space/Enter/Gamepad A) was pressed in the previous frame.

#### CancelButtonWasPressed
Tracks whether the cancel button (Escape/Backspace/Gamepad B) was pressed in the previous frame.

#### ResetButtonWasPressed
Tracks whether the reset button (R key) was pressed in the previous frame.

**Pattern:**
```csharp
// Read current frame input
var buttonPressed = _inputs.Keyboard.IsPressed(KeyCode.R);

// Read previous frame state from component
var buttonWasPressed = Has<ResetButtonWasPressed>(gameState);

// Detect new press (rising edge)
if (buttonPressed && !buttonWasPressed)
{
    // Handle new button press
}

// Update component state for next frame
if (buttonPressed)
    Set(gameState, new ResetButtonWasPressed());
else
    Remove<ResetButtonWasPressed>(gameState);
```

## Architecture Changes

### Before (Anti-Pattern)
```csharp
public class ChessInputSystem : System
{
    // ❌ State stored in system fields
    private bool _resetWasPressed;
    private bool _confirmWasPressed;
    private bool _cancelWasPressed;

    public override void Update(TimeSpan delta)
    {
        var resetPressed = _inputs.Keyboard.IsPressed(KeyCode.R);
        if (resetPressed && !_resetWasPressed)
        {
            // Handle reset
        }
        _resetWasPressed = resetPressed; // State lives outside ECS
    }
}
```

**Problems:**
- State not visible to other systems
- Not serializable with world state
- Breaks ECS single source of truth principle
- Cannot be queried or filtered

### After (Idiomatic ECS)
```csharp
public class ChessInputSystem : System
{
    // ✅ No state stored in system

    public override void Update(TimeSpan delta)
    {
        var gameState = GetGameStateEntity();

        var resetPressed = _inputs.Keyboard.IsPressed(KeyCode.R);
        var resetWasPressed = Has<ResetButtonWasPressed>(gameState);

        if (resetPressed && !resetWasPressed)
        {
            // Handle reset
        }

        // State stored as component in ECS world
        if (resetPressed)
            Set(gameState, new ResetButtonWasPressed());
        else
            Remove<ResetButtonWasPressed>(gameState);
    }
}
```

**Benefits:**
- ✅ State is part of ECS world
- ✅ Visible to all systems
- ✅ Can be serialized with save games
- ✅ Queryable through filters
- ✅ Single source of truth
- ✅ Testable in isolation

## Reset Flow

### 1. Input Detection (ChessInputSystem)
```csharp
// Detect R key press
var resetPressed = _inputs.Keyboard.IsPressed(KeyCode.R);
var resetWasPressed = Has<ResetButtonWasPressed>(gameState);

if (resetPressed && !resetWasPressed)
{
    Send(new ResetGameMessage());
}

// Update component for next frame
if (resetPressed)
    Set(gameState, new ResetButtonWasPressed());
else
    Remove<ResetButtonWasPressed>(gameState);
```

### 2. Message Handling (InGameAppState)
```csharp
if (_world.SomeMessage<ResetGameMessage>())
{
    _world.FinishUpdate();
    ResetGame();
    return;
}
```

### 3. Entity Destruction (InGameAppState.ResetGame)
```csharp
// Destroy all entities with DestroyedOnReset component
foreach (var entity in _resettableEntitiesFilter.Entities)
{
    _world.Destroy(entity);
}

// Clear internal tracking arrays
_chessBoardSystem.ClearBoard();
```

### 4. Entity Recreation (InGameAppState.InitializeEntities)
```csharp
// Board creates squares with DestroyedOnReset
_chessBoardSystem.InitializeBoard();

// Board creates pieces with DestroyedOnReset
_chessBoardSystem.SpawnInitialPieces();

// Create game state with DestroyedOnReset
var gameStateEntity = _world.CreateEntity();
_chessTurnSystem.InitializeGameState(gameStateEntity);
_world.Set(gameStateEntity, new DestroyedOnReset());
```

## Filter Usage

### Pre-built Filter
**File:** `src/GameStates/InGameAppState.cs`

```csharp
public override void Start()
{
    _world = new World();

    // Build filter BEFORE creating entities
    _resettableEntitiesFilter = _world.FilterBuilder
        .Include<DestroyedOnReset>()
        .Build();

    // Now create entities
    InitializeEntities();
}
```

**Why Pre-built?**
- Filter built before entities are created
- No need for FinishUpdate() to sync
- Filter automatically tracks entities as they're created/destroyed
- Reusable across multiple resets

## Bug Fixes

### Cursor Not Being Destroyed
**Root Cause:** Filter created after entities existed, without FinishUpdate() to sync.

**Solution:**
1. Build filter at Start() before entities exist
2. Mark cursor with DestroyedOnReset component
3. Filter automatically tracks cursor creation in InitializeBoard()

### Multiple Cursors After Reset
**Root Cause:** Old cursor not destroyed, new cursor created.

**Solution:**
- Filter properly destroys old cursor
- InitializeBoard() creates new cursor with DestroyedOnReset
- One cursor always exists after reset

## Component Lifecycle

### DestroyedOnReset Entities

**Creation:**
```csharp
// Board squares
var square = World.CreateEntity();
World.Set(square, new BoardPosition(file, rank));
World.Set(square, new DestroyedOnReset()); // ✅ Tagged

// Chess pieces
var piece = World.CreateEntity();
World.Set(piece, new ChessPiece(type, owner));
World.Set(piece, new DestroyedOnReset()); // ✅ Tagged

// Cursor
var cursor = World.CreateEntity();
World.Set(cursor, new Cursor());
World.Set(cursor, new DestroyedOnReset()); // ✅ Tagged
```

**Destruction:**
```csharp
// Filter automatically contains all tagged entities
foreach (var entity in _resettableEntitiesFilter.Entities)
{
    _world.Destroy(entity); // All components removed, entity freed
}
```

**Recreation:**
```csharp
// InitializeEntities creates fresh entities with components
InitializeEntities();
```

## Best Practices Applied

### ✅ Separation of Concerns
- **ChessBoardSystem:** Manages board arrays, creates/destroys entities
- **ChessInputSystem:** Reads input, sets components, sends messages
- **InGameAppState:** Orchestrates reset, manages system lifecycle

### ✅ Component-Based State
- Input state stored as components on game state entity
- Resettable entities marked with DestroyedOnReset component
- No hidden state in system fields

### ✅ Filter Efficiency
- Filter built once at startup
- Automatically tracks entity creation/destruction
- No manual synchronization needed

### ✅ Clear Data Flow
1. Input → Component state on game state entity
2. Component state → Message
3. Message → Reset function
4. Reset → Filter-based entity destruction
5. Destruction → Recreation with components

## Testing

### Verify Reset Works
```bash
dotnet run --project Tactician.csproj
```

**Test Steps:**
1. Make a move
2. Press 'R' to reset
3. Check logs for "Destroyed N entities"
4. Verify board is in starting position
5. Verify only one cursor exists

**Expected Log Output:**
```
INFO: Game reset requested
INFO: Resetting game...
INFO: Destroyed 99 entities
INFO: Game reset complete
```

**Entity Count Breakdown:**
- 64 board squares
- 32 chess pieces
- 1 cursor
- 1 game state entity
- 1 game in progress entity
= 99 total entities destroyed and recreated

## Performance

### Reset Operation
- **Destroy:** O(n) where n = entities with DestroyedOnReset
- **Clear Arrays:** O(64) constant time for 8x8 board
- **Recreate:** O(n) to create fresh entities
- **Total:** ~99 entity operations, < 1ms on modern hardware

### Memory
- No memory leaks - all entities properly destroyed
- Filter maintains references, updated automatically
- Component state cleared with entity destruction

## Future Enhancements

### Possible Additions
1. **Save/Load Support:** Input state components are serializable
2. **Replay System:** Component state can be recorded per frame
3. **Undo/Redo:** Stack of component states
4. **Network Sync:** Component state easily transmitted

### Extensibility
To add new resettable entities:
```csharp
var newEntity = World.CreateEntity();
World.Set(newEntity, new YourComponent());
World.Set(newEntity, new DestroyedOnReset()); // Auto-reset!
```

No changes needed to reset logic - filter automatically includes it.

---

**Architecture Version:** 2.0 (ECS-Idiomatic)
**Last Updated:** March 7, 2026
**Status:** ✅ Production Ready
