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

### Replay/Rewind (Time Travel)
- **Z**: Step backward one turn in history (rewind)
- **X**: Step forward one turn in history (replay)
- History is recorded automatically after each main player move (not AI moves)
- Can rewind all the way back to the starting game state
- **Branching**: Make moves while viewing history to create alternate timelines
  - Rewind to any past state with Z
  - Make a different move than originally played
  - All future history from that point is discarded (new branch created)
  - AI is disabled while viewing history to allow player-controlled branching

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
   - Shift+1 through Shift+0: Save to slots 1-10
   - 1 through 0: Load from slots 1-10
   - F5: Quick save current game state
   - F9: Quick load from save file
   - Load performs full reset and restores state

5. **TurnHistoryManager** (`src/Serialization/TurnHistoryManager.cs`)
   - Turn-by-turn history tracking using save data
   - Automatic recording triggered by `TurnCompletedMessage` (efficient, only checks when turns complete)
   - Only records states when it's the main player's turn (not AI turns)
   - Z: Step backward one turn in history
   - X: Step forward one turn in history
   - State comparison to avoid duplicate entries
   - **History branching**: Automatically creates new branches when moves are made from past states
   - AI disabled when viewing history to allow player-controlled branching

6. **TurnCompletedMessage** (`src/Messages/Messages.cs`)
   - Message sent by `ChessMoveExecutionSystem` after each move
   - Triggers history recording only when needed (not every frame)

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

### Test 5: Rewind Feature (Z Key)
1. Start a new game
2. Make several moves (e.g., move 4-5 pieces)
3. Press **Z** repeatedly to step backward through history
4. Verify that:
   - Each press of Z takes you back one move
   - The board state accurately reflects the historical position
   - You can rewind all the way to the starting position
   - Input is disabled while viewing history (can't make moves)
5. Check console logs to see turn count

### Test 6: Replay Feature (X Key)
1. After rewinding (Test 5), press **X** to step forward
2. Verify that:
   - Each press of X moves forward one move through history
   - The board state accurately reflects each move
   - You can replay up to the latest recorded state
   - Can't step forward beyond the latest state

### Test 7: History Branching (Alternate Timelines)
1. Start a new game and make 3-4 moves (let AI respond each time)
2. Press **Z** multiple times to rewind several moves
3. Now make a different move than originally played
4. Verify that:
   - You can select and move pieces while viewing history
   - AI does not make moves while viewing history (so you control the branch)
   - The new move creates a new branch automatically
   - Future history (the discarded moves) is gone
   - Pressing **X** now does nothing (no future to replay)
   - New moves continue from the branched point
   - **AI resumes normal operation** after the branch is created (should log "AI is thinking..." and make a move)
5. Check console logs for:
   - "Branched history: discarded N future states" message
   - "AI thinking state reset" after branch
   - "AI is thinking..." when AI's turn comes
   - "AI finished thinking" after AI makes its move

### Test 8: History After Load
1. Save a game to slot 1 (Shift+1)
2. Make several more moves
3. Load from slot 1 (press 1)
4. Verify that:
   - History is cleared and starts fresh from the loaded state
   - Pressing **Z** doesn't show moves before the load
   - New moves create new history from the loaded point

### Test 9: History After Reset
1. Make several moves
2. Press **R** to reset the game
3. Verify that:
   - History is cleared
   - Pressing **Z** does nothing (only initial state exists)
   - New moves create fresh history

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

### Save/Load
1. Only 11 save slots total (1 quicksave + 10 numbered slots)
2. No UI feedback for save/load operations (only console logging)
3. No save file versioning/migration system yet
4. Player input state (selected piece, highlights) is not saved (by design)
5. No confirmation prompt before overwriting existing saves

### Replay/Rewind
1. History is stored in memory only (cleared on game restart)
2. No UI indicator showing current position in history
3. No fast-forward/rewind to specific turn
4. History can grow large in long games (all states kept in memory)
5. No way to save/load history along with save files

## Future Enhancements

### Completed ✅
- ✅ Multiple save slots (implemented: 10 numbered slots + quicksave)
- ✅ Replay/rewind system (implemented: Z/X keys for time travel)
- ✅ History branching (implemented: make moves while viewing history to create alternate timelines)

### Save/Load
- Auto-save functionality (e.g., save on every turn)
- Save file validation and error recovery
- UI notifications for save/load operations (visual feedback in-game)
- Save slot preview/metadata (show timestamp, turn count, etc.)
- Confirmation prompts before overwriting saves
- Cloud save support via MoonWorks UserStorage API
- Save file compression for smaller file sizes
- In-game save slot management UI (delete, rename, etc.)

### Replay/Rewind
- UI indicator showing position in history (e.g., "Turn 5 of 12")
- Fast-forward/rewind to specific turn (jump to turn N)
- Visual timeline scrubber
- History persistence (save/load history with save files)
- Export game replay to PGN or similar format
- Playback controls (play/pause auto-replay)
- Memory optimization (compress old history entries)
