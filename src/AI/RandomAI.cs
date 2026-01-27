using System.Collections.Generic;
using MoonTools.ECS;
using Tactician.Components;
using Tactician.Data;
using Tactician.Systems;

namespace Tactician.AI;

public class RandomAI : IChessAI
{
	private readonly World _world;
	private readonly ChessBoardSystem _boardSystem;
	private readonly MoveValidationSystem _validationSystem;
	private readonly Player _player;

	public RandomAI(World world, ChessBoardSystem boardSystem, MoveValidationSystem validationSystem, Player player)
	{
		_world = world;
		_boardSystem = boardSystem;
		_validationSystem = validationSystem;
		_player = player;
	}

	public ChessMove GetBestMove()
	{
		var allValidMoves = new List<ChessMove>();

		// Collect all valid moves for all pieces
		for (int rank = 0; rank < ChessConstants.BOARD_SIZE; rank++)
		{
			for (int file = 0; file < ChessConstants.BOARD_SIZE; file++)
			{
				var pieceEntity = _boardSystem.GetPieceAt(new BoardPosition(file, rank));
				if (!pieceEntity.HasValue)
					continue;

				// Access components through the world since we're not in a System
				if (!_world.Has<ChessPiece>(pieceEntity.Value))
					continue;

				var piece = _world.Get<ChessPiece>(pieceEntity.Value);
				if (piece.Owner != _player)
					continue;

				var validMoves = _validationSystem.GetValidMoves(pieceEntity.Value);
				allValidMoves.AddRange(validMoves);
			}
		}

		// Return random valid move
		if (allValidMoves.Count > 0)
		{
			var randomIndex = System.Random.Shared.Next(allValidMoves.Count);
			return allValidMoves[randomIndex];
		}

		return ChessMove.None;
	}
}
