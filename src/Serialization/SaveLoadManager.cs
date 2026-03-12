using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using MoonTools.ECS;
using Tactician.Components;
using Tactician.Data;
using Tactician.Systems;

namespace Tactician.Serialization;

/// <summary>
/// Manages saving and loading chess game state to/from JSON files.
/// </summary>
public class SaveLoadManager
{
	private readonly World _world;
	private readonly ChessBoardSystem _chessBoardSystem;
	private readonly string _saveDirectory;
	private readonly Filter _gameStateFilter;
	private readonly Filter _pieceFilter;

	private static readonly JsonSerializerOptions JsonOptions = new()
	{
		WriteIndented = true,
		Converters = { new JsonStringEnumConverter() }
	};

	public SaveLoadManager(World world, ChessBoardSystem chessBoardSystem)
	{
		_world = world;
		_chessBoardSystem = chessBoardSystem;

		// Use platform-appropriate save directory
		var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
		_saveDirectory = Path.Combine(appDataPath, "Tactician", "saves");

		// Ensure directory exists
		Directory.CreateDirectory(_saveDirectory);

		// Create filters once and store them
		_gameStateFilter = world.FilterBuilder
			.Include<ChessGameState>()
			.Build();

		_pieceFilter = world.FilterBuilder
			.Include<ChessPiece>()
			.Include<BoardPosition>()
			.Build();
	}

	/// <summary>
	/// Constructor for testing — accepts a custom save directory to avoid touching the real save path.
	/// </summary>
	internal SaveLoadManager(World world, ChessBoardSystem chessBoardSystem, string saveDirectory)
	{
		_world = world;
		_chessBoardSystem = chessBoardSystem;
		_saveDirectory = saveDirectory;

		Directory.CreateDirectory(_saveDirectory);

		_gameStateFilter = world.FilterBuilder
			.Include<ChessGameState>()
			.Build();

		_pieceFilter = world.FilterBuilder
			.Include<ChessPiece>()
			.Include<BoardPosition>()
			.Build();
	}

	/// <summary>
	/// Extracts current game state into a ChessSaveData structure.
	/// </summary>
	public ChessSaveData ExtractSaveData()
	{
		var saveData = new ChessSaveData
		{
			Version = 1,
			SavedAt = DateTime.UtcNow
		};

		// Extract game state components
		Entity? gameStateEntity = null;
		foreach (var entity in _gameStateFilter.Entities)
		{
			gameStateEntity = entity;
			break;
		}

		if (gameStateEntity.HasValue)
		{
			var gameState = gameStateEntity.Value;

			// Extract turn
			if (_world.Has<CurrentTurn>(gameState))
			{
				var turn = _world.Get<CurrentTurn>(gameState);
				saveData.GameState.CurrentTurn = (int)turn.Player;
			}

			// Extract phase
			if (_world.Has<CurrentGamePhase>(gameState))
			{
				var phase = _world.Get<CurrentGamePhase>(gameState);
				saveData.GameState.Phase = phase.Phase.ToString();
			}

			// Extract AI config
			if (_world.Has<AiConfig>(gameState))
			{
				var aiConfig = _world.Get<AiConfig>(gameState);
				saveData.GameState.AiConfig = new AiConfigData
				{
					IsEnabled = aiConfig.IsEnabled,
					AiPlayer = (int)aiConfig.AiPlayer
				};
			}
		}

		// Extract piece data
		MoonWorks.Logger.LogInfo($"ExtractSaveData: Found {_pieceFilter.Count} pieces to save");

		foreach (var pieceEntity in _pieceFilter.Entities)
		{
			var chessPiece = _world.Get<ChessPiece>(pieceEntity);
			var boardPos = _world.Get<BoardPosition>(pieceEntity);

			var pieceData = new PieceData
			{
				Type = chessPiece.Type.ToString(),
				Owner = chessPiece.Owner.ToString(),
				File = boardPos.File,
				Rank = boardPos.Rank,
				HasNotMoved = _world.Has<HasNotMoved>(pieceEntity),
				IsEnPassantTarget = _world.Has<EnPassantTarget>(pieceEntity)
			};

			saveData.Pieces.Add(pieceData);
			MoonWorks.Logger.LogInfo($"  Saved {chessPiece.Owner} {chessPiece.Type} at ({boardPos.File}, {boardPos.Rank})");
		}

		MoonWorks.Logger.LogInfo($"ExtractSaveData: Saved {saveData.Pieces.Count} pieces total");
		return saveData;
	}

	/// <summary>
	/// Serializes save data to a JSON file.
	/// </summary>
	public void SerializeToJson(ChessSaveData saveData, string fileName = "quicksave.json")
	{
		var filePath = Path.Combine(_saveDirectory, fileName);
		var json = JsonSerializer.Serialize(saveData, JsonOptions);
		File.WriteAllText(filePath, json);

		MoonWorks.Logger.LogInfo($"Game saved to: {filePath}");
	}

	/// <summary>
	/// Deserializes save data from a JSON file.
	/// </summary>
	public ChessSaveData DeserializeFromJson(string fileName = "quicksave.json")
	{
		var filePath = Path.Combine(_saveDirectory, fileName);

		if (!File.Exists(filePath))
		{
			MoonWorks.Logger.LogWarn($"Save file not found: {filePath}");
			return null;
		}

		try
		{
			var json = File.ReadAllText(filePath);
			var saveData = JsonSerializer.Deserialize<ChessSaveData>(json, JsonOptions);

			MoonWorks.Logger.LogInfo($"Game loaded from: {filePath}");
			return saveData;
		}
		catch (Exception ex)
		{
			MoonWorks.Logger.LogError($"Failed to load save file: {ex.Message}");
			return null;
		}
	}

	/// <summary>
	/// Applies save data to the current world state.
	/// Assumes board squares and cursor have already been initialized.
	/// </summary>
	public void ApplySaveData(ChessSaveData saveData)
	{
		// Get game state entity
		Entity? gameStateEntity = null;
		foreach (var entity in _gameStateFilter.Entities)
		{
			gameStateEntity = entity;
			break;
		}

		if (!gameStateEntity.HasValue)
		{
			MoonWorks.Logger.LogError("No ChessGameState entity found!");
			return;
		}

		var gameState = gameStateEntity.Value;

		// Restore game state components
		_world.Set(gameState, new CurrentTurn((Player)saveData.GameState.CurrentTurn));

		if (Enum.TryParse<GamePhase>(saveData.GameState.Phase, out var phase))
		{
			_world.Set(gameState, new CurrentGamePhase(phase));
		}

		if (saveData.GameState.AiConfig != null)
		{
			_world.Set(gameState, new AiConfig(
				saveData.GameState.AiConfig.IsEnabled,
				(Player)saveData.GameState.AiConfig.AiPlayer
			));
		}

		// Remove all existing pieces
		foreach (var piece in _pieceFilter.Entities)
		{
			_world.Destroy(piece);
		}

		// Spawn pieces from save data
		foreach (var pieceData in saveData.Pieces)
		{
			if (!Enum.TryParse<PieceType>(pieceData.Type, out var pieceType))
			{
				MoonWorks.Logger.LogWarn($"Unknown piece type: {pieceData.Type}");
				continue;
			}

			if (!Enum.TryParse<Player>(pieceData.Owner, out var owner))
			{
				MoonWorks.Logger.LogWarn($"Unknown player: {pieceData.Owner}");
				continue;
			}

			var boardPos = new BoardPosition(pieceData.File, pieceData.Rank);
			var piece = _chessBoardSystem.SpawnPieceAt(pieceType, owner, boardPos);

			// Remove HasNotMoved if piece has moved
			if (!pieceData.HasNotMoved && _world.Has<HasNotMoved>(piece))
			{
				_world.Remove<HasNotMoved>(piece);
			}

			// Add EnPassantTarget if needed
			if (pieceData.IsEnPassantTarget)
			{
				_world.Set(piece, new EnPassantTarget());
			}
		}

		MoonWorks.Logger.LogInfo($"Loaded {saveData.Pieces.Count} pieces");
	}

	/// <summary>
	/// Gets the filename for a save slot.
	/// Slot 0 is "quicksave.json", slots 1-10 are "save_slot_N.json"
	/// </summary>
	private string GetSlotFileName(int slot)
	{
		return slot == 0 ? "quicksave.json" : $"save_slot_{slot}.json";
	}

	/// <summary>
	/// Save to a specific slot (1-10).
	/// Keys 1-9 map to slots 1-9, key 0 maps to slot 10.
	/// </summary>
	public void SaveToSlot(int slot)
	{
		if (slot < 1 || slot > 10)
		{
			MoonWorks.Logger.LogError($"Invalid save slot: {slot}. Must be 1-10.");
			return;
		}

		var saveData = ExtractSaveData();
		var fileName = GetSlotFileName(slot);
		SerializeToJson(saveData, fileName);
		MoonWorks.Logger.LogInfo($"Saved to slot {slot}");
	}

	/// <summary>
	/// Load from a specific slot (0 for quicksave, 1-10 for numbered slots).
	/// Keys 1-9 map to slots 1-9, key 0 maps to slot 10.
	/// Returns the loaded save data, or null if load failed.
	/// </summary>
	public ChessSaveData LoadFromSlot(int slot)
	{
		if (slot < 0 || slot > 10)
		{
			MoonWorks.Logger.LogError($"Invalid load slot: {slot}. Must be 0-10.");
			return null;
		}

		var fileName = GetSlotFileName(slot);
		var saveData = DeserializeFromJson(fileName);

		if (saveData != null)
		{
			MoonWorks.Logger.LogInfo($"Loaded from slot {slot}");
		}

		return saveData;
	}

	/// <summary>
	/// Quick save to default file (F5 key).
	/// </summary>
	public void QuickSave()
	{
		var saveData = ExtractSaveData();
		SerializeToJson(saveData, "quicksave.json");
	}

	/// <summary>
	/// Quick load from default file (F9 key).
	/// Returns true if successful.
	/// </summary>
	public bool QuickLoad()
	{
		var saveData = DeserializeFromJson("quicksave.json");
		if (saveData == null)
		{
			return false;
		}

		ApplySaveData(saveData);
		return true;
	}
}
