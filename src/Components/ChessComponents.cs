using MoonTools.ECS;
using Tactician.Data;

namespace Tactician.Components;

// Marks an entity as a chess piece with its type and owner
public readonly record struct ChessPiece(PieceType Type, Player Owner);

// Board position using chess notation (0-7 for both file and rank)
// File 0 = A, File 7 = H; Rank 0 = 1, Rank 7 = 8
public readonly record struct BoardPosition(int File, int Rank)
{
	public bool IsValid() => File >= 0 && File < ChessConstants.BOARD_SIZE && Rank >= 0 && Rank < ChessConstants.BOARD_SIZE;

	public static BoardPosition operator +(BoardPosition pos, (int fileOffset, int rankOffset) offset)
	{
		return new BoardPosition(pos.File + offset.fileOffset, pos.Rank + offset.rankOffset);
	}
}

// Marks a piece that has not moved (for castling/pawn double move)
public readonly record struct HasNotMoved;

// Marks a pawn eligible for en passant capture (set on pawn that just double-moved)
public readonly record struct EnPassantTarget;

// Marks a square as a valid move destination (for highlighting)
public readonly record struct ValidMoveHighlight;

// Marks the currently active/lifted piece
public readonly record struct ActivePiece;

// Marks the king as being in check
public readonly record struct InCheck;

// === Game State Components (attached to game state entity) ===

// Current player's turn
public readonly record struct CurrentTurn(Player Player);

// Current game phase
public readonly record struct CurrentGamePhase(GamePhase Phase);

// Reference to the currently selected piece (if any)
public readonly record struct SelectedPieceRef(Entity PieceEntity);

// Pending move to execute (set by input/AI, consumed by execution system)
public readonly record struct PendingMove(ChessMove Move);

// AI configuration
public readonly record struct AiConfig(bool IsEnabled, Player AiPlayer);

// Marker for the chess game state entity
public readonly record struct ChessGameState;
