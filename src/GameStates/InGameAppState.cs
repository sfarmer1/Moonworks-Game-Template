using System;
using MoonTools.ECS;
using MoonWorks;
using Tactician.AI;
using Tactician.Components;
using Tactician.Messages;
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
}
