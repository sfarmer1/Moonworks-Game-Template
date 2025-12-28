# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

Tactician is a game built with the MoonWorks framework, based on a stripped-down version of "ROLL AND CASH: GROCERY LORDS" from Global Game Jam 2024. It uses an Entity Component System (ECS) architecture via MoonTools.ECS.

## Build and Run Commands

### Prerequisites
1. Download [moonlibs](https://moonside.games/files/moonlibs.tar.gz) and extract to `./moonlibs/` in the project root
2. Download [Shadercross](https://nightly.link/libsdl-org/SDL_shadercross/workflows/main/main?preview), extract, and:
   - Copy `lib/` to `./ContentBuilder/ContentBuilderUI/bin/Debug/`
   - Copy `shadercross` executable to `./ContentBuilder/ContentBuilderUI/bin/Debug/net9.0/`

### Content Building (Required First)
Content must be built before running the game:

```bash
# Navigate to ContentBuilderUI
cd ContentBuilder/ContentBuilderUI

# If you get NU1105 error about ImGui.NET, run:
dotnet restore
cd lib/ImGui.NET
dotnet build ImGui.NET.csproj
cd ../..

# Run ContentBuilderUI
dotnet run
```

In the UI:
- Set first path to: `<project-root>/ContentBuilder/ContentSource`
- Set second path to: `<project-root>`
- Click "Build Content" to build all assets
- Click "Shaders" button to compile shaders
- On macOS: Accept security popups and approve in System Settings > Privacy & Security after each popup

Content must be rebuilt whenever assets in `ContentSource/` are modified.

### Building the Game

```bash
# Build the main project
dotnet build Tactician.csproj

# Or build the solution
dotnet build Tactician.sln
```

### Running the Game

```bash
dotnet run --project Tactician.csproj
```

**macOS only**: Set environment variable before running:
```bash
export DYLD_LIBRARY_PATH=/full/path/to/Tactician/bin/Debug/net9.0
dotnet run --project Tactician.csproj
```

In IDEs like Rider: Add `DYLD_LIBRARY_PATH` to run configuration's environment variables.

## Architecture

### App State Pattern
The game uses a state machine pattern where states inherit from `AppState`:
- `AppBootstrapper` (src/AppBootstrapper.cs) - Entry point, creates window and initializes the `App`
- `App` (src/App.cs) - Main game class, manages state transitions
- `AppState` (src/AppState.cs) - Abstract base class for game states
- State implementations in `src/GameStates/`:
  - `LoadingAppState` - Handles asset loading
  - `GameLoopAppState` - Main gameplay loop
  - `TutorialAppState` - How-to-play screen

States implement: `Start()`, `Update(TimeSpan)`, `Draw(Window, double)`, `End()`

### Entity Component System (MoonTools.ECS)
The game uses a pure ECS architecture:

**Components** (src/Components/): Pure data structs (Position, Velocity, Sprite, Player, etc.)

**Systems** (src/Systems/): Logic that operates on entities with specific component combinations
- `InputSystem` - Captures player input
- `PlayerControllerSystem` - Handles player movement and spawning
- `MotionSystem` - Applies velocity to positions
- `DirectionalAnimationSystem` - Updates sprite based on movement direction
- `SetSpriteAnimationSystem` - Assigns sprite animations
- `UpdateSpriteAnimationSystem` - Advances animation frames
- `ColorAnimationSystem` - Handles color transitions
- `AudioSystem` - Manages sound playback

**World** (MoonTools.ECS): Central ECS world managing all entities, components, and systems

System update order in `GameLoopAppState`:
1. UpdateSpriteAnimationSystem
2. InputSystem
3. PlayerControllerSystem
4. MotionSystem
5. DirectionalAnimationSystem
6. SetSpriteAnimationSystem
7. ColorAnimationSystem
8. AudioSystem
9. World.FinishUpdate()

### Content Pipeline
Content is processed by `ContentBuilderUI` and loaded at runtime:

**Generated Code** (src/Generated/): Auto-generated content references
- `TextureAtlases.cs` - Texture atlas initialization
- `SpriteAnimations.cs` - Sprite animation definitions
- `Fonts.cs` - Font loading
- `StaticAudio.cs` / `StaticAudioPacks.cs` - Static audio resources
- `StreamingAudio.cs` - Streaming audio resources

**Content Data** (src/Data/):
- `CramAtlasReader.cs` - Reads texture atlas JSON
- `CramTextureAtlasData.cs` - Atlas data structures
- `SpriteAnimationInfo.cs` - Animation metadata
- `TexturePage.cs` - Texture page management

**Runtime Content** (Content/): Built assets
- `Content/Textures/` - Texture atlases and JSON metadata
- `Content/Shaders/` - Compiled shaders (.spv files)
- `Content/Fonts/` - Font files
- `Content/Audio/` - Audio files
- `Content/Data/` - Game data files

### Rendering
- `Graphics/Renderer.cs` - Main rendering system using SpriteBatch
- `Graphics/SpriteBatch.cs` - Batched 2D sprite rendering with MoonWorks GPU API
- `Graphics/UV.cs` - Texture coordinate utilities
- Renders to game dimensions (640x360), supports fullscreen toggle with F11

### Platform-Specific Notes
**macOS**:
- Requires `DYLD_LIBRARY_PATH` environment variable pointing to output directory
- Post-build steps include `install_name_tool` and `codesign` for dynamic library loading
- Security popups are normal when running ContentBuilderUI with Shadercross

**Windows/Linux**:
- Native libraries copied from `moonlibs/win64/` or `moonlibs/lib64/` via `CopyMoonlibs.targets`

## Project Structure Notes
- Main game project: `Tactician.csproj` (namespace: `Tactician`)
- StartupObject: `Tactician.AppBootstrapper`
- Target Framework: .NET 9.0
- AllowUnsafeBlocks: true (required for native interop)
- ContentBuilder is excluded from main build via `DefaultItemExcludes`

## Dependencies
- **MoonWorks** (lib/MoonWorks) - Game framework wrapping SDL3 GPU API
- **MoonTools.ECS** (lib/MoonTools.ECS) - Entity Component System
- Both are included as project references via git submodules

## Debugging
- Use breakpoints in `AppBootstrapper.Main()` to understand initialization flow
- System update order in `GameLoopAppState.Update()` is critical for gameplay logic
- Unhandled exceptions are logged to `%LOCALAPPDATA%/Tactician/log.txt` (or macOS equivalent)