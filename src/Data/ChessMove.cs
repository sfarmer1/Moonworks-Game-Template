using Tactician.Components;

namespace Tactician.Data;

public readonly record struct ChessMove(
	BoardPosition From,
	BoardPosition To,
	PieceType PieceType,
	bool IsCapture,
	bool IsCastling,
	bool IsEnPassant,
	PieceType PromotionPiece // PieceType.None if not a promotion
)
{
	public static ChessMove None => new ChessMove(
		new BoardPosition(-1, -1),
		new BoardPosition(-1, -1),
		PieceType.None,
		false,
		false,
		false,
		PieceType.None
	);

	public bool IsValid() => From.IsValid() && To.IsValid();
}
