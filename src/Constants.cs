namespace Tactician;

public static class GameDimensions
{
    public const int WIDTH = 640;
    public const int HEIGHT = 360;
}

public static class FontSizes
{
    public const int SCORE = 12;
}

public enum Direction
{
    None = 0,
    Left,
    Right,
    Up,
    Down
}

public enum TileType
{
    _ = 0,
    Wall,
    PawnP1,
    PawnP2
}