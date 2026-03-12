using System.IO;
using MoonTools.ECS;
using Tactician.Components;
using Tactician.Data;
using Tactician.Serialization;
using Tactician.Systems;

namespace Tactician.Tests;

/// <summary>
/// End-to-end integration tests combining SaveLoadManager and TurnHistoryManager.
/// Verifies that the two systems interoperate correctly: history snapshots contain
/// accurate world state data and the combined rewind + save/load workflow is sound.
/// </summary>
public class SaveLoadHistoryIntegrationTests : IDisposable
{
    private readonly World _world;
    private readonly ChessBoardSystem _chessBoardSystem;
    private readonly SaveLoadManager _saveLoadManager;
    private readonly TurnHistoryManager _historyManager;
    private readonly string _tempDir;
    private readonly Entity _gameStateEntity;

    public SaveLoadHistoryIntegrationTests()
    {
        _world = new World();
        _chessBoardSystem = new ChessBoardSystem(_world);
        _tempDir = Path.Combine(Path.GetTempPath(), "TactIntegTest_" + Guid.NewGuid().ToString("N"));
        _saveLoadManager = new SaveLoadManager(_world, _chessBoardSystem, _tempDir);
        _historyManager = new TurnHistoryManager(_saveLoadManager);

        _gameStateEntity = _world.CreateEntity();
        _world.Set(_gameStateEntity, new ChessGameState());
        _world.Set(_gameStateEntity, new CurrentTurn(Player.White));
        _world.Set(_gameStateEntity, new CurrentGamePhase(GamePhase.SelectingPiece));
        _world.Set(_gameStateEntity, new AiConfig(false, Player.Black));
    }

    public void Dispose()
    {
        _world.Dispose();
        if (Directory.Exists(_tempDir))
            Directory.Delete(_tempDir, recursive: true);
    }

    // ── Helpers ────────────────────────────────────────────────────────────────

    private Entity AddPiece(PieceType type, Player owner, int file, int rank)
    {
        var entity = _world.CreateEntity();
        _world.Set(entity, new ChessPiece(type, owner));
        _world.Set(entity, new BoardPosition(file, rank));
        return entity;
    }

    private void AdvanceTurn()
    {
        var current = _world.Get<CurrentTurn>(_gameStateEntity).Player;
        _world.Set(_gameStateEntity, new CurrentTurn(
            current == Player.White ? Player.Black : Player.White));
    }

    // ── Tests ──────────────────────────────────────────────────────────────────

    [Fact]
    public void RecordMultipleStates_StepBack_DataMatchesRecordedState()
    {
        AddPiece(PieceType.King, Player.White, 4, 0);

        // State 0: White's turn
        _world.Set(_gameStateEntity, new CurrentTurn(Player.White));
        _historyManager.RecordCurrentState();

        // State 1: Black's turn
        AdvanceTurn();
        _historyManager.RecordCurrentState();

        // Step back to state 0
        var state0 = _historyManager.StepBackward();

        Assert.NotNull(state0);
        Assert.Equal((int)Player.White, state0.GameState.CurrentTurn);
    }

    [Fact]
    public void RewindFullGame_AllStatesAccessible()
    {
        AddPiece(PieceType.King, Player.White, 4, 0);

        // Simulate 5 alternating turns
        var expectedTurns = new List<Player>();
        for (int i = 0; i < 5; i++)
        {
            var current = _world.Get<CurrentTurn>(_gameStateEntity).Player;
            expectedTurns.Add(current);
            _historyManager.RecordCurrentState();
            AdvanceTurn();
        }
        // expectedTurns = [White, Black, White, Black, White]

        // Step all the way back through history verifying each state
        // We're at index 4 after the loop above
        for (int i = 3; i >= 0; i--)
        {
            var state = _historyManager.StepBackward();
            Assert.NotNull(state);
            Assert.Equal((int)expectedTurns[i], state.GameState.CurrentTurn);
        }

        Assert.Equal(0, _historyManager.CurrentTurn);
    }

    [Fact]
    public void SaveToFile_LoadFromFile_MatchesExtractedState()
    {
        AddPiece(PieceType.King, Player.White, 4, 0);
        AddPiece(PieceType.Queen, Player.Black, 3, 7);
        _world.Set(_gameStateEntity, new CurrentTurn(Player.Black));
        _world.Set(_gameStateEntity, new CurrentGamePhase(GamePhase.AIThinking));

        var extracted = _saveLoadManager.ExtractSaveData();
        _saveLoadManager.SerializeToJson(extracted, "integration_test.json");
        var loaded = _saveLoadManager.DeserializeFromJson("integration_test.json");

        Assert.NotNull(loaded);
        Assert.Equal(extracted.GameState.CurrentTurn, loaded.GameState.CurrentTurn);
        Assert.Equal(extracted.GameState.Phase, loaded.GameState.Phase);
        Assert.Equal(extracted.Pieces.Count, loaded.Pieces.Count);
    }

    [Fact]
    public void HistorySnapshot_MatchesSavedFile_ForSameWorldState()
    {
        AddPiece(PieceType.King, Player.White, 4, 0);
        _world.Set(_gameStateEntity, new CurrentTurn(Player.White));

        // Both operations read from the same world at the same moment
        _historyManager.RecordCurrentState();
        _saveLoadManager.SaveToSlot(1);

        // Step back to get the recorded snapshot
        AdvanceTurn();
        _historyManager.RecordCurrentState(); // record a second state to allow step back
        var historySnapshot = _historyManager.StepBackward();

        var savedSnapshot = _saveLoadManager.LoadFromSlot(1);

        Assert.NotNull(historySnapshot);
        Assert.NotNull(savedSnapshot);
        Assert.Equal(savedSnapshot.GameState.CurrentTurn, historySnapshot.GameState.CurrentTurn);
        Assert.Equal(savedSnapshot.Pieces.Count, historySnapshot.Pieces.Count);
    }

    [Fact]
    public void History_AfterClear_CanRecordAndStepFresh()
    {
        AddPiece(PieceType.King, Player.White, 4, 0);
        for (int i = 0; i < 5; i++)
        {
            AdvanceTurn();
            _historyManager.RecordCurrentState();
        }

        _historyManager.ClearHistory();

        // Record two fresh states
        _world.Set(_gameStateEntity, new CurrentTurn(Player.White));
        _historyManager.RecordCurrentState(); // index 0
        AdvanceTurn();
        _historyManager.RecordCurrentState(); // index 1

        Assert.Equal(2, _historyManager.TotalTurns);

        var stepped = _historyManager.StepBackward();
        Assert.NotNull(stepped);
        Assert.Equal((int)Player.White, stepped.GameState.CurrentTurn);
        Assert.Equal(1, _historyManager.CanStepForwardCount);
    }

    [Fact]
    public void SimulatedGame_RecordThenSave_PieceCountConsistent()
    {
        // Simulate a partial game: add pieces, record state, remove one, record again
        var king = AddPiece(PieceType.King, Player.White, 4, 0);
        var pawn = AddPiece(PieceType.Pawn, Player.White, 0, 1);
        AddPiece(PieceType.King, Player.Black, 4, 7);

        _historyManager.RecordCurrentState(); // 3 pieces

        // "Capture" the pawn
        _world.Destroy(pawn);
        AdvanceTurn();
        _historyManager.RecordCurrentState(); // 2 pieces

        // Save current state (2 pieces)
        _saveLoadManager.SaveToSlot(3);

        // Step back to see 3-piece state in history
        var previousState = _historyManager.StepBackward();
        Assert.NotNull(previousState);
        Assert.Equal(3, previousState.Pieces.Count);

        // Saved state should still show 2 pieces
        var savedState = _saveLoadManager.LoadFromSlot(3);
        Assert.NotNull(savedState);
        Assert.Equal(2, savedState.Pieces.Count);
    }

    [Fact]
    public void BranchAfterSave_DoesNotAffectSavedFile()
    {
        AddPiece(PieceType.King, Player.White, 4, 0);
        _world.Set(_gameStateEntity, new CurrentTurn(Player.White));

        // Record and save 3 states
        _historyManager.RecordCurrentState();
        _saveLoadManager.SaveToSlot(2);
        AdvanceTurn();
        _historyManager.RecordCurrentState();
        AdvanceTurn();
        _historyManager.RecordCurrentState();

        // Branch: go back to state 1, record a new state
        _historyManager.StepBackward(); // at index 1
        _historyManager.StepBackward(); // at index 0
        AdvanceTurn();
        _historyManager.RecordCurrentState(); // branch: state 2 is now Black's turn at index 1

        // The saved file (slot 2) should be unaffected
        var savedState = _saveLoadManager.LoadFromSlot(2);
        Assert.NotNull(savedState);
        Assert.Equal((int)Player.White, savedState.GameState.CurrentTurn);
    }

    [Fact]
    public void MultipleHistorySteps_ThenSave_CapturesCurrentWorldState()
    {
        AddPiece(PieceType.King, Player.White, 4, 0);
        // Record 3 states: White, Black, White
        _world.Set(_gameStateEntity, new CurrentTurn(Player.White));
        _historyManager.RecordCurrentState();
        AdvanceTurn();
        _historyManager.RecordCurrentState();
        AdvanceTurn();
        _historyManager.RecordCurrentState();

        // Step back into history (world is NOT restored here — we only get the snapshot)
        _historyManager.StepBackward();
        _historyManager.StepBackward(); // now viewing state 0

        // Save the actual current world state (still White's — world wasn't changed)
        _saveLoadManager.SaveToSlot(4);
        var saved = _saveLoadManager.LoadFromSlot(4);

        Assert.NotNull(saved);
        // World still has White's turn (history viewing doesn't change the world in our tests)
        Assert.Equal((int)Player.White, saved.GameState.CurrentTurn);
    }
}
