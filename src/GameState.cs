using System;
using MoonWorks;

namespace MoonworksTemplateGame;

public abstract class GameState {
    public abstract void Start();
    public abstract void Update(TimeSpan delta);
    public abstract void Draw(Window window, double alpha);
    public abstract void End();
}