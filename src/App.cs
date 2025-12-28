using System;
using System.IO;
using MoonWorks;
using MoonWorks.Graphics;
using MoonWorks.Input;
using SDL3;
using Tactician.Content;
using Tactician.GameStates;

namespace Tactician;

public class App : Game 
{
    private AppState _currentState;

    public App(
        AppInfo appInfo,
        WindowCreateInfo windowCreateInfo,
        FramePacingSettings framePacingSettings,
        ShaderFormat shaderFormats,
        bool debugMode
    ) : base(appInfo, windowCreateInfo, framePacingSettings, shaderFormats, debugMode) 
    {
        TextureAtlases.Init(GraphicsDevice);
        StaticAudioPacks.Init(AudioDevice);
        StreamingAudio.Init(AudioDevice);
        Fonts.LoadAll(GraphicsDevice, RootTitleStorage);

        var gameLoopAppState = new GameLoopAppState(this, null);
        var loadState = new LoadingAppState(this, gameLoopAppState);
        gameLoopAppState.SetTransitionState(loadState);

        SetState(loadState);
    }

    protected override void Update(TimeSpan dt) 
    {
        if (Inputs.Keyboard.IsPressed(KeyCode.F11)) 
        {
            if (MainWindow.ScreenMode == ScreenMode.Fullscreen)
                MainWindow.SetScreenMode(ScreenMode.Windowed);
            else
                MainWindow.SetScreenMode(ScreenMode.Fullscreen);
        }

        _currentState.Update(dt);
    }

    protected override void Draw(double alpha) 
    {
        _currentState.Draw(MainWindow, alpha);
    }

    protected override void Destroy() {
    }

    public void SetState(AppState appState) 
    {
        _currentState?.End();

        appState.Start();
        _currentState = appState;
    }
}