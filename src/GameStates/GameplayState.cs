using System;
using MoonTools.ECS;
using MoonWorks;
using Tactician.Content;
using Tactician.Components;
using Tactician.Messages;
using Tactician.Systems;
using Graphics_Renderer = Tactician.Graphics.Renderer;
using Renderer = Tactician.Graphics.Renderer;
using Tactician_Graphics_Renderer = Tactician.Graphics.Renderer;

namespace Tactician.GameStates;

public class GameplayState : GameState {
    private readonly TacticianGame _game;
    private AudioSystem _audioSystem;
    private ColorAnimationSystem _colorAnimationSystem;
    private DirectionalAnimationSystem _directionalAnimationSystem;
    private InputSystem _inputSystem;
    private MotionSystem _motionSystem;
    private PlayerControllerSystem _playerControllerSystem;

    private Tactician_Graphics_Renderer _renderer;
    private SetSpriteAnimationSystem _setSpriteAnimationSystem;
    private GameState _transitionState;
    private UpdateSpriteAnimationSystem _updateSpriteAnimationSystem;
    private World _world;

    public GameplayState(TacticianGame game, GameState transitionState) {
        _game = game;
        _transitionState = transitionState;
    }

    public override void Start() {
        _world = new World();

        _inputSystem = new InputSystem(_world, _game.Inputs);
        _motionSystem = new MotionSystem(_world);
        _audioSystem = new AudioSystem(_world, _game.AudioDevice);
        _playerControllerSystem = new PlayerControllerSystem(_world);
        _setSpriteAnimationSystem = new SetSpriteAnimationSystem(_world);
        _updateSpriteAnimationSystem = new UpdateSpriteAnimationSystem(_world);
        _colorAnimationSystem = new ColorAnimationSystem(_world);
        _directionalAnimationSystem = new DirectionalAnimationSystem(_world);

        _renderer = new Tactician_Graphics_Renderer(_world, _game.GraphicsDevice, _game.RootTitleStorage, _game.MainWindow.SwapchainFormat);

        var topBorder = _world.CreateEntity();
        _world.Set(topBorder, new Position(0, 65));
        _world.Set(topBorder, new Rectangle(0, 0, Dimensions.GAME_W, 10));
        _world.Set(topBorder, new Solid());

        var leftBorder = _world.CreateEntity();
        _world.Set(leftBorder, new Position(-10, 0));
        _world.Set(leftBorder, new Rectangle(0, 0, 10, Dimensions.GAME_H));
        _world.Set(leftBorder, new Solid());

        var rightBorder = _world.CreateEntity();
        _world.Set(rightBorder, new Position(Dimensions.GAME_W, 0));
        _world.Set(rightBorder, new Rectangle(0, 0, 10, Dimensions.GAME_H));
        _world.Set(rightBorder, new Solid());

        var bottomBorder = _world.CreateEntity();
        _world.Set(bottomBorder, new Position(0, Dimensions.GAME_H));
        _world.Set(bottomBorder, new Rectangle(0, 0, Dimensions.GAME_W, 10));
        _world.Set(bottomBorder, new Solid());

        var background = _world.CreateEntity();
        _world.Set(background, new Position(0, 0));
        _world.Set(background, new Depth(999));
        _world.Set(background, new SpriteAnimation(SpriteAnimations.BG, 0));

        var uiBottomBackground = _world.CreateEntity();
        _world.Set(uiBottomBackground, new Position(0, Dimensions.GAME_H - 40));
        _world.Set(uiBottomBackground, new Depth(9));
        _world.Set(uiBottomBackground, new SpriteAnimation(SpriteAnimations.HUD_Bottom, 0));


        var scoreOne = _world.CreateEntity();
        _world.Set(scoreOne, new Position(80, 345));
        _world.Set(scoreOne, new Score(0));
        _world.Set(scoreOne, new DisplayScore(0));
        _world.Set(scoreOne, new Text(Fonts.KosugiID, FontSizes.SCORE, "0"));

        var scoreTwo = _world.CreateEntity();
        _world.Set(scoreTwo, new Position(560, 345));
        _world.Set(scoreTwo, new Score(0));
        _world.Set(scoreTwo, new DisplayScore(0));
        _world.Set(scoreTwo, new Text(Fonts.KosugiID, FontSizes.SCORE, "0"));

        var playerOne = _playerControllerSystem.SpawnPlayer(0);
        var playerTwo = _playerControllerSystem.SpawnPlayer(1);

        _world.Relate(playerOne, scoreOne, new HasScore());
        _world.Relate(playerTwo, scoreTwo, new HasScore());

        var gameInProgressEntity = _world.CreateEntity();
        _world.Set(gameInProgressEntity, new GameInProgress());

        _world.Send(new PlaySongMessage());
    }

    public override void Update(TimeSpan dt) {
        _updateSpriteAnimationSystem.Update(dt);
        _inputSystem.Update(dt);
        _playerControllerSystem.Update(dt);
        _motionSystem.Update(dt);
        _directionalAnimationSystem.Update(dt);
        _setSpriteAnimationSystem.Update(dt);
        _colorAnimationSystem.Update(dt);
        _audioSystem.Update(dt);

        if (_world.SomeMessage<EndGame>()) {
            _world.FinishUpdate();
            _audioSystem.Cleanup();
            _world.Dispose();
            _game.SetState(_transitionState);
            return;
        }

        _world.FinishUpdate();
    }

    public override void Draw(Window window, double alpha) {
        _renderer.Render(_game.MainWindow);
    }

    public override void End() {
    }

    public void SetTransitionState(GameState state) {
        _transitionState = state;
    }
}