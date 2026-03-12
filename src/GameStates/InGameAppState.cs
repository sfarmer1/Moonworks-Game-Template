using System;
using MoonTools.ECS;
using MoonWorks;
using MoonWorks.Input;
using Tactician.AI;
using Tactician.Components;
using Tactician.Messages;
using Tactician.Serialization;
using Tactician.Systems;
using Tactician_Graphics_Renderer = Tactician.Graphics.Renderer;

namespace Tactician.GameStates;

public class InGameAppState : AppState
{
	private readonly App _app;

	private AudioSystem                 _audioSystem;
	private GamepadInputSystem          _gamepadInputSystem;
	private CursorSystem                _cursorSystem;
	private Tactician_Graphics_Renderer _renderer;
	private SpriteAnimationSystem       _spriteAnimationSystem;
	private AppState                    _transitionState;
	private World                       _world;
	private Filter						_resettableEntitiesFilter;

	// Chess systems
	private ChessBoardSystem            _chessBoardSystem;
	private MoveValidationSystem        _moveValidationSystem;
	private ChessTurnSystem             _chessTurnSystem;
	private ChessMoveExecutionSystem    _chessMoveExecutionSystem;
	private ChessInputSystem            _chessInputSystem;
	private ChessHighlightSystem        _chessHighlightSystem;
	private ChessAISystem               _chessAISystem;

	// Save/Load
	private SaveLoadManager             _saveLoadManager;

	public InGameAppState(App app, AppState transitionState)
	{
		_app             = app;
		_transitionState = transitionState;
	}

	public override void Start()
	{
		_world = new World();

		// Initialize core systems
		_gamepadInputSystem = new GamepadInputSystem(_world, _app.Inputs);
		_audioSystem        = new AudioSystem(_world, _app.AudioDevice);
		_cursorSystem       = new CursorSystem(_world);
		_spriteAnimationSystem = new SpriteAnimationSystem(_world);

		// Initialize chess systems
		_chessBoardSystem = new ChessBoardSystem(_world);
		_moveValidationSystem = new MoveValidationSystem(_world, _chessBoardSystem);
		_chessTurnSystem = new ChessTurnSystem(_world, _chessBoardSystem, _moveValidationSystem);
		_chessMoveExecutionSystem = new ChessMoveExecutionSystem(_world, _chessBoardSystem, _chessTurnSystem);
		_chessInputSystem = new ChessInputSystem(_world, _chessBoardSystem, _moveValidationSystem, _app.Inputs);
		_chessHighlightSystem = new ChessHighlightSystem(_world);
		_chessAISystem = new ChessAISystem(_world);

		_renderer = new Tactician_Graphics_Renderer(_world, _app.GraphicsDevice, _app.RootTitleStorage,
													_app.MainWindow.SwapchainFormat);

		_resettableEntitiesFilter = _world.FilterBuilder.Include<DestroyedOnReset>().Build();

		// Initialize save/load manager
		_saveLoadManager = new SaveLoadManager(_world, _chessBoardSystem);

		InitializeEntities();

		_world.Send(new PlaySongMessage());
	}

	private void InitializeEntities()
	{
		// Initialize chess board and pieces (creates entities with DestroyedOnReset)
		_chessBoardSystem.InitializeBoard();
		_chessBoardSystem.SpawnInitialPieces();

		// Create game state entity
		var gameStateEntity = _world.CreateEntity();
		_chessTurnSystem.InitializeGameState(gameStateEntity);
		_world.Set(gameStateEntity, new DestroyedOnReset()); // Mark for destruction on reset

		// Set up AI (optional - enable for AI vs player mode)
		var randomAi = new MinimaxAI(_world, _chessBoardSystem, _moveValidationSystem, Player.Black);
		_chessAISystem.SetAI(randomAi);

		// Enable AI for Black player
		_world.Set(gameStateEntity, new AiConfig(true, Player.Black));

		var gameInProgressEntity = _world.CreateEntity();
		_world.Set(gameInProgressEntity, new GameInProgress());
		_world.Set(gameInProgressEntity, new DestroyedOnReset());
	}

	public override void Update(TimeSpan dt)
	{
		// Check if shift is held
		var shiftHeld = _app.Inputs.Keyboard.IsDown(KeyCode.LeftShift) || _app.Inputs.Keyboard.IsDown(KeyCode.RightShift);

		// Handle save/load input for slots 1-10 (keys 1-9 and 0)
		// Key 0 maps to slot 10
		var numberKeys = new (KeyCode key, int slot)[]
		{
			(KeyCode.D1, 1),
			(KeyCode.D2, 2),
			(KeyCode.D3, 3),
			(KeyCode.D4, 4),
			(KeyCode.D5, 5),
			(KeyCode.D6, 6),
			(KeyCode.D7, 7),
			(KeyCode.D8, 8),
			(KeyCode.D9, 9),
			(KeyCode.D0, 10)
		};

		foreach (var (key, slot) in numberKeys)
		{
			if (_app.Inputs.Keyboard.IsPressed(key))
			{
				if (shiftHeld)
				{
					// Shift+Number = Save
					MoonWorks.Logger.LogInfo($"Save to slot {slot} requested");
					_saveLoadManager.SaveToSlot(slot);
				}
				else
				{
					// Number = Load
					MoonWorks.Logger.LogInfo($"Load from slot {slot} requested");
					_world.FinishUpdate();
					LoadGame(slot);
					return;
				}
			}
		}

		// Keep F5/F9 for backward compatibility (uses slot 0/quicksave)
		if (_app.Inputs.Keyboard.IsPressed(KeyCode.F5))
		{
			MoonWorks.Logger.LogInfo("Quick save requested (F5)");
			_saveLoadManager.QuickSave();
		}

		if (_app.Inputs.Keyboard.IsPressed(KeyCode.F9))
		{
			MoonWorks.Logger.LogInfo("Quick load requested (F9)");
			_world.FinishUpdate();
			LoadGame(0);
			return;
		}

		// Update systems in order
		_gamepadInputSystem.Update(dt);
		_cursorSystem.Update(dt);
		_chessInputSystem.Update(dt);
		_chessTurnSystem.Update(dt);
		_chessAISystem.Update(dt);
		_chessMoveExecutionSystem.Update(dt);
		_chessHighlightSystem.Update(dt);
		_spriteAnimationSystem.Update(dt);
		_audioSystem.Update(dt);

		if (_world.SomeMessage<ResetGameMessage>())
		{
			_world.FinishUpdate();
			ResetGame();
			return;
		}

		if (_world.SomeMessage<EndGame>())
		{
			_world.FinishUpdate();
			_audioSystem.Cleanup();
			_world.Dispose();
			_app.SetState(_transitionState);
			return;
		}

		_world.FinishUpdate();
	}

	public override void Draw(Window window, double alpha)
	{
		_renderer.Render(_app.MainWindow);
	}

	public override void End() { }

	public void SetTransitionState(AppState state)
	{
		_transitionState = state;
	}

	private void ResetGame()
	{
		MoonWorks.Logger.LogInfo("Resetting game...");

		// Destroy all entities marked with DestroyedOnReset component
		var entityCount = 0;
		foreach (var entity in _resettableEntitiesFilter.Entities)
		{
			_world.Destroy(entity);
			entityCount++;
		}
		MoonWorks.Logger.LogInfo($"Destroyed {entityCount} entities");

		// Clear the board system's internal tracking arrays
		_chessBoardSystem.ClearBoard();

		// Recreate all game entities
		InitializeEntities();

		MoonWorks.Logger.LogInfo("Game reset complete");
	}

	private void LoadGame(int slot)
	{
		MoonWorks.Logger.LogInfo($"Loading game from slot {slot}...");

		var saveData = _saveLoadManager.LoadFromSlot(slot);
		if (saveData == null)
		{
			MoonWorks.Logger.LogWarn($"Load failed - no save file found for slot {slot}");
			return;
		}

		// Destroy all entities marked with DestroyedOnReset component
		var entityCount = 0;
		foreach (var entity in _resettableEntitiesFilter.Entities)
		{
			_world.Destroy(entity);
			entityCount++;
		}
		MoonWorks.Logger.LogInfo($"Destroyed {entityCount} entities for load");

		// Clear the board system's internal tracking arrays
		_chessBoardSystem.ClearBoard();

		// Initialize board squares and cursor (but not pieces)
		_chessBoardSystem.InitializeBoardSquaresOnly();

		// Create game state entity
		var gameStateEntity = _world.CreateEntity();
		_chessTurnSystem.InitializeGameState(gameStateEntity);
		_world.Set(gameStateEntity, new DestroyedOnReset());

		// Set up AI
		var randomAi = new MinimaxAI(_world, _chessBoardSystem, _moveValidationSystem, Player.Black);
		_chessAISystem.SetAI(randomAi);

		var gameInProgressEntity = _world.CreateEntity();
		_world.Set(gameInProgressEntity, new GameInProgress());
		_world.Set(gameInProgressEntity, new DestroyedOnReset());

		// Apply save data
		_saveLoadManager.ApplySaveData(saveData);

		MoonWorks.Logger.LogInfo("Game load complete");
	}
}
