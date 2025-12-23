using System.Numerics;

namespace Tactician.Components;

public readonly record struct Velocity {
    public static Velocity Zero = new(0f, 0f);
    public static Velocity One = new(1f, 1f);

    public readonly Vector2 Value;

    public Velocity(Vector2 v) {
        Value = v;
    }

    public Velocity(float x, float y) {
        Value = new Vector2(x, y);
    }

    public float X => Value.X;

    public float Y => Value.Y;

    public static implicit operator Vector2(Velocity v) {
        return v.Value;
    }
}