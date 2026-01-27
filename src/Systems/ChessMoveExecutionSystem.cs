using System;
using MoonTools.ECS;
using MoonWorks;
using Tactician.Components;
using Tactician.Data;
using Tactician.Messages;

namespace Tactician.Systems;

public class ChessMoveExecutionSystem : MoonTools.ECS.System
{
	private readonly ChessBoardSystem _boardSystem;

	public ChessMoveExecutionSystem(World world, ChessBoardSystem boardSystem) : base(world)
	{
		_boardSystem = boardSystem;
	}

	public override void Update(TimeSpan delta)
	{
		// Process move execution messages
		if (SomeMessage<ExecuteMoveMessage>())
		{
			var message = ReadMessage<ExecuteMoveMessage>();
			ExecuteMove(message.Move);
		}
	}

	private void ExecuteMove(ChessMove move)
	{
		Logger.LogInfo($"Executing move: {move.PieceType} from ({move.From.File},{move.From.Rank}) to ({move.To.File},{move.To.Rank})");

		var pieceEntity = _boardSystem.GetPieceAt(move.From);
		if (!pieceEntity.HasValue)
		{
			Logger.LogError("No piece at source position!");
			return;
		}

		// Handle capture
		if (move.IsCapture)
		{
			if (move.IsEnPassant)
			{
				// En passant: captured pawn is not at the destination
				var piece = Get<ChessPiece>(pieceEntity.Value);
				var capturedPawnRank = piece.Owner == Player.White ? move.To.Rank - 1 : move.To.Rank + 1;
				var capturedPawnPos = new BoardPosition(move.To.File, capturedPawnRank);
				_boardSystem.RemovePiece(capturedPawnPos);
				Logger.LogInfo("En passant capture");
			}
			else
			{
				// Regular capture
				_boardSystem.RemovePiece(move.To);
				Logger.LogInfo("Captured piece");
			}
		}

		// Handle castling
		if (move.IsCastling)
		{
			// Move the rook
			var isKingside = move.To.File > move.From.File;
			if (isKingside)
			{
				// Kingside: rook moves from h-file to f-file
				var rookFrom = new BoardPosition(7, move.From.Rank);
				var rookTo = new BoardPosition(5, move.From.Rank);
				_boardSystem.MovePiece(rookFrom, rookTo);

				var rookEntity = _boardSystem.GetPieceAt(rookTo);
				if (rookEntity.HasValue && Has<HasNotMoved>(rookEntity.Value))
					World.Remove<HasNotMoved>(rookEntity.Value);

				Logger.LogInfo("Kingside castle");
			}
			else
			{
				// Queenside: rook moves from a-file to d-file
				var rookFrom = new BoardPosition(0, move.From.Rank);
				var rookTo = new BoardPosition(3, move.From.Rank);
				_boardSystem.MovePiece(rookFrom, rookTo);

				var rookEntity = _boardSystem.GetPieceAt(rookTo);
				if (rookEntity.HasValue && Has<HasNotMoved>(rookEntity.Value))
					World.Remove<HasNotMoved>(rookEntity.Value);

				Logger.LogInfo("Queenside castle");
			}
		}

		// Move the piece
		_boardSystem.MovePiece(move.From, move.To);

		// Remove HasNotMoved component
		if (Has<HasNotMoved>(pieceEntity.Value))
			World.Remove<HasNotMoved>(pieceEntity.Value);

		// Handle pawn promotion
		if (move.PromotionPiece != PieceType.None)
		{
			var piece = Get<ChessPiece>(pieceEntity.Value);
			Set(pieceEntity.Value, new ChessPiece(move.PromotionPiece, piece.Owner));
			Logger.LogInfo($"Pawn promoted to {move.PromotionPiece}");
		}

		// Clear all EnPassantTarget components
		ClearEnPassantTargets();

		// Set EnPassantTarget if pawn moved two squares
		if (move.PieceType == PieceType.Pawn && Math.Abs(move.To.Rank - move.From.Rank) == 2)
		{
			World.Set(pieceEntity.Value, new EnPassantTarget());
			Logger.LogInfo("Set en passant target");
		}
	}

	private void ClearEnPassantTargets()
	{
		var filter = FilterBuilder.Include<EnPassantTarget>().Build();
		foreach (var entity in filter.Entities)
		{
			World.Remove<EnPassantTarget>(entity);
		}
	}
}
