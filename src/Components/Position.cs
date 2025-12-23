using System;
using System.Numerics;

namespace Tactician.Components;

public readonly record struct Position {
    private readonly Vector2 _rawPosition;

    public Position(float x, float y) {
        _rawPosition = new Vector2(x, y);
        X = (int)MathF.Round(x);
        Y = (int)MathF.Round(y);
    }

    public Position(int x, int y) {
        _rawPosition = new Vector2(x, y);
        X = x;
        Y = y;
    }

    public Position(Vector2 v) {
        _rawPosition = v;
        X = (int)MathF.Round(v.X);
        Y = (int)MathF.Round(v.Y);
    }

    public int X { get; }
    public int Y { get; }

    public Position SetX(int x) {
        return new Position(x, _rawPosition.Y);
    }

    public Position SetY(int y) {
        return new Position(_rawPosition.X, y);
    }

    public static Position operator +(Position a, Position b) {
        return new Position(a._rawPosition + b._rawPosition);
    }

    public static Vector2 operator -(Position a, Position b) {
        return a._rawPosition - b._rawPosition;
    }

    public static Position operator +(Position a, Vector2 b) {
        return new Position(a._rawPosition + b);
    }

    public override string ToString() {
        return $"({X}, {Y})";
    }
}