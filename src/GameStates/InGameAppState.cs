using System;
using MoonTools.ECS;
using MoonWorks;
using Tactician.AI;
using Tactician.Components;
using Tactician.Messages;
using Tactician.Singletons;
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

		// Initialize chess board and pieces
		_chessBoardSystem.InitializeBoard();
		_chessBoardSystem.SpawnInitialPieces();

		// Create game state entity
		var gameStateEntity = _world.CreateEntity();
		_chessTurnSystem.InitializeGameState(gameStateEntity);

		// Set up AI (optional - enable for AI vs player mode)
		var randomAI = new MinimaxAI(_world, _chessBoardSystem, _moveValidationSystem, Player.Black);
		_chessAISystem.SetAI(randomAI);

		// Enable AI for Black player
		_world.Set(gameStateEntity, new AiConfig(true, Player.Black));

		var gameInProgressEntity = _world.CreateEntity();
		_world.Set(gameInProgressEntity, new GameInProgress());

		_world.Send(new PlaySongMessage());
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
}
