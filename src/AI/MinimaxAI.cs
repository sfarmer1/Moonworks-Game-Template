using System.Collections.Generic;
using Tactician.Components;
using Tactician.Data;
using Tactician.Systems;

namespace Tactician.AI;

public class MinimaxAI : IChessAI
{
	private readonly ChessBoardSystem _boardSystem;
	private readonly MoveValidationSystem _validationSystem;
	private readonly Player _player;
	private readonly int _maxDepth;

	// Piece values for material evaluation
	private static readonly Dictionary<PieceType, int> PieceValues = new()
	{
		{ PieceType.Pawn, 100 },
		{ PieceType.Knight, 320 },
		{ PieceType.Bishop, 330 },
		{ PieceType.Rook, 500 },
		{ PieceType.Queen, 900 },
		{ PieceType.King, 20000 }
	};

	public MinimaxAI(ChessBoardSystem boardSystem, MoveValidationSystem validationSystem, Player player, int maxDepth = 2)
	{
		_boardSystem = boardSystem;
		_validationSystem = validationSystem;
		_player = player;
		_maxDepth = maxDepth;
	}

	public ChessMove GetBestMove()
	{
		var allValidMoves = GetAllValidMovesForPlayer(_player);

		if (allValidMoves.Count == 0)
			return ChessMove.None;

		ChessMove bestMove = allValidMoves[0];
		int bestValue = int.MinValue;

		// Evaluate each move
		foreach (var move in allValidMoves)
		{
			int moveValue = EvaluateMove(move);

			if (moveValue > bestValue)
			{
				bestValue = moveValue;
				bestMove = move;
			}
		}

		return bestMove;
	}

	private int EvaluateMove(ChessMove move)
	{
		// Simple evaluation: material gain + position bonus
		int value = 0;

		// Material gain from capture
		if (move.IsCapture)
		{
			var capturedPiece = _boardSystem.GetPieceAt(move.To);
			if (capturedPiece.HasValue)
			{
				var piece = _validationSystem.Get<ChessPiece>(capturedPiece.Value);
				value += PieceValues[piece.Type];
			}
		}

		// Bonus for central control
		var fileDist = System.Math.Abs(move.To.File - 3.5);
		var rankDist = System.Math.Abs(move.To.Rank - 3.5);
		value += (int)((7 - fileDist - rankDist) * 10);

		// Bonus for pawn advancement
		if (move.PieceType == PieceType.Pawn)
		{
			value += _player == Player.White ? move.To.Rank * 10 : (7 - move.To.Rank) * 10;
		}

		// Bonus for piece development (moving from starting position)
		if (move.From.Rank == (_player == Player.White ? 0 : 7))
		{
			value += 30;
		}

		return value;
	}

	private List<ChessMove> GetAllValidMovesForPlayer(Player player)
	{
		var allMoves = new List<ChessMove>();

		for (int rank = 0; rank < ChessConstants.BOARD_SIZE; rank++)
		{
			for (int file = 0; file < ChessConstants.BOARD_SIZE; file++)
			{
				var pieceEntity = _boardSystem.GetPieceAt(new BoardPosition(file, rank));
				if (!pieceEntity.HasValue)
					continue;

				var piece = _validationSystem.Get<ChessPiece>(pieceEntity.Value);
				if (piece.Owner != player)
					continue;

				var validMoves = _validationSystem.GetValidMoves(pieceEntity.Value);
				allMoves.AddRange(validMoves);
			}
		}

		return allMoves;
	}

	private int Evaluate()
	{
		// Evaluate current board position
		int score = 0;

		for (int rank = 0; rank < ChessConstants.BOARD_SIZE; rank++)
		{
			for (int file = 0; file < ChessConstants.BOARD_SIZE; file++)
			{
				var pieceEntity = _boardSystem.GetPieceAt(new BoardPosition(file, rank));
				if (!pieceEntity.HasValue)
					continue;

				var piece = _validationSystem.Get<ChessPiece>(pieceEntity.Value);
				int pieceValue = PieceValues[piece.Type];

				if (piece.Owner == _player)
					score += pieceValue;
				else
					score -= pieceValue;
			}
		}

		return score;
	}
}
