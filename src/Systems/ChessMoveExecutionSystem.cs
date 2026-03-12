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
	private readonly ChessTurnSystem _turnSystem;
	private readonly Filter _gameStateFilter;
	private readonly Filter _validMoveHighlightFilter;
	private readonly Filter _activePieceFilter;

	public ChessMoveExecutionSystem(World world, ChessBoardSystem boardSystem, ChessTurnSystem turnSystem) : base(world)
	{
		_boardSystem = boardSystem;
		_turnSystem = turnSystem;
		_gameStateFilter = FilterBuilder.Include<ChessGameState>().Include<PendingMove>().Build();
		_validMoveHighlightFilter = FilterBuilder.Include<ValidMoveHighlight>().Build();
		_activePieceFilter = FilterBuilder.Include<ActivePiece>().Build();
	}

	public override void Update(TimeSpan delta)
	{
		// Check for pending moves to execute
		foreach (var gameState in _gameStateFilter.Entities)
		{
			var pendingMove = Get<PendingMove>(gameState);
			ExecuteMove(pendingMove.Move);

			// Clear pending move
			Remove<PendingMove>(gameState);

			// Clear selection state
			ClearSelectionState(gameState);

			// End turn
			_turnSystem.EndTurn(gameState);

			// Notify that a turn has been completed (for history recording)
			var currentTurn = Get<CurrentTurn>(gameState);
			var aiConfig = Get<AiConfig>(gameState);
			if (currentTurn.Player != aiConfig.AiPlayer)
			{
				Send(new TurnCompletedMessage());
			}

			break; // Only one game state entity
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
					Remove<HasNotMoved>(rookEntity.Value);

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
					Remove<HasNotMoved>(rookEntity.Value);

				Logger.LogInfo("Queenside castle");
			}
		}

		// Move the piece
		_boardSystem.MovePiece(move.From, move.To);

		// Remove HasNotMoved component
		if (Has<HasNotMoved>(pieceEntity.Value))
			Remove<HasNotMoved>(pieceEntity.Value);

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
			Set(pieceEntity.Value, new EnPassantTarget());
			Logger.LogInfo("Set en passant target");
		}
	}

	private void ClearEnPassantTargets()
	{
		var filter = FilterBuilder.Include<EnPassantTarget>().Build();
		foreach (var entity in filter.Entities)
		{
			Remove<EnPassantTarget>(entity);
		}
	}

	private void ClearSelectionState(Entity gameState)
	{
		// Remove selected piece reference
		if (Has<SelectedPieceRef>(gameState))
			Remove<SelectedPieceRef>(gameState);

		// Clear valid move highlights
		foreach (var highlightEntity in _validMoveHighlightFilter.Entities)
		{
			Remove<ValidMoveHighlight>(highlightEntity);
		}

		// Clear active piece markers
		foreach (var activePiece in _activePieceFilter.Entities)
		{
			Remove<ActivePiece>(activePiece);
		}
	}
}
