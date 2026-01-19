using System;
using MoonTools.ECS;
using MoonWorks;
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

	public InGameAppState(App app, AppState transitionState)
	{
		_app             = app;
		_transitionState = transitionState;
	}

	public override void Start()
	{
		_world = new World();

		_gamepadInputSystem = new GamepadInputSystem(_world, _app.Inputs);
		_audioSystem        = new AudioSystem(_world, _app.AudioDevice);
		_cursorSystem          = new CursorSystem(_world);
		_spriteAnimationSystem = new SpriteAnimationSystem(_world);

		_renderer = new Tactician_Graphics_Renderer(_world, _app.GraphicsDevice, _app.RootTitleStorage,
													_app.MainWindow.SwapchainFormat);

		var gameInProgressEntity = _world.CreateEntity();
		_world.Set(gameInProgressEntity, new GameInProgress());
		var prefabSpawner = new PrefabSpawner(_world);
		prefabSpawner.SpawnLevel_1();

		_world.Send(new PlaySongMessage());
	}

	public override void Update(TimeSpan dt)
	{
		_gamepadInputSystem.Update(dt);
		_cursorSystem.Update(dt);
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