using System;
using MoonTools.ECS;
using MoonWorks.Graphics;
using Tactician.Components;
using Tactician.Content;
using Tactician.Data;

namespace Tactician.Systems;

public class ChessBoardSystem : MoonTools.ECS.System
{
	private Entity[,] _squares;
	private Entity[,] _pieces;

	public ChessBoardSystem(World world) : base(world)
	{
		_squares = new Entity[ChessConstants.BOARD_SIZE, ChessConstants.BOARD_SIZE];
		_pieces = new Entity[ChessConstants.BOARD_SIZE, ChessConstants.BOARD_SIZE];
	}

	public void InitializeBoard()
	{
		var centerX = GameDimensions.WIDTH * 0.5f;
		var centerY = GameDimensions.HEIGHT * 0.5f;
		var boardPixelSize = ChessConstants.BOARD_SIZE * ChessConstants.TILE_SIZE;
		var gridStartX = centerX - (boardPixelSize * 0.5f) + (ChessConstants.TILE_SIZE * 0.5f);
		var gridStartY = centerY - (boardPixelSize * 0.5f) + (ChessConstants.TILE_SIZE * 0.5f);

		// Create 8x8 grid of squares
		for (int rank = 0; rank < ChessConstants.BOARD_SIZE; rank++)
		{
			for (int file = 0; file < ChessConstants.BOARD_SIZE; file++)
			{
				var square = World.CreateEntity();
				var posX = gridStartX + file * ChessConstants.TILE_SIZE;
				var posY = gridStartY + rank * ChessConstants.TILE_SIZE;

				World.Set(square, new Position(posX, posY));
				World.Set(square, new BoardPosition(file, rank));
				World.Set(square, new SpriteAnimation(SpriteAnimations.Pixel));

				// Alternate light/dark squares (chess board pattern)
				var isLightSquare = (file + rank) % 2 == 0;
				var squareColor = isLightSquare
					? new Color(0.9f, 0.9f, 0.8f, 1f)  // Light beige
					: new Color(0.6f, 0.4f, 0.3f, 1f); // Dark brown
				World.Set(square, new ColorBlend(squareColor));
				World.Set(square, new SpriteScale(new System.Numerics.Vector2(ChessConstants.TILE_SIZE, ChessConstants.TILE_SIZE)));
				World.Set(square, new Depth(0)); // Squares render at base level

				_squares[file, rank] = square;
			}
		}

		// Set up navigation relations between squares
		for (int rank = 0; rank < ChessConstants.BOARD_SIZE; rank++)
		{
			for (int file = 0; file < ChessConstants.BOARD_SIZE; file++)
			{
				var square = _squares[file, rank];

				// Left neighbor
				if (file > 0)
					World.Relate(square, _squares[file - 1, rank], new GamepadNavLeft());

				// Right neighbor
				if (file < ChessConstants.BOARD_SIZE - 1)
					World.Relate(square, _squares[file + 1, rank], new GamepadNavRight());

				// Up neighbor
				if (rank > 0)
					World.Relate(square, _squares[file, rank - 1], new GamepadNavUp());

				// Down neighbor
				if (rank < ChessConstants.BOARD_SIZE - 1)
					World.Relate(square, _squares[file, rank + 1], new GamepadNavDown());
			}
		}

		// Select first square and create cursor
		var firstSquare = _squares[0, 0];
		World.Set(firstSquare, new Selected());

		var cursor = World.CreateEntity();
		World.Set(cursor, new Cursor());
		var firstSquarePos = Get<Position>(firstSquare);
		World.Set(cursor, firstSquarePos);
		World.Set(cursor, new CanReceiveDirectionalInput());
		World.Set(cursor, new HasPlayerOwner(0));
		World.Set(cursor, new SpriteAnimation(SpriteAnimations.Effect_SpinningCoin));
		World.Set(cursor, new Depth(-10)); // Cursor renders above everything
	}

	public void SpawnInitialPieces()
	{
		// White pieces (bottom, rank 0-1)
		SpawnPiece(PieceType.Rook,   Player.White, new BoardPosition(0, 0));
		SpawnPiece(PieceType.Knight, Player.White, new BoardPosition(1, 0));
		SpawnPiece(PieceType.Bishop, Player.White, new BoardPosition(2, 0));
		SpawnPiece(PieceType.Queen,  Player.White, new BoardPosition(3, 0));
		SpawnPiece(PieceType.King,   Player.White, new BoardPosition(4, 0));
		SpawnPiece(PieceType.Bishop, Player.White, new BoardPosition(5, 0));
		SpawnPiece(PieceType.Knight, Player.White, new BoardPosition(6, 0));
		SpawnPiece(PieceType.Rook,   Player.White, new BoardPosition(7, 0));

		for (int file = 0; file < ChessConstants.BOARD_SIZE; file++)
			SpawnPiece(PieceType.Pawn, Player.White, new BoardPosition(file, 1));

		// Black pieces (top, rank 6-7)
		for (int file = 0; file < ChessConstants.BOARD_SIZE; file++)
			SpawnPiece(PieceType.Pawn, Player.Black, new BoardPosition(file, 6));

		SpawnPiece(PieceType.Rook,   Player.Black, new BoardPosition(0, 7));
		SpawnPiece(PieceType.Knight, Player.Black, new BoardPosition(1, 7));
		SpawnPiece(PieceType.Bishop, Player.Black, new BoardPosition(2, 7));
		SpawnPiece(PieceType.Queen,  Player.Black, new BoardPosition(3, 7));
		SpawnPiece(PieceType.King,   Player.Black, new BoardPosition(4, 7));
		SpawnPiece(PieceType.Bishop, Player.Black, new BoardPosition(5, 7));
		SpawnPiece(PieceType.Knight, Player.Black, new BoardPosition(6, 7));
		SpawnPiece(PieceType.Rook,   Player.Black, new BoardPosition(7, 7));
	}

	private void SpawnPiece(PieceType type, Player owner, BoardPosition boardPos)
	{
		var piece = World.CreateEntity();
		World.Set(piece, new ChessPiece(type, owner));
		World.Set(piece, boardPos);
		World.Set(piece, new HasNotMoved());

		// Get screen position from square
		var square = _squares[boardPos.File, boardPos.Rank];
		var screenPos = Get<Position>(square);
		World.Set(piece, screenPos);

		// Assign sprite based on piece type and owner
		// Using existing sprites as placeholders
		var spriteAnim = GetSpriteForPiece(type, owner);
		World.Set(piece, new SpriteAnimation(spriteAnim));
		World.Set(piece, new Depth(-5)); // Pieces render above squares, below cursor

		_pieces[boardPos.File, boardPos.Rank] = piece;
	}

	private SpriteAnimationInfo GetSpriteForPiece(PieceType type, Player owner)
	{
		// Use existing sprites as placeholders
		// White = Player 1, Black = Player 2
		var isWhite = owner == Player.White;

		return type switch
		{
			PieceType.Pawn   => isWhite ? SpriteAnimations.Char_Walk_Down : SpriteAnimations.Char2_Walk_Down,
			PieceType.Knight => isWhite ? SpriteAnimations.Char_Walk_Left : SpriteAnimations.Char2_Walk_Left,
			PieceType.Bishop => isWhite ? SpriteAnimations.Char_Walk_UpLeft : SpriteAnimations.Char2_Walk_UpLeft,
			PieceType.Rook   => isWhite ? SpriteAnimations.NPC_Drone_Fly_Down : SpriteAnimations.NPC_DroneEvil_Fly_Down,
			PieceType.Queen  => isWhite ? SpriteAnimations.Char_Walk_Up : SpriteAnimations.Char2_Walk_Up,
			PieceType.King   => isWhite ? SpriteAnimations.Char_Walk_Right : SpriteAnimations.Char2_Walk_Right,
			_ => SpriteAnimations.Pixel
		};
	}

	public Entity? GetPieceAt(BoardPosition pos)
	{
		if (!pos.IsValid())
			return null;

		var piece = _pieces[pos.File, pos.Rank];
		return piece.Equals(default(Entity)) ? null : piece;
	}

	public Entity GetSquareAt(BoardPosition pos)
	{
		if (!pos.IsValid())
			throw new ArgumentException($"Invalid board position: {pos}");

		return _squares[pos.File, pos.Rank];
	}

	public void MovePiece(BoardPosition from, BoardPosition to)
	{
		var piece = _pieces[from.File, from.Rank];
		_pieces[from.File, from.Rank] = default;
		_pieces[to.File, to.Rank] = piece;

		// Update position component
		var square = _squares[to.File, to.Rank];
		var screenPos = Get<Position>(square);
		Set(piece, screenPos);
		Set(piece, to);
	}

	public void RemovePiece(BoardPosition pos)
	{
		if (!pos.IsValid())
			return;

		var piece = _pieces[pos.File, pos.Rank];
		if (!piece.Equals(default(Entity)))
		{
			World.Destroy(piece);
			_pieces[pos.File, pos.Rank] = default;
		}
	}

	public override void Update(TimeSpan delta)
	{
		// No per-frame updates needed
	}
}
