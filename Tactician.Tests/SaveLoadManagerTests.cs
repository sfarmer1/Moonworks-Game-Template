using System.IO;
using System.Text.Json;
using MoonTools.ECS;
using Tactician.Components;
using Tactician.Data;
using Tactician.Serialization;
using Tactician.Systems;

namespace Tactician.Tests;

/// <summary>
/// Integration tests for SaveLoadManager — extraction, serialization, slot management.
/// These tests are headless: they do NOT call ApplySaveData() (which requires
/// ChessBoardSystem.SpawnPieceAt and initialized board squares).
/// World state is built directly via ECS component set/remove operations.
/// </summary>
public class SaveLoadManagerTests : IDisposable
{
    private readonly World _world;
    private readonly ChessBoardSystem _chessBoardSystem;
    private readonly SaveLoadManager _saveLoadManager;
    private readonly string _tempDir;
    private readonly Entity _gameStateEntity;

    public SaveLoadManagerTests()
    {
        _world = new World();
        _chessBoardSystem = new ChessBoardSystem(_world);
        _tempDir = Path.Combine(Path.GetTempPath(), "TactTest_" + Guid.NewGuid().ToString("N"));
        _saveLoadManager = new SaveLoadManager(_world, _chessBoardSystem, _tempDir);

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

    private Entity AddPiece(PieceType type, Player owner, int file, int rank,
        bool hasNotMoved = true, bool isEnPassant = false)
    {
        var entity = _world.CreateEntity();
        _world.Set(entity, new ChessPiece(type, owner));
        _world.Set(entity, new BoardPosition(file, rank));
        if (hasNotMoved)
            _world.Set(entity, new HasNotMoved());
        if (isEnPassant)
            _world.Set(entity, new EnPassantTarget());
        return entity;
    }

    // ── Section 1: ExtractSaveData ────────────────────────────────────────────

    [Fact]
    public void ExtractSaveData_CapturesCorrectPieceCount()
    {
        AddPiece(PieceType.King,  Player.White, 4, 0);
        AddPiece(PieceType.Queen, Player.White, 3, 0);
        AddPiece(PieceType.Rook,  Player.Black, 0, 7);
        AddPiece(PieceType.Pawn,  Player.Black, 1, 6);

        var saveData = _saveLoadManager.ExtractSaveData();

        Assert.Equal(4, saveData.Pieces.Count);
    }

    [Fact]
    public void ExtractSaveData_CapturesHasNotMoved_True()
    {
        AddPiece(PieceType.King, Player.White, 4, 0, hasNotMoved: true);

        var saveData = _saveLoadManager.ExtractSaveData();
        var kingData = saveData.Pieces.Single(p => p.Type == "King");

        Assert.True(kingData.HasNotMoved);
    }

    [Fact]
    public void ExtractSaveData_CapturesHasNotMoved_False()
    {
        AddPiece(PieceType.Rook, Player.White, 0, 0, hasNotMoved: false);

        var saveData = _saveLoadManager.ExtractSaveData();
        var rookData = saveData.Pieces.Single(p => p.Type == "Rook");

        Assert.False(rookData.HasNotMoved);
    }

    [Fact]
    public void ExtractSaveData_CapturesEnPassantTarget()
    {
        AddPiece(PieceType.Pawn, Player.Black, 3, 4, hasNotMoved: false, isEnPassant: true);

        var saveData = _saveLoadManager.ExtractSaveData();
        var pawnData = saveData.Pieces.Single(p => p.Type == "Pawn");

        Assert.True(pawnData.IsEnPassantTarget);
    }

    [Fact]
    public void ExtractSaveData_CapturesCurrentTurn_White()
    {
        _world.Set(_gameStateEntity, new CurrentTurn(Player.White));

        var saveData = _saveLoadManager.ExtractSaveData();

        Assert.Equal((int)Player.White, saveData.GameState.CurrentTurn);
    }

    [Fact]
    public void ExtractSaveData_CapturesCurrentTurn_Black()
    {
        _world.Set(_gameStateEntity, new CurrentTurn(Player.Black));

        var saveData = _saveLoadManager.ExtractSaveData();

        Assert.Equal((int)Player.Black, saveData.GameState.CurrentTurn);
    }

    [Fact]
    public void ExtractSaveData_CapturesGamePhase()
    {
        _world.Set(_gameStateEntity, new CurrentGamePhase(GamePhase.AIThinking));

        var saveData = _saveLoadManager.ExtractSaveData();

        Assert.Equal("AIThinking", saveData.GameState.Phase);
    }

    [Fact]
    public void ExtractSaveData_CapturesAiConfig()
    {
        _world.Set(_gameStateEntity, new AiConfig(true, Player.Black));

        var saveData = _saveLoadManager.ExtractSaveData();

        Assert.NotNull(saveData.GameState.AiConfig);
        Assert.True(saveData.GameState.AiConfig.IsEnabled);
        Assert.Equal((int)Player.Black, saveData.GameState.AiConfig.AiPlayer);
    }

    [Fact]
    public void ExtractSaveData_PiecePositions_AreCorrect()
    {
        AddPiece(PieceType.Bishop, Player.White, 2, 3);

        var saveData = _saveLoadManager.ExtractSaveData();
        var bishop = saveData.Pieces.Single(p => p.Type == "Bishop");

        Assert.Equal(2, bishop.File);
        Assert.Equal(3, bishop.Rank);
    }

    [Fact]
    public void ExtractSaveData_PieceOwner_IsCorrect()
    {
        AddPiece(PieceType.Knight, Player.Black, 6, 7);

        var saveData = _saveLoadManager.ExtractSaveData();
        var knight = saveData.Pieces.Single(p => p.Type == "Knight");

        Assert.Equal("Black", knight.Owner);
    }

    [Fact]
    public void ExtractSaveData_EmptyWorld_HasNoPieces()
    {
        // No pieces added, only the game-state entity
        var saveData = _saveLoadManager.ExtractSaveData();

        Assert.Empty(saveData.Pieces);
    }

    [Fact]
    public void ExtractSaveData_VersionIsOne()
    {
        var saveData = _saveLoadManager.ExtractSaveData();

        Assert.Equal(1, saveData.Version);
    }

    // ── Section 2: Serialization round-trip ───────────────────────────────────

    [Fact]
    public void SerializeDeserialize_RoundTrip_PreservesAllData()
    {
        var original = new ChessSaveData
        {
            Version = 1,
            GameState = new Tactician.Serialization.GameStateData
            {
                CurrentTurn = (int)Player.Black,
                Phase = "AIThinking",
                AiConfig = new Tactician.Serialization.AiConfigData { IsEnabled = true, AiPlayer = (int)Player.Black }
            },
            Pieces =
            {
                new Tactician.Serialization.PieceData
                {
                    Type = "King", Owner = "White", File = 4, Rank = 0,
                    HasNotMoved = true, IsEnPassantTarget = false
                },
                new Tactician.Serialization.PieceData
                {
                    Type = "Pawn", Owner = "Black", File = 3, Rank = 4,
                    HasNotMoved = false, IsEnPassantTarget = true
                },
                new Tactician.Serialization.PieceData
                {
                    Type = "Rook", Owner = "White", File = 0, Rank = 0,
                    HasNotMoved = false, IsEnPassantTarget = false
                }
            }
        };

        _saveLoadManager.SerializeToJson(original, "roundtrip_test.json");
        var restored = _saveLoadManager.DeserializeFromJson("roundtrip_test.json");

        Assert.NotNull(restored);
        Assert.Equal(original.Version, restored.Version);
        Assert.Equal(original.GameState.CurrentTurn, restored.GameState.CurrentTurn);
        Assert.Equal(original.GameState.Phase, restored.GameState.Phase);
        Assert.NotNull(restored.GameState.AiConfig);
        Assert.Equal(original.GameState.AiConfig.IsEnabled, restored.GameState.AiConfig.IsEnabled);
        Assert.Equal(original.GameState.AiConfig.AiPlayer, restored.GameState.AiConfig.AiPlayer);

        Assert.Equal(3, restored.Pieces.Count);

        var king = restored.Pieces.Single(p => p.Type == "King");
        Assert.Equal("White", king.Owner);
        Assert.Equal(4, king.File);
        Assert.Equal(0, king.Rank);
        Assert.True(king.HasNotMoved);

        var pawn = restored.Pieces.Single(p => p.Type == "Pawn");
        Assert.True(pawn.IsEnPassantTarget);
        Assert.False(pawn.HasNotMoved);

        var rook = restored.Pieces.Single(p => p.Type == "Rook");
        Assert.False(rook.HasNotMoved);
    }

    [Fact]
    public void SerializeToJson_CreatesFile()
    {
        var saveData = _saveLoadManager.ExtractSaveData();
        _saveLoadManager.SerializeToJson(saveData, "exists_check.json");

        Assert.True(File.Exists(Path.Combine(_tempDir, "exists_check.json")));
    }

    [Fact]
    public void SerializeToJson_ProducesValidJson()
    {
        var saveData = _saveLoadManager.ExtractSaveData();
        _saveLoadManager.SerializeToJson(saveData, "valid_json.json");

        var raw = File.ReadAllText(Path.Combine(_tempDir, "valid_json.json"));

        // Should not throw
        using var doc = JsonDocument.Parse(raw);
        Assert.True(doc.RootElement.TryGetProperty("version", out _));
    }

    // ── Section 3: DeserializeFromJson error handling ─────────────────────────

    [Fact]
    public void DeserializeFromJson_MissingFile_ReturnsNull()
    {
        var result = _saveLoadManager.DeserializeFromJson("nonexistent_slot_99.json");

        Assert.Null(result);
    }

    [Fact]
    public void DeserializeFromJson_CorruptJson_ReturnsNull()
    {
        var corruptPath = Path.Combine(_tempDir, "corrupt.json");
        File.WriteAllText(corruptPath, "{ this is not valid json !!!");

        var result = _saveLoadManager.DeserializeFromJson("corrupt.json");

        Assert.Null(result);
    }

    [Fact]
    public void DeserializeFromJson_EmptyFile_ReturnsNull()
    {
        var emptyPath = Path.Combine(_tempDir, "empty.json");
        File.WriteAllText(emptyPath, "");

        var result = _saveLoadManager.DeserializeFromJson("empty.json");

        Assert.Null(result);
    }

    // ── Section 4: Slot management ────────────────────────────────────────────

    [Fact]
    public void LoadFromSlot_NonexistentSlot_ReturnsNull()
    {
        // Temp dir has no save files
        var result = _saveLoadManager.LoadFromSlot(3);

        Assert.Null(result);
    }

    [Fact]
    public void LoadFromSlot_AfterSave_ReturnsData()
    {
        AddPiece(PieceType.King, Player.White, 4, 0);
        _world.Set(_gameStateEntity, new CurrentTurn(Player.Black));

        _saveLoadManager.SaveToSlot(5);
        var loaded = _saveLoadManager.LoadFromSlot(5);

        Assert.NotNull(loaded);
        Assert.Equal((int)Player.Black, loaded.GameState.CurrentTurn);
    }

    [Fact]
    public void SaveAndLoad_MultipleSlots_AreIndependent()
    {
        // Save White's turn to slot 1
        _world.Set(_gameStateEntity, new CurrentTurn(Player.White));
        _saveLoadManager.SaveToSlot(1);

        // Save Black's turn to slot 2
        _world.Set(_gameStateEntity, new CurrentTurn(Player.Black));
        _saveLoadManager.SaveToSlot(2);

        var slot1 = _saveLoadManager.LoadFromSlot(1);
        var slot2 = _saveLoadManager.LoadFromSlot(2);

        Assert.NotNull(slot1);
        Assert.NotNull(slot2);
        Assert.Equal((int)Player.White, slot1.GameState.CurrentTurn);
        Assert.Equal((int)Player.Black, slot2.GameState.CurrentTurn);
    }

    [Fact]
    public void QuickSave_And_QuickLoad_WorkCorrectly()
    {
        AddPiece(PieceType.Queen, Player.White, 3, 0);
        _world.Set(_gameStateEntity, new CurrentTurn(Player.Black));

        _saveLoadManager.QuickSave();
        var loaded = _saveLoadManager.DeserializeFromJson("quicksave.json");

        Assert.NotNull(loaded);
        Assert.Equal((int)Player.Black, loaded.GameState.CurrentTurn);
        Assert.Single(loaded.Pieces);
        Assert.Equal("Queen", loaded.Pieces[0].Type);
    }

    [Fact]
    public void SaveToSlot_OverwritesPreviousSave()
    {
        _world.Set(_gameStateEntity, new CurrentTurn(Player.White));
        _saveLoadManager.SaveToSlot(7);

        _world.Set(_gameStateEntity, new CurrentTurn(Player.Black));
        _saveLoadManager.SaveToSlot(7); // overwrite

        var loaded = _saveLoadManager.LoadFromSlot(7);

        Assert.NotNull(loaded);
        Assert.Equal((int)Player.Black, loaded.GameState.CurrentTurn);
    }

    [Fact]
    public void ExtractSaveData_Then_Serialize_PreservesPieceCount()
    {
        AddPiece(PieceType.King,  Player.White, 4, 0);
        AddPiece(PieceType.King,  Player.Black, 4, 7);
        AddPiece(PieceType.Pawn,  Player.White, 0, 1);
        AddPiece(PieceType.Pawn,  Player.Black, 0, 6);
        AddPiece(PieceType.Rook,  Player.White, 0, 0);

        var saveData = _saveLoadManager.ExtractSaveData();
        _saveLoadManager.SerializeToJson(saveData, "piece_count.json");
        var loaded = _saveLoadManager.DeserializeFromJson("piece_count.json");

        Assert.NotNull(loaded);
        Assert.Equal(5, loaded.Pieces.Count);
    }
}
