# MOONWORKS GAME TEMPLATE

This template is a stripped down version of [ROLL AND CASH: GROCERY LORDS](https://github.com/thatcosmonaut/GGJ2024), a game made in 48 hours for Global Game Jam 2024 using the [MoonWorks](https://github.com/MoonsideGames/MoonWorks) game framework.

The goals of this template are to:
- Make it easier to get started making games with MoonWorks
- Provide a bare bones example of how to organize a project with Just Enough structure to get to the fun part quickly
- Make it easy to rip out the example code and make it your own
- Make a template that easily runs on on Windows, Linux, and MacOS ARM chips (M1, M2, etc)
- Clean up the C# formatting in the original Roll and Cash template project.
- Stay up to date with all dependencies
- Implement an in-game ImGui inspector as an example of how to build your tooling into the game, itself. 


## SETUP INSTRUCTIONS:

Clone this template using the following terminal or PowerShell command:
git clone --recurse-submodules -j8 https://github.com/sfarmer1/Moonworks-Game-Template.git



NOTE: You will need to run the ContentBuilderUI project first. This will generate the content files necessary to build and run the game.

First download the latest version of Moonlibs (prebuilt dlls) from [here](https://moonside.games/files/moonlibs.tar.gz) and unzip the moonlibs folder into the root directory of your newly cloned MoonWorks Game Template project.

Then download the relevant version of Shadercross from [here](https://nightly.link/libsdl-org/SDL_shadercross/workflows/main/main?preview) and unzip its contents. Then move the lib folder to ./ContentBuilder/ContentBuilderUI/bin/Debug and move the shadercross executable to ./ContentBuilder/ContentBuilderUI/bin/Debug/net9.0


### Now it is time to run ContentBuilderUI.

In the process, you may encounter the following error message

"ContentBuilderUI.csproj: Error NU1105 : Unable to find project information for '/Users/dev/Documents/GitHub/Moonworks/Tactician/ContentBuilder/ContentBuilderUI/lib/ImGui.NET/ImGui.NET.csproj'. If you are using Visual Studio, this may be because the project is unloaded or not part of the current solution so run a restore from the command-line. Otherwise, the project file may be invalid or missing targets required for restore."

If you do, try using dotnet restore

Then navigate to the ImGui.NET.csproj file (which is in ./ContentBuilder/ContentBuilderUI/lib/ImGui.NET) in your terminal or powershell and run:
dotnet build ImGui.NET.csproj

Then try running the ContentBuilderUI project again. If you use [Jetbrains Rider](https://www.jetbrains.com/rider/) for your IDE, this should be easy to do.

Once you have that up and running, you'll need to copy the paths to two directories and past them into the application. NOTE: If you're on a Mac, the paste shortcut is actually CONTROL+V, not CMD+V for this application.

The first path is should look something like whatever/directories/lead/to/Moonworks-Game-Template/ContentBuilder/ContentSource

The second path looks more like whatever/directories/lead/to/Moonworks-Game-Template

Once you have them both correct, the text should turn green and a few buttons should appear. Click on the "Build Content" button to build all of the content you'll need to run the game. NOTE: You'll need to re-run this every time you change an asset in that ContentSource folder before running your game.

If you're on a Mac, you'll likely get a whole lot of security popups which are a real pain to go through. For each one you agree to, you'll need to go into your computer's System Settings > Privacy & Security and at the bottom there will be a button you need to press to allow the shadercross dylib files to execute. The most annoying part is that you'll need to re-run the ContentBuilderUI application, click on the "Shaders" button, accept the popup, and then allow it in the system settings in that exact order countless times to get it all running correctly. Once you can see that .Content/Shaders actually contains the SpriteBatch.comp.hlsl.spv, SpriteBatch.frag.hlsl.spv, and Spritebatch.vert.hlsl.spv shaders, you'll know that your ContentBuilderUI application now works correctly with shadercross.

If you're on a Mac, you'll also need to setup your MoonworksTemplateGame application to run with the following environment variable:
DYLD_LIBRARY_PATH=your/full/path/to/Moonworks-Game-Template/bin/Debug/net9.0

How you do this depends on your IDE. In Rider, you just need to click on the Run/Debug Configuration in the top right corner that says MoonworksTemplateGame, find the edit configuration button and in the Environment Variables section add the above text. Also please remember replace the text "your/full/path/to" above with the actual absolute path that leads to your Moonworks-Game-Template folder.

After that, all you need to do is run the MoonworksTemplateGame application and you should see the following screen appear:
<img width="1277" alt="image" src="https://github.com/user-attachments/assets/e66dbfcb-1a1c-4478-9507-f00023731436" />

Use the arrow keys and wasd keys to move your characters around!

I encourage you to set a breakpoint in the Main fuction of Program.cs and step through it using a debugger like the one that comes with Rider to realy understand everything that is happening under the hood and how the code is structured.

## Additional Reading and References

MoonWorks: https://github.com/MoonsideGames/MoonWorks

Moonlibs (prebuilt dlls): https://moonside.games/files/moonlibs.tar.gz

Shader compiler (you can find prebuilts in the Actions artifacts): https://github.com/libsdl-org/SDL_shadercross

Finished game: https://github.com/thatcosmonaut/GGJ2024

GPU API documentation (MoonWorks.Graphics is a pretty thin wrapper around this): https://wiki.libsdl.org/SDL3/CategoryGPU

Article on GPU API cycling: https://moonside.games/posts/sdl-gpu-concepts-cycling/

## Licenses

ROLL AND CASH: GROCERY LORDS is released under multiple licenses, contained in the licenses folder.

Code is released under the zlib license: code.LICENSE\
Assets are released under the CC-BY-SA 4.0 license: assets.LICENSE

Kosugi font is licensed under the Apache License, Version 2.0\
Pixeltype font was made by TheJman0205 with FontStruct. It can be used by anyone for free.
