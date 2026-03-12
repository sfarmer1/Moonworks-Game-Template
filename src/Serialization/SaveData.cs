using System;
using System.Collections.Generic;
using Tactician.Components;

namespace Tactician.Serialization;

/// <summary>
/// Root save data structure for chess game.
/// </summary>
public class ChessSaveData
{
    public int Version { get; set; } = 1;
    public DateTime SavedAt { get; set; }
    public GameStateData GameState { get; set; } = new();
    public List<PieceData> Pieces { get; set; } = new();
}

/// <summary>
/// Game state data (turn, phase, AI configuration).
/// </summary>
public class GameStateData
{
    public int CurrentTurn { get; set; }
    public string Phase { get; set; } = "SelectingPiece";
    public AiConfigData AiConfig { get; set; }
}

/// <summary>
/// AI configuration data.
/// </summary>
public class AiConfigData
{
    public bool IsEnabled { get; set; }
    public int AiPlayer { get; set; }
}

/// <summary>
/// Individual chess piece data.
/// </summary>
public class PieceData
{
    public string Type { get; set; } = "";
    public string Owner { get; set; } = "";
    public int File { get; set; }
    public int Rank { get; set; }
    public bool HasNotMoved { get; set; }
    public bool IsEnPassantTarget { get; set; }
}
