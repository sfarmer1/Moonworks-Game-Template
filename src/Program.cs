using System;
using System.IO;
using MoonWorks;
using MoonWorks.Graphics;

namespace Tactician;

internal class Program {
	private static readonly string UserDataDirectory =
        $"{Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Tactician")}";

    private static void Main(string[] args) {
        if (!Directory.Exists(UserDataDirectory)) Directory.CreateDirectory(UserDataDirectory);

        AppDomain.CurrentDomain.UnhandledException += HandleUnhandledException;

        var debugMode = false;
        
#if DEBUG
        var windowCreateInfo = new WindowCreateInfo {
            WindowWidth = 640,
            WindowHeight = 360,
            WindowTitle = "Tactician",
            ScreenMode = ScreenMode.Windowed
        };
        debugMode = true;
#else
			WindowCreateInfo windowCreateInfo = new WindowCreateInfo {
				WindowWidth = 640,
				WindowHeight = 360,
				WindowTitle = "Tactician",
				ScreenMode = ScreenMode.Fullscreen
			};
#endif

        var framePacingSettings = FramePacingSettings.CreateLatencyOptimized(60);

		var appInfo = new AppInfo("TEMPLATE_ORGANIZATION", "Tactician");
		var game = new global::Tactician.TacticianGame(
			appInfo,
			windowCreateInfo,
			framePacingSettings,
			ShaderFormat.SPIRV | ShaderFormat.DXBC | ShaderFormat.MSL,
			debugMode
		);

        game.Run();
    }

    private static void HandleUnhandledException(object sender, UnhandledExceptionEventArgs args) {
        var e = (Exception)args.ExceptionObject;
        Logger.LogError("Unhandled exception caught!");
        Logger.LogError(e.ToString());

        Game.ShowRuntimeError("FLAGRANT SYSTEM ERROR", e.ToString());

        var streamWriter = new StreamWriter(Path.Combine(UserDataDirectory, "log.txt"));

        streamWriter.WriteLine(e.ToString());
        streamWriter.Flush();
        streamWriter.Close();
    }
}