# Save/Load System Testing

## Controls

### Saving
- **Shift+1** through **Shift+9**: Save to slots 1-9
- **Shift+0**: Save to slot 10
- **F5**: Quick save (backward compatibility, uses slot 0)

### Loading
- **1** through **9**: Load from slots 1-9
- **0**: Load from slot 10
- **F9**: Quick load (backward compatibility, uses slot 0)

## Implementation Summary
The save/load system has been successfully implemented with the following components:

1. **Serialization Attributes** (`src/Serialization/SerializationAttributes.cs`)
   - `[SerializableComponent]` - Marks components with data for serialization
   - `[SerializableMarker]` - Marks empty marker components for serialization
   - `[TransientComponent]` - Explicitly excludes components from serialization

2. **Save Data DTOs** (`src/Serialization/SaveData.cs`)
   - `ChessSaveData` - Root save structure with version and timestamp
   - `GameStateData` - Turn, phase, and AI configuration
   - `PieceData` - Piece type, owner, position, and flags

3. **SaveLoadManager** (`src/Serialization/SaveLoadManager.cs`)
   - Extracts game state from ECS world
   - Serializes/deserializes to/from JSON
   - Applies loaded state back to world

4. **Integration** (`src/GameStates/InGameAppState.cs`)
   - F5: Quick save current game state
   - F9: Quick load from save file
   - Load performs full reset and restores state

## Manual Testing Procedure

### Test 1: Save to Multiple Slots
1. Build and run the game:
   ```bash
   dotnet build Tactician.csproj
   export DYLD_LIBRARY_PATH=/path/to/Tactician/bin/Debug/net9.0
   dotnet run --project Tactician.csproj
   ```
2. Make some moves in the game
3. Press **Shift+1** to save to slot 1
4. Make different moves
5. Press **Shift+2** to save to slot 2
6. Check that save files were created at:
   - macOS: `~/Library/Application Support/Tactician/saves/save_slot_1.json`
   - macOS: `~/Library/Application Support/Tactician/saves/save_slot_2.json`
   - Linux: `~/.local/share/Tactician/saves/save_slot_N.json`
   - Windows: `%LOCALAPPDATA%\Tactician\saves\save_slot_N.json`
7. Verify JSON files contain different piece positions

### Test 2: Load from Specific Slots
1. With the game running, press **R** to reset the board
2. Press **1** to load from slot 1
3. Verify the board shows the first saved state
4. Press **2** to load from slot 2
5. Verify the board shows the second saved state
6. Verify that:
   - Pieces are restored to correct positions for each slot
   - Turn state is correct
   - AI configuration is preserved
   - Game can continue normally from loaded state

### Test 3: Save/Load Round-Trip with Slots
1. Start a new game
2. Make specific moves (e.g., move pawn from e2 to e4)
3. Press **Shift+3** to save to slot 3
4. Make different moves
5. Press **3** to load from slot 3
6. Verify the board returns to the state at step 2

### Test 4: Backward Compatibility (F5/F9)
1. Press **F5** to quick save
2. Make different moves
3. Press **F9** to quick load
4. Verify the F5/F9 keys still work as expected
5. Check that `quicksave.json` is created separately from slot files

### Test 4: En Passant and Castling Flags
1. Set up a position where en passant is possible
2. Save the game (F5)
3. Load the game (F9)
4. Verify en passant move is still available

## Save File Format

The game supports 11 save files total:
- **Slot 0** (F5/F9): `quicksave.json`
- **Slot 1** (1/Shift+1): `save_slot_1.json`
- **Slot 2** (2/Shift+2): `save_slot_2.json`
- **Slot 3** (3/Shift+3): `save_slot_3.json`
- **Slot 4** (4/Shift+4): `save_slot_4.json`
- **Slot 5** (5/Shift+5): `save_slot_5.json`
- **Slot 6** (6/Shift+6): `save_slot_6.json`
- **Slot 7** (7/Shift+7): `save_slot_7.json`
- **Slot 8** (8/Shift+8): `save_slot_8.json`
- **Slot 9** (9/Shift+9): `save_slot_9.json`
- **Slot 10** (0/Shift+0): `save_slot_10.json`

Example save file structure:
```json
{
  "Version": 1,
  "SavedAt": "2026-03-11T20:00:00Z",
  "GameState": {
    "CurrentTurn": 0,
    "Phase": "SelectingPiece",
    "AiConfig": {
      "IsEnabled": true,
      "AiPlayer": 1
    }
  },
  "Pieces": [
    {
      "Type": "King",
      "Owner": "White",
      "File": 4,
      "Rank": 0,
      "HasNotMoved": true,
      "IsEnPassantTarget": false
    }
  ]
}
```

## Known Limitations
1. Only 11 save slots total (1 quicksave + 10 numbered slots)
2. No UI feedback for save/load operations (only console logging)
3. No save file versioning/migration system yet
4. Player input state (selected piece, highlights) is not saved (by design)
5. No confirmation prompt before overwriting existing saves

## Future Enhancements
- ✅ Multiple save slots (implemented: 10 numbered slots + quicksave)
- Auto-save functionality (e.g., save on every turn)
- Save file validation and error recovery
- UI notifications for save/load operations (visual feedback in-game)
- Save slot preview/metadata (show timestamp, turn count, etc.)
- Confirmation prompts before overwriting saves
- Cloud save support via MoonWorks UserStorage API
- Save file compression for smaller file sizes
- In-game save slot management UI (delete, rename, etc.)
