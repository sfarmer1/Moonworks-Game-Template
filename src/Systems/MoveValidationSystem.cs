using System;
using System.Collections.Generic;
using MoonTools.ECS;
using Tactician.Components;
using Tactician.Data;

namespace Tactician.Systems;

public class MoveValidationSystem : MoonTools.ECS.System
{
	private readonly ChessBoardSystem _boardSystem;

	public MoveValidationSystem(World world, ChessBoardSystem boardSystem) : base(world)
	{
		_boardSystem = boardSystem;
	}

	public List<ChessMove> GetValidMoves(Entity pieceEntity)
	{
		if (!Has<ChessPiece>(pieceEntity) || !Has<BoardPosition>(pieceEntity))
			return new List<ChessMove>();

		var piece = Get<ChessPiece>(pieceEntity);
		var position = Get<BoardPosition>(pieceEntity);
		var hasMoved = !Has<HasNotMoved>(pieceEntity);

		var potentialMoves = piece.Type switch
		{
			PieceType.Pawn   => GetPawnMoves(position, piece.Owner, hasMoved),
			PieceType.Knight => GetKnightMoves(position, piece.Owner),
			PieceType.Bishop => GetBishopMoves(position, piece.Owner),
			PieceType.Rook   => GetRookMoves(position, piece.Owner),
			PieceType.Queen  => GetQueenMoves(position, piece.Owner),
			PieceType.King   => GetKingMoves(position, piece.Owner, hasMoved),
			_ => new List<ChessMove>()
		};

		// Filter out moves that would leave own king in check
		var validMoves = new List<ChessMove>();
		foreach (var move in potentialMoves)
		{
			if (!WouldLeaveKingInCheck(move, piece.Owner))
				validMoves.Add(move);
		}

		return validMoves;
	}

	private List<ChessMove> GetPawnMoves(BoardPosition pos, Player owner, bool hasMoved)
	{
		var moves = new List<ChessMove>();
		var direction = owner == Player.White ? 1 : -1; // White moves up (increasing rank), Black down

		// Forward one square
		var oneForward = pos + (0, direction);
		if (oneForward.IsValid() && _boardSystem.GetPieceAt(oneForward) == null)
		{
			// Check for promotion
			var isPromotion = (owner == Player.White && oneForward.Rank == 7) || (owner == Player.Black && oneForward.Rank == 0);
			if (isPromotion)
			{
				// Add all promotion options
				moves.Add(new ChessMove(pos, oneForward, PieceType.Pawn, false, false, false, PieceType.Queen));
				moves.Add(new ChessMove(pos, oneForward, PieceType.Pawn, false, false, false, PieceType.Rook));
				moves.Add(new ChessMove(pos, oneForward, PieceType.Pawn, false, false, false, PieceType.Bishop));
				moves.Add(new ChessMove(pos, oneForward, PieceType.Pawn, false, false, false, PieceType.Knight));
			}
			else
			{
				moves.Add(new ChessMove(pos, oneForward, PieceType.Pawn, false, false, false, PieceType.None));

				// Forward two squares (only if hasn't moved and path is clear)
				if (!hasMoved)
				{
					var twoForward = pos + (0, direction * 2);
					if (twoForward.IsValid() && _boardSystem.GetPieceAt(twoForward) == null)
						moves.Add(new ChessMove(pos, twoForward, PieceType.Pawn, false, false, false, PieceType.None));
				}
			}
		}

		// Diagonal captures
		foreach (var fileOffset in new[] { -1, 1 })
		{
			var capturePos = pos + (fileOffset, direction);
			if (!capturePos.IsValid()) continue;

			var targetPiece = _boardSystem.GetPieceAt(capturePos);
			if (targetPiece != null && Has<ChessPiece>(targetPiece.Value))
			{
				var targetOwner = Get<ChessPiece>(targetPiece.Value).Owner;
				if (targetOwner != owner)
				{
					var isPromotion = (owner == Player.White && capturePos.Rank == 7) || (owner == Player.Black && capturePos.Rank == 0);
					if (isPromotion)
					{
						moves.Add(new ChessMove(pos, capturePos, PieceType.Pawn, true, false, false, PieceType.Queen));
						moves.Add(new ChessMove(pos, capturePos, PieceType.Pawn, true, false, false, PieceType.Rook));
						moves.Add(new ChessMove(pos, capturePos, PieceType.Pawn, true, false, false, PieceType.Bishop));
						moves.Add(new ChessMove(pos, capturePos, PieceType.Pawn, true, false, false, PieceType.Knight));
					}
					else
					{
						moves.Add(new ChessMove(pos, capturePos, PieceType.Pawn, true, false, false, PieceType.None));
					}
				}
			}
			// En passant
			else
			{
				var enPassantTarget = _boardSystem.GetPieceAt(pos + (fileOffset, 0));
				if (enPassantTarget != null && Has<EnPassantTarget>(enPassantTarget.Value))
				{
					var targetOwner = Get<ChessPiece>(enPassantTarget.Value).Owner;
					if (targetOwner != owner)
						moves.Add(new ChessMove(pos, capturePos, PieceType.Pawn, true, false, true, PieceType.None));
				}
			}
		}

		return moves;
	}

	private List<ChessMove> GetKnightMoves(BoardPosition pos, Player owner)
	{
		var moves = new List<ChessMove>();
		var offsets = new[]
		{
			(-2, -1), (-2, 1), (-1, -2), (-1, 2),
			(1, -2), (1, 2), (2, -1), (2, 1)
		};

		foreach (var offset in offsets)
		{
			var targetPos = pos + offset;
			if (!targetPos.IsValid()) continue;

			var targetPiece = _boardSystem.GetPieceAt(targetPos);
			if (targetPiece == null)
			{
				moves.Add(new ChessMove(pos, targetPos, PieceType.Knight, false, false, false, PieceType.None));
			}
			else if (Has<ChessPiece>(targetPiece.Value))
			{
				var targetOwner = Get<ChessPiece>(targetPiece.Value).Owner;
				if (targetOwner != owner)
					moves.Add(new ChessMove(pos, targetPos, PieceType.Knight, true, false, false, PieceType.None));
			}
		}

		return moves;
	}

	private List<ChessMove> GetBishopMoves(BoardPosition pos, Player owner)
	{
		var moves = new List<ChessMove>();
		var directions = new[] { (-1, -1), (-1, 1), (1, -1), (1, 1) };

		foreach (var dir in directions)
			AddSlidingMoves(moves, pos, dir, owner, PieceType.Bishop);

		return moves;
	}

	private List<ChessMove> GetRookMoves(BoardPosition pos, Player owner)
	{
		var moves = new List<ChessMove>();
		var directions = new[] { (-1, 0), (1, 0), (0, -1), (0, 1) };

		foreach (var dir in directions)
			AddSlidingMoves(moves, pos, dir, owner, PieceType.Rook);

		return moves;
	}

	private List<ChessMove> GetQueenMoves(BoardPosition pos, Player owner)
	{
		var moves = new List<ChessMove>();
		var directions = new[]
		{
			(-1, -1), (-1, 0), (-1, 1),
			(0, -1),           (0, 1),
			(1, -1),  (1, 0),  (1, 1)
		};

		foreach (var dir in directions)
			AddSlidingMoves(moves, pos, dir, owner, PieceType.Queen);

		return moves;
	}

	private void AddSlidingMoves(List<ChessMove> moves, BoardPosition start, (int fileOffset, int rankOffset) direction, Player owner, PieceType pieceType)
	{
		var currentPos = start;
		while (true)
		{
			currentPos = currentPos + direction;
			if (!currentPos.IsValid()) break;

			var targetPiece = _boardSystem.GetPieceAt(currentPos);
			if (targetPiece == null)
			{
				moves.Add(new ChessMove(start, currentPos, pieceType, false, false, false, PieceType.None));
			}
			else if (Has<ChessPiece>(targetPiece.Value))
			{
				var targetOwner = Get<ChessPiece>(targetPiece.Value).Owner;
				if (targetOwner != owner)
					moves.Add(new ChessMove(start, currentPos, pieceType, true, false, false, PieceType.None));
				break; // Can't move past any piece
			}
		}
	}

	private List<ChessMove> GetKingMoves(BoardPosition pos, Player owner, bool hasMoved)
	{
		var moves = new List<ChessMove>();
		var offsets = new[]
		{
			(-1, -1), (-1, 0), (-1, 1),
			(0, -1),           (0, 1),
			(1, -1),  (1, 0),  (1, 1)
		};

		// Normal king moves
		foreach (var offset in offsets)
		{
			var targetPos = pos + offset;
			if (!targetPos.IsValid()) continue;

			var targetPiece = _boardSystem.GetPieceAt(targetPos);
			if (targetPiece == null)
			{
				if (!IsSquareThreatened(targetPos, owner == Player.White ? Player.Black : Player.White))
					moves.Add(new ChessMove(pos, targetPos, PieceType.King, false, false, false, PieceType.None));
			}
			else if (Has<ChessPiece>(targetPiece.Value))
			{
				var targetOwner = Get<ChessPiece>(targetPiece.Value).Owner;
				if (targetOwner != owner && !IsSquareThreatened(targetPos, owner == Player.White ? Player.Black : Player.White))
					moves.Add(new ChessMove(pos, targetPos, PieceType.King, true, false, false, PieceType.None));
			}
		}

		// Castling (only if king hasn't moved)
		if (!hasMoved && !IsInCheck(owner))
		{
			// Kingside castling
			var kingsideRookPos = new BoardPosition(7, pos.Rank);
			var kingsideRook = _boardSystem.GetPieceAt(kingsideRookPos);
			if (kingsideRook != null && Has<HasNotMoved>(kingsideRook.Value))
			{
				// Check if squares between king and rook are empty and not threatened
				var f1 = pos + (1, 0);
				var g1 = pos + (2, 0);
				if (_boardSystem.GetPieceAt(f1) == null && _boardSystem.GetPieceAt(g1) == null &&
					!IsSquareThreatened(f1, owner == Player.White ? Player.Black : Player.White) &&
					!IsSquareThreatened(g1, owner == Player.White ? Player.Black : Player.White))
				{
					moves.Add(new ChessMove(pos, g1, PieceType.King, false, true, false, PieceType.None));
				}
			}

			// Queenside castling
			var queensideRookPos = new BoardPosition(0, pos.Rank);
			var queensideRook = _boardSystem.GetPieceAt(queensideRookPos);
			if (queensideRook != null && Has<HasNotMoved>(queensideRook.Value))
			{
				// Check if squares between king and rook are empty and not threatened
				var d1 = pos + (-1, 0);
				var c1 = pos + (-2, 0);
				var b1 = pos + (-3, 0);
				if (_boardSystem.GetPieceAt(d1) == null && _boardSystem.GetPieceAt(c1) == null && _boardSystem.GetPieceAt(b1) == null &&
					!IsSquareThreatened(d1, owner == Player.White ? Player.Black : Player.White) &&
					!IsSquareThreatened(c1, owner == Player.White ? Player.Black : Player.White))
				{
					moves.Add(new ChessMove(pos, c1, PieceType.King, false, true, false, PieceType.None));
				}
			}
		}

		return moves;
	}

	public bool IsSquareThreatened(BoardPosition pos, Player byPlayer)
	{
		// Check all opponent pieces to see if any threaten this square
		for (int rank = 0; rank < ChessConstants.BOARD_SIZE; rank++)
		{
			for (int file = 0; file < ChessConstants.BOARD_SIZE; file++)
			{
				var pieceEntity = _boardSystem.GetPieceAt(new BoardPosition(file, rank));
				if (pieceEntity == null || !Has<ChessPiece>(pieceEntity.Value))
					continue;

				var piece = Get<ChessPiece>(pieceEntity.Value);
				if (piece.Owner != byPlayer)
					continue;

				// Check if this piece can attack the target square
				var piecePos = new BoardPosition(file, rank);
				if (CanPieceAttackSquare(piecePos, piece.Type, pos, byPlayer))
					return true;
			}
		}

		return false;
	}

	private bool CanPieceAttackSquare(BoardPosition piecePos, PieceType pieceType, BoardPosition targetPos, Player owner)
	{
		// Simplified attack detection (doesn't need to check for check)
		switch (pieceType)
		{
			case PieceType.Pawn:
				var direction = owner == Player.White ? 1 : -1;
				return (targetPos == piecePos + (-1, direction) || targetPos == piecePos + (1, direction));

			case PieceType.Knight:
				var diff = (targetPos.File - piecePos.File, targetPos.Rank - piecePos.Rank);
				return (Math.Abs(diff.Item1) == 2 && Math.Abs(diff.Item2) == 1) ||
					   (Math.Abs(diff.Item1) == 1 && Math.Abs(diff.Item2) == 2);

			case PieceType.Bishop:
				return IsDiagonalClear(piecePos, targetPos);

			case PieceType.Rook:
				return IsStraightClear(piecePos, targetPos);

			case PieceType.Queen:
				return IsDiagonalClear(piecePos, targetPos) || IsStraightClear(piecePos, targetPos);

			case PieceType.King:
				return Math.Abs(targetPos.File - piecePos.File) <= 1 && Math.Abs(targetPos.Rank - piecePos.Rank) <= 1;

			default:
				return false;
		}
	}

	private bool IsDiagonalClear(BoardPosition from, BoardPosition to)
	{
		var fileDiff = to.File - from.File;
		var rankDiff = to.Rank - from.Rank;

		if (Math.Abs(fileDiff) != Math.Abs(rankDiff) || fileDiff == 0)
			return false;

		var fileStep = Math.Sign(fileDiff);
		var rankStep = Math.Sign(rankDiff);
		var current = from + (fileStep, rankStep);

		while (current != to)
		{
			if (_boardSystem.GetPieceAt(current) != null)
				return false;
			current = current + (fileStep, rankStep);
		}

		return true;
	}

	private bool IsStraightClear(BoardPosition from, BoardPosition to)
	{
		if (from.File != to.File && from.Rank != to.Rank)
			return false;

		var fileStep = Math.Sign(to.File - from.File);
		var rankStep = Math.Sign(to.Rank - from.Rank);
		var current = from + (fileStep, rankStep);

		while (current != to)
		{
			if (_boardSystem.GetPieceAt(current) != null)
				return false;
			current = current + (fileStep, rankStep);
		}

		return true;
	}

	private bool WouldLeaveKingInCheck(ChessMove move, Player player)
	{
		// TODO: Implement by simulating the move and checking if king is in check
		// For now, return false (allows all moves)
		return false;
	}

	public bool IsInCheck(Player player)
	{
		// Find the king
		for (int rank = 0; rank < ChessConstants.BOARD_SIZE; rank++)
		{
			for (int file = 0; file < ChessConstants.BOARD_SIZE; file++)
			{
				var pieceEntity = _boardSystem.GetPieceAt(new BoardPosition(file, rank));
				if (pieceEntity == null || !Has<ChessPiece>(pieceEntity.Value))
					continue;

				var piece = Get<ChessPiece>(pieceEntity.Value);
				if (piece.Type == PieceType.King && piece.Owner == player)
				{
					var opponent = player == Player.White ? Player.Black : Player.White;
					return IsSquareThreatened(new BoardPosition(file, rank), opponent);
				}
			}
		}

		return false;
	}

	public bool IsCheckmate(Player player)
	{
		if (!IsInCheck(player))
			return false;

		// Check if player has any valid moves
		for (int rank = 0; rank < ChessConstants.BOARD_SIZE; rank++)
		{
			for (int file = 0; file < ChessConstants.BOARD_SIZE; file++)
			{
				var pieceEntity = _boardSystem.GetPieceAt(new BoardPosition(file, rank));
				if (pieceEntity == null || !Has<ChessPiece>(pieceEntity.Value))
					continue;

				var piece = Get<ChessPiece>(pieceEntity.Value);
				if (piece.Owner == player)
				{
					var validMoves = GetValidMoves(pieceEntity.Value);
					if (validMoves.Count > 0)
						return false; // Player has at least one valid move
				}
			}
		}

		return true; // In check and no valid moves = checkmate
	}

	public bool IsStalemate(Player player)
	{
		if (IsInCheck(player))
			return false; // Can't be stalemate if in check

		// Check if player has any valid moves
		for (int rank = 0; rank < ChessConstants.BOARD_SIZE; rank++)
		{
			for (int file = 0; file < ChessConstants.BOARD_SIZE; file++)
			{
				var pieceEntity = _boardSystem.GetPieceAt(new BoardPosition(file, rank));
				if (pieceEntity == null || !Has<ChessPiece>(pieceEntity.Value))
					continue;

				var piece = Get<ChessPiece>(pieceEntity.Value);
				if (piece.Owner == player)
				{
					var validMoves = GetValidMoves(pieceEntity.Value);
					if (validMoves.Count > 0)
						return false; // Player has at least one valid move
				}
			}
		}

		return true; // Not in check but no valid moves = stalemate
	}

	public override void Update(TimeSpan delta)
	{
		// No per-frame updates needed
	}
}
