using System;
using MoonWorks;
using MoonWorks.Graphics;
using MoonWorks.Input;
using Tactician.Content;
using Tactician.GameStates;

namespace Tactician;

public class TacticianGame : Game {
    private GameState _currentState;

    public TacticianGame(
        AppInfo appInfo,
        WindowCreateInfo windowCreateInfo,
        FramePacingSettings framePacingSettings,
        ShaderFormat shaderFormats,
        bool debugMode
    ) : base(appInfo, windowCreateInfo, framePacingSettings, shaderFormats, debugMode) {
        Inputs.Mouse.Hide();

        TextureAtlases.Init(GraphicsDevice);
        StaticAudioPacks.Init(AudioDevice);
        StreamingAudio.Init(AudioDevice);
        Fonts.LoadAll(GraphicsDevice, RootTitleStorage);

        var gameplayState = new GameplayState(this, null);
        var loadState = new LoadState(this, gameplayState);
        var howToPlayState = new HowToPlayState(this, gameplayState);
        gameplayState.SetTransitionState(howToPlayState); // i hate this

        SetState(loadState);
    }

    protected override void Update(TimeSpan dt) {
        if (Inputs.Keyboard.IsPressed(KeyCode.F11)) {
            if (MainWindow.ScreenMode == ScreenMode.Fullscreen)
                MainWindow.SetScreenMode(ScreenMode.Windowed);
            else
                MainWindow.SetScreenMode(ScreenMode.Fullscreen);
        }

        _currentState.Update(dt);
    }

    protected override void Draw(double alpha) {
        _currentState.Draw(MainWindow, alpha);
    }

    protected override void Destroy() {
    }

    public void SetState(GameState gameState) {
        _currentState?.End();

        gameState.Start();
        _currentState = gameState;
    }
}