using MoonTools.ECS;
using Tactician.Data;
using Tactician.Serialization;

namespace Tactician.Components;

// Marks an entity as a chess piece with its type and owner
[SerializableComponent(ComponentTypeId ="ChessPiece")]
public readonly record struct ChessPiece(PieceType Type, Player Owner);

// Board position using chess notation (0-7 for both file and rank)
// File 0 = A, File 7 = H; Rank 0 = 1, Rank 7 = 8
[SerializableComponent(ComponentTypeId ="BoardPosition")]
public readonly record struct BoardPosition(int File, int Rank)
{
	public bool IsValid() => File >= 0 && File < ChessConstants.BOARD_SIZE && Rank >= 0 && Rank < ChessConstants.BOARD_SIZE;

	public static BoardPosition operator +(BoardPosition pos, (int fileOffset, int rankOffset) offset)
	{
		return new BoardPosition(pos.File + offset.fileOffset, pos.Rank + offset.rankOffset);
	}
}

// Marks a piece that has not moved (for castling/pawn double move)
[SerializableMarker(ComponentTypeId ="HasNotMoved")]
public readonly record struct HasNotMoved;

// Marks a pawn eligible for en passant capture (set on pawn that just double-moved)
[SerializableMarker(ComponentTypeId ="EnPassantTarget")]
public readonly record struct EnPassantTarget;

// Marks a square as a valid move destination (for highlighting)
[TransientComponent]
public readonly record struct ValidMoveHighlight;

// Marks the currently active/lifted piece
[TransientComponent]
public readonly record struct ActivePiece;

// Marks the king as being in check
[TransientComponent]
public readonly record struct InCheck;

// === Game State Components (attached to game state entity) ===

// Current player's turn
[SerializableComponent(ComponentTypeId ="CurrentTurn")]
public readonly record struct CurrentTurn(Player Player);

// Current game phase
[SerializableComponent(ComponentTypeId ="CurrentGamePhase")]
public readonly record struct CurrentGamePhase(GamePhase Phase);

// Reference to the currently selected piece (if any)
[TransientComponent]
public readonly record struct SelectedPieceRef(Entity PieceEntity);

// Pending move to execute (set by input/AI, consumed by execution system)
[TransientComponent]
public readonly record struct PendingMove(ChessMove Move);

// AI configuration
[SerializableComponent(ComponentTypeId ="AiConfig")]
public readonly record struct AiConfig(bool IsEnabled, Player AiPlayer);

// Marker for the chess game state entity
[SerializableMarker(ComponentTypeId ="ChessGameState")]
public readonly record struct ChessGameState;

// === Input State Tracking Components (attached to game state entity) ===
// These track the previous frame's input state to detect new button presses

// Tracks whether confirm button was pressed last frame
[TransientComponent]
public readonly record struct ConfirmButtonWasPressed;

// Tracks whether cancel button was pressed last frame
[TransientComponent]
public readonly record struct CancelButtonWasPressed;

// Tracks whether reset button was pressed last frame
[TransientComponent]
public readonly record struct ResetButtonWasPressed;
