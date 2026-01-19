using System.Numerics;
using MoonWorks.Graphics;
using Tactician.Data;

namespace Tactician.Components;

public readonly record struct HasPlayerOwner(int Index);
public readonly record struct Orientation(float  Angle);
public readonly record struct Solid;
public readonly record struct Cursor;
public readonly record struct Selected;
public readonly record struct ColorBlend(Color Color);
public readonly record struct Depth(float      Value);
public readonly record struct DrawAsRectangle;
public readonly record struct TextDropShadow(int OffsetX, int OffsetY);
public readonly record struct GameInProgress; // yaaargh
public readonly record struct ColorFlicker(int    ElapsedFrames, Color Color);
public readonly record struct SpriteScale(Vector2 Scale);

public readonly record struct DirectionalSprites(
	SpriteAnimationInfoID Up,
	SpriteAnimationInfoID UpRight,
	SpriteAnimationInfoID Right,
	SpriteAnimationInfoID DownRight,
	SpriteAnimationInfoID Down,
	SpriteAnimationInfoID DownLeft,
	SpriteAnimationInfoID Left,
	SpriteAnimationInfoID UpLeft
);

public readonly record struct Level4X4(
	TileType A1,
	TileType A2,
	TileType A3,
	TileType A4,
	TileType B1,
	TileType B2,
	TileType B3,
	TileType B4,
	TileType C1,
	TileType C2,
	TileType C3,
	TileType C4,
	TileType D1,
	TileType D2,
	TileType D3,
	TileType D4
);

public readonly record struct Rectangle(int X, int Y, int Width, int Height)
{
	public int Left   => X;
	public int Right  => X + Width;
	public int Top    => Y;
	public int Bottom => Y + Height;

	public bool Intersects(Rectangle other)
	{
		return
			other.Left < Right &&
			Left < other.Right &&
			other.Top < Bottom &&
			Top < other.Bottom;
	}

	public static Rectangle Union(Rectangle a, Rectangle b)
	{
		var x = int.Min(a.X, a.X);
		var y = int.Min(a.Y, b.Y);
		return new Rectangle(
			x,
			y,
			int.Max(a.Right, b.Right) - x,
			int.Max(a.Bottom, b.Bottom) - y
		);
	}

	public Rectangle Inflate(int horizontal, int vertical)
	{
		return new Rectangle(
			X - horizontal,
			Y - vertical,
			Width + horizontal * 2,
			Height + vertical * 2
		);
	}
}