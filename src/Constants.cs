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

public static class ChessConstants
{
    public const int BOARD_SIZE = 8;
    public const int TILE_SIZE = 40; // 8 tiles * 40 = 320px, fits in 640x360
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

public enum PieceType
{
    None = 0,
    Pawn,
    Knight,
    Bishop,
    Rook,
    Queen,
    King
}

public enum Player
{
    White = 0,
    Black = 1
}

public enum GamePhase
{
    SelectingPiece,
    SelectingDestination,
    AIThinking,
    GameOver
}