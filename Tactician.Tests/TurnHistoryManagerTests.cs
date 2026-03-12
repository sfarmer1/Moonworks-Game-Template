using System.IO;
using MoonTools.ECS;
using Tactician.Components;
using Tactician.Data;
using Tactician.Serialization;
using Tactician.Systems;

namespace Tactician.Tests;

/// <summary>
/// Integration tests for TurnHistoryManager — the rewind/playback state machine.
/// These tests run headlessly: no graphics or content pipeline is required.
/// They instantiate a real World + SaveLoadManager + TurnHistoryManager and
/// manipulate ECS state directly between recordings.
/// </summary>
public class TurnHistoryManagerTests : IDisposable
{
    private readonly World _world;
    private readonly ChessBoardSystem _chessBoardSystem;
    private readonly SaveLoadManager _saveLoadManager;
    private readonly TurnHistoryManager _historyManager;
    private readonly string _tempDir;
    private readonly Entity _gameStateEntity;

    public TurnHistoryManagerTests()
    {
        _world = new World();
        _chessBoardSystem = new ChessBoardSystem(_world);
        _tempDir = Path.Combine(Path.GetTempPath(), "TactTest_" + Guid.NewGuid().ToString("N"));
        _saveLoadManager = new SaveLoadManager(_world, _chessBoardSystem, _tempDir);
        _historyManager = new TurnHistoryManager(_saveLoadManager);

        // Set up a minimal game-state entity (required by ExtractSaveData)
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

    /// <summary>Adds a single pawn piece to the world at the given position.</summary>
    private Entity AddPiece(PieceType type, Player owner, int file, int rank)
    {
        var entity = _world.CreateEntity();
        _world.Set(entity, new ChessPiece(type, owner));
        _world.Set(entity, new BoardPosition(file, rank));
        return entity;
    }

    /// <summary>
    /// Switches the current turn on the game-state entity so consecutive
    /// RecordCurrentState() calls see different data (avoiding dedup).
    /// </summary>
    private void AdvanceTurn()
    {
        var current = _world.Get<CurrentTurn>(_gameStateEntity).Player;
        var next = current == Player.White ? Player.Black : Player.White;
        _world.Set(_gameStateEntity, new CurrentTurn(next));
    }

    // ── Section 1: Initial state ───────────────────────────────────────────────

    [Fact]
    public void History_StartsEmpty()
    {
        Assert.Equal(0, _historyManager.TotalTurns);
        Assert.Equal(-1, _historyManager.CurrentTurn);
        Assert.False(_historyManager.IsViewingHistory);
    }

    // ── Section 2: Recording ──────────────────────────────────────────────────

    [Fact]
    public void RecordCurrentState_FirstRecord_Succeeds()
    {
        AddPiece(PieceType.King, Player.White, 4, 0);

        _historyManager.RecordCurrentState();

        Assert.Equal(1, _historyManager.TotalTurns);
        Assert.Equal(0, _historyManager.CurrentTurn);
    }

    [Fact]
    public void RecordCurrentState_DuplicateState_IsIgnored()
    {
        // Record the same world state twice — StatesAreEqual should suppress the duplicate
        AddPiece(PieceType.King, Player.White, 4, 0);
        _historyManager.RecordCurrentState();
        _historyManager.RecordCurrentState(); // identical state

        Assert.Equal(1, _historyManager.TotalTurns);
    }

    [Fact]
    public void RecordCurrentState_DistinctStates_AreAllRecorded()
    {
        AddPiece(PieceType.King, Player.White, 4, 0);
        _historyManager.RecordCurrentState(); // turn 0: White

        AdvanceTurn(); // now Black
        _historyManager.RecordCurrentState(); // turn 1: Black

        AdvanceTurn(); // now White again
        _historyManager.RecordCurrentState(); // turn 2: White

        Assert.Equal(3, _historyManager.TotalTurns);
    }

    // ── Section 3: Boundary checks ────────────────────────────────────────────

    [Fact]
    public void StepBackward_AtBeginning_ReturnsNull()
    {
        AddPiece(PieceType.King, Player.White, 4, 0);
        _historyManager.RecordCurrentState(); // index 0

        var result = _historyManager.StepBackward(); // already at 0

        Assert.Null(result);
        Assert.Equal(0, _historyManager.CurrentTurn);
    }

    [Fact]
    public void StepForward_AtLatest_ReturnsNull()
    {
        AddPiece(PieceType.King, Player.White, 4, 0);
        _historyManager.RecordCurrentState();
        AdvanceTurn();
        _historyManager.RecordCurrentState();

        // Already at latest (index 1)
        var result = _historyManager.StepForward();

        Assert.Null(result);
    }

    // ── Section 4: Stepping backward/forward ──────────────────────────────────

    [Fact]
    public void StepBackward_ReturnsCorrectState()
    {
        AddPiece(PieceType.King, Player.White, 4, 0);
        _historyManager.RecordCurrentState(); // state 0: White's turn

        AdvanceTurn();
        _historyManager.RecordCurrentState(); // state 1: Black's turn

        var previous = _historyManager.StepBackward(); // should give state 0

        Assert.NotNull(previous);
        Assert.Equal((int)Player.White, previous.GameState.CurrentTurn);
    }

    [Fact]
    public void StepForward_AfterStepBack_ReturnsCorrectState()
    {
        AddPiece(PieceType.King, Player.White, 4, 0);
        _historyManager.RecordCurrentState(); // state 0: White
        AdvanceTurn();
        _historyManager.RecordCurrentState(); // state 1: Black

        _historyManager.StepBackward(); // now at 0
        var forward = _historyManager.StepForward(); // back to 1

        Assert.NotNull(forward);
        Assert.Equal((int)Player.Black, forward.GameState.CurrentTurn);
    }

    [Fact]
    public void StepForward_AtLatestAfterStepBack_SetsIsViewingHistoryFalse()
    {
        AddPiece(PieceType.King, Player.White, 4, 0);
        _historyManager.RecordCurrentState();
        AdvanceTurn();
        _historyManager.RecordCurrentState();

        _historyManager.StepBackward();
        Assert.True(_historyManager.IsViewingHistory);

        _historyManager.StepForward();
        Assert.False(_historyManager.IsViewingHistory);
    }

    // ── Section 5: IsViewingHistory ────────────────────────────────────────────

    [Fact]
    public void IsViewingHistory_SetCorrectly_ThroughNavigation()
    {
        AddPiece(PieceType.King, Player.White, 4, 0);
        _historyManager.RecordCurrentState();
        AdvanceTurn();
        _historyManager.RecordCurrentState();
        AdvanceTurn();
        _historyManager.RecordCurrentState(); // 3 states: 0, 1, 2

        // Step back once → viewing history
        _historyManager.StepBackward();
        Assert.True(_historyManager.IsViewingHistory);

        // Step back again → still viewing history
        _historyManager.StepBackward();
        Assert.True(_historyManager.IsViewingHistory);

        // Step forward twice → back at latest
        _historyManager.StepForward();
        _historyManager.StepForward();
        Assert.False(_historyManager.IsViewingHistory);
    }

    // ── Section 6: ReturnToPresent ────────────────────────────────────────────

    [Fact]
    public void ReturnToPresent_FromMiddle_JumpsToLatest()
    {
        AddPiece(PieceType.King, Player.White, 4, 0);
        for (int i = 0; i < 5; i++)
        {
            AdvanceTurn();
            _historyManager.RecordCurrentState();
        }
        // 5 states recorded (index 0..4)

        _historyManager.StepBackward();
        _historyManager.StepBackward();
        _historyManager.StepBackward();

        var present = _historyManager.ReturnToPresent();

        Assert.NotNull(present);
        Assert.Equal(4, _historyManager.CurrentTurn);
        Assert.False(_historyManager.IsViewingHistory);
    }

    [Fact]
    public void ReturnToPresent_WhenAlreadyPresent_ReturnsNull()
    {
        AddPiece(PieceType.King, Player.White, 4, 0);
        _historyManager.RecordCurrentState();
        AdvanceTurn();
        _historyManager.RecordCurrentState();

        // At latest — ReturnToPresent should be a no-op
        var result = _historyManager.ReturnToPresent();

        Assert.Null(result);
    }

    // ── Section 7: CanStepBackward / CanStepForward counts ───────────────────

    [Fact]
    public void CanStepBackwardCount_And_CanStepForwardCount_Accurate()
    {
        AddPiece(PieceType.King, Player.White, 4, 0);
        for (int i = 0; i < 4; i++)
        {
            AdvanceTurn();
            _historyManager.RecordCurrentState();
        }
        // 4 states: indices 0..3, currently at 3

        _historyManager.StepBackward();
        _historyManager.StepBackward(); // now at index 1

        Assert.Equal(1, _historyManager.CanStepBackwardCount);
        Assert.Equal(2, _historyManager.CanStepForwardCount);
    }

    // ── Section 8: ClearHistory ───────────────────────────────────────────────

    [Fact]
    public void ClearHistory_ResetsAllState()
    {
        AddPiece(PieceType.King, Player.White, 4, 0);
        _historyManager.RecordCurrentState();
        AdvanceTurn();
        _historyManager.RecordCurrentState();
        AdvanceTurn();
        _historyManager.RecordCurrentState();
        _historyManager.StepBackward();

        _historyManager.ClearHistory();

        Assert.Equal(0, _historyManager.TotalTurns);
        Assert.Equal(-1, _historyManager.CurrentTurn);
        Assert.False(_historyManager.IsViewingHistory);
    }

    [Fact]
    public void AfterClearHistory_CanRecordFresh()
    {
        AddPiece(PieceType.King, Player.White, 4, 0);
        for (int i = 0; i < 5; i++)
        {
            AdvanceTurn();
            _historyManager.RecordCurrentState();
        }

        _historyManager.ClearHistory();

        _historyManager.RecordCurrentState(); // first fresh record
        AdvanceTurn();
        _historyManager.RecordCurrentState(); // second fresh record

        Assert.Equal(2, _historyManager.TotalTurns);
        Assert.Equal(1, _historyManager.CurrentTurn);
    }

    // ── Section 9: History branching ──────────────────────────────────────────

    [Fact]
    public void RecordWhileViewingHistory_BranchesTimeline()
    {
        AddPiece(PieceType.King, Player.White, 4, 0);
        _historyManager.RecordCurrentState(); // state A(0): White
        AdvanceTurn();
        _historyManager.RecordCurrentState(); // state B(1): Black
        AdvanceTurn();
        _historyManager.RecordCurrentState(); // state C(2): White — 3 states total, at index 2

        // Step back once to B (index 1), then record new state D
        _historyManager.StepBackward(); // index 2 → 1 (B)
        Assert.Equal(1, _historyManager.CurrentTurn);
        Assert.True(_historyManager.IsViewingHistory);

        AdvanceTurn(); // White → Black, so new state differs from C
        _historyManager.RecordCurrentState(); // state D — C should be discarded

        // History is now [A, B, D]
        Assert.Equal(3, _historyManager.TotalTurns);
        Assert.False(_historyManager.IsViewingHistory);
    }

    [Fact]
    public void BranchedHistory_CanStillStepBackward()
    {
        AddPiece(PieceType.King, Player.White, 4, 0);
        _historyManager.RecordCurrentState(); // A(0): White
        AdvanceTurn();
        _historyManager.RecordCurrentState(); // B(1): Black
        AdvanceTurn();
        _historyManager.RecordCurrentState(); // C(2): White — at index 2

        // Branch at B: one step back to B(1), then record D
        _historyManager.StepBackward(); // 2 → 1 (B)
        AdvanceTurn(); // Black → White (same as C, but we're in history mode so dedup is skipped)
        // Actually advance one more time to make it unambiguously different
        AdvanceTurn(); // White → Black
        _historyManager.RecordCurrentState(); // D(Black) at index 2, C discarded
        // History is now [A(White), B(Black), D(Black)] — at index 2

        // Can still step back through A and B
        var stateB = _historyManager.StepBackward(); // 2 → 1
        Assert.NotNull(stateB);
        Assert.Equal(1, _historyManager.CurrentTurn);

        var stateA = _historyManager.StepBackward(); // 1 → 0
        Assert.NotNull(stateA);
        Assert.Equal(0, _historyManager.CurrentTurn);
        Assert.Equal((int)Player.White, stateA.GameState.CurrentTurn);
    }

    [Fact]
    public void MultipleBranches_EachTruncatesCorrectly()
    {
        AddPiece(PieceType.King, Player.White, 4, 0);
        // Build 5 states
        for (int i = 0; i < 5; i++)
        {
            AdvanceTurn();
            _historyManager.RecordCurrentState();
        }
        Assert.Equal(5, _historyManager.TotalTurns);

        // Branch at index 2 (step back 2 from index 4)
        _historyManager.StepBackward();
        _historyManager.StepBackward(); // now at index 2
        AdvanceTurn();
        _historyManager.RecordCurrentState(); // branch: 3 states (0,1,2 + new)
        Assert.Equal(4, _historyManager.TotalTurns);

        // Branch again at index 1
        _historyManager.StepBackward(); // at index 2
        _historyManager.StepBackward(); // at index 1
        AdvanceTurn();
        _historyManager.RecordCurrentState(); // branch: states 0,1 + new
        Assert.Equal(3, _historyManager.TotalTurns);
        Assert.Equal(2, _historyManager.CurrentTurn);
    }
}
