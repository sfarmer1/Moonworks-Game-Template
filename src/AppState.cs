using System;
using MoonWorks;

namespace Tactician;

public abstract class AppState {
    public abstract void Start();
    public abstract void Update(TimeSpan delta);
    public abstract void Draw(Window window, double alpha);
    public abstract void End();
}