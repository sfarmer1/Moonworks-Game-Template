using MoonTools.ECS;
using Tactician;
using Tactician.Components;
using Tactician.Messages;

namespace Tactician.Tests;

/// <summary>
/// Tests for reset logic that can run without graphics initialization.
/// Tests the ECS message and state management aspects of reset functionality.
/// Updated to test component-based input state tracking and DestroyedOnReset pattern.
/// </summary>
public class ResetLogicTests
{
	[Fact]
	public void Reset_Message_CanBeSent()
	{
		// Arrange
		var world = new World();

		// Act
		world.Send(new ResetGameMessage());

		// Assert
		Assert.True(world.SomeMessage<ResetGameMessage>());
		world.Dispose();
	}

	[Fact]
	public void GameState_EntityCanBeCreated_WithCorrectComponents()
	{
		// Arrange
		var world = new World();
		var gameState = world.CreateEntity();

		// Act
		world.Set(gameState, new ChessGameState());
		world.Set(gameState, new CurrentTurn(Player.White));
		world.Set(gameState, new CurrentGamePhase(GamePhase.SelectingPiece));
		world.Set(gameState, new AiConfig(false, Player.Black));
		world.Set(gameState, new DestroyedOnReset()); // New component

		// Assert
		Assert.True(world.Has<ChessGameState>(gameState));
		Assert.True(world.Has<CurrentTurn>(gameState));
		Assert.True(world.Has<CurrentGamePhase>(gameState));
		Assert.True(world.Has<AiConfig>(gameState));
		Assert.True(world.Has<DestroyedOnReset>(gameState));
		Assert.Equal(Player.White, world.Get<CurrentTurn>(gameState).Player);
		Assert.Equal(GamePhase.SelectingPiece, world.Get<CurrentGamePhase>(gameState).Phase);

		world.Dispose();
	}

	[Fact]
	public void GameState_CanBeDestroyed_AndRecreated()
	{
		// Arrange
		var world = new World();
		var filter = world.FilterBuilder.Include<ChessGameState>().Build();

		var gameState = world.CreateEntity();
		world.Set(gameState, new ChessGameState());
		world.Set(gameState, new CurrentTurn(Player.Black)); // Set to black
		world.Set(gameState, new CurrentGamePhase(GamePhase.AIThinking));
		world.Set(gameState, new DestroyedOnReset());

		// Act - Simulate reset destroying and recreating
		world.Destroy(gameState);
		var newGameState = world.CreateEntity();
		world.Set(newGameState, new ChessGameState());
		world.Set(newGameState, new CurrentTurn(Player.White)); // Reset to white
		world.Set(newGameState, new CurrentGamePhase(GamePhase.SelectingPiece));
		world.Set(newGameState, new DestroyedOnReset());

		// Assert
		int count = 0;
		Entity? foundEntity = null;
		foreach (var entity in filter.Entities)
		{
			count++;
			foundEntity = entity;
		}

		Assert.Equal(1, count);
		Assert.NotNull(foundEntity);
		Assert.Equal(Player.White, world.Get<CurrentTurn>(foundEntity.Value).Player);
		Assert.Equal(GamePhase.SelectingPiece, world.Get<CurrentGamePhase>(foundEntity.Value).Phase);

		world.Dispose();
	}

	[Fact]
	public void ChessPiece_CanBeCreated_WithCorrectData()
	{
		// Arrange
		var world = new World();
		var piece = world.CreateEntity();

		// Act
		world.Set(piece, new ChessPiece(PieceType.Pawn, Player.White));
		world.Set(piece, new BoardPosition(1, 1));
		world.Set(piece, new HasNotMoved());
		world.Set(piece, new DestroyedOnReset());

		// Assert
		Assert.True(world.Has<ChessPiece>(piece));
		Assert.True(world.Has<BoardPosition>(piece));
		Assert.True(world.Has<HasNotMoved>(piece));
		Assert.True(world.Has<DestroyedOnReset>(piece));

		var chessPiece = world.Get<ChessPiece>(piece);
		Assert.Equal(PieceType.Pawn, chessPiece.Type);
		Assert.Equal(Player.White, chessPiece.Owner);

		world.Dispose();
	}

	[Fact]
	public void MultipleEntities_CanBeFilteredAndCounted()
	{
		// Arrange
		var world = new World();
		var filter = world.FilterBuilder.Include<ChessPiece>().Build();

		// Create 16 white pieces
		for (int i = 0; i < 16; i++)
		{
			var piece = world.CreateEntity();
			world.Set(piece, new ChessPiece(PieceType.Pawn, Player.White));
			world.Set(piece, new DestroyedOnReset());
		}

		// Create 16 black pieces
		for (int i = 0; i < 16; i++)
		{
			var piece = world.CreateEntity();
			world.Set(piece, new ChessPiece(PieceType.Pawn, Player.Black));
			world.Set(piece, new DestroyedOnReset());
		}

		// Act
		int totalCount = 0;
		int whiteCount = 0;
		int blackCount = 0;

		foreach (var entity in filter.Entities)
		{
			totalCount++;
			var piece = world.Get<ChessPiece>(entity);
			if (piece.Owner == Player.White)
				whiteCount++;
			else
				blackCount++;
		}

		// Assert
		Assert.Equal(32, totalCount);
		Assert.Equal(16, whiteCount);
		Assert.Equal(16, blackCount);

		world.Dispose();
	}

	[Fact]
	public void Entities_CanBeDestroyedAndRecounted()
	{
		// Arrange
		var world = new World();
		var entities = new List<Entity>();

		for (int i = 0; i < 10; i++)
		{
			var entity = world.CreateEntity();
			world.Set(entity, new ChessPiece(PieceType.Pawn, Player.White));
			world.Set(entity, new DestroyedOnReset());
			entities.Add(entity);
		}

		// Act - Destroy all entities
		foreach (var entity in entities)
		{
			world.Destroy(entity);
		}

		// Assert - Count should be 0
		var filter = world.FilterBuilder.Include<ChessPiece>().Build();
		int count = 0;
		foreach (var _ in filter.Entities)
		{
			count++;
		}

		Assert.Equal(0, count);

		world.Dispose();
	}

	[Fact]
	public void Components_CanBeAdded_AndRemoved()
	{
		// Arrange
		var world = new World();
		var piece = world.CreateEntity();
		world.Set(piece, new ChessPiece(PieceType.Pawn, Player.White));
		world.Set(piece, new HasNotMoved());

		// Act - Remove HasNotMoved (simulating piece movement)
		world.Remove<HasNotMoved>(piece);

		// Assert
		Assert.False(world.Has<HasNotMoved>(piece));
		Assert.True(world.Has<ChessPiece>(piece)); // Other component still present

		// Act - Add it back (simulating reset)
		world.Set(piece, new HasNotMoved());

		// Assert
		Assert.True(world.Has<HasNotMoved>(piece));

		world.Dispose();
	}

	[Fact]
	public void EnPassantTarget_CanBeAddedAndCleared()
	{
		// Arrange
		var world = new World();
		var pawn = world.CreateEntity();
		world.Set(pawn, new ChessPiece(PieceType.Pawn, Player.White));
		world.Set(pawn, new EnPassantTarget());

		// Act - Clear en passant (simulating reset)
		world.Remove<EnPassantTarget>(pawn);

		// Assert
		Assert.False(world.Has<EnPassantTarget>(pawn));

		world.Dispose();
	}

	[Fact]
	public void ValidMoveHighlight_CanBeAddedToMultipleSquares_AndCleared()
	{
		// Arrange
		var world = new World();
		var filter = world.FilterBuilder.Include<ValidMoveHighlight>().Build();
		var squares = new List<Entity>();

		for (int i = 0; i < 5; i++)
		{
			var square = world.CreateEntity();
			world.Set(square, new BoardPosition(i, 2));
			world.Set(square, new ValidMoveHighlight());
			world.Set(square, new DestroyedOnReset());
			squares.Add(square);
		}

		// Verify highlights exist
		int countBefore = 0;
		foreach (var _ in filter.Entities)
		{
			countBefore++;
		}
		Assert.Equal(5, countBefore);

		// Act - Clear all highlights (simulating reset)
		foreach (var square in squares)
		{
			world.Remove<ValidMoveHighlight>(square);
		}

		// Assert
		int countAfter = 0;
		foreach (var _ in filter.Entities)
		{
			countAfter++;
		}
		Assert.Equal(0, countAfter);

		world.Dispose();
	}

	[Fact]
	public void SelectedPieceRef_CanBeSetAndCleared()
	{
		// Arrange
		var world = new World();
		var gameState = world.CreateEntity();
		world.Set(gameState, new ChessGameState());

		var piece = world.CreateEntity();
		world.Set(piece, new ChessPiece(PieceType.Pawn, Player.White));

		// Act - Set selected piece
		world.Set(gameState, new SelectedPieceRef(piece));
		Assert.True(world.Has<SelectedPieceRef>(gameState));

		// Act - Clear selected piece (simulating reset)
		world.Remove<SelectedPieceRef>(gameState);

		// Assert
		Assert.False(world.Has<SelectedPieceRef>(gameState));

		world.Dispose();
	}

	[Fact]
	public void TurnOrder_CanBeResetToWhite()
	{
		// Arrange
		var world = new World();
		var gameState = world.CreateEntity();
		world.Set(gameState, new ChessGameState());
		world.Set(gameState, new CurrentTurn(Player.Black)); // Simulate it being Black's turn

		// Act - Reset to White's turn
		world.Set(gameState, new CurrentTurn(Player.White));

		// Assert
		Assert.Equal(Player.White, world.Get<CurrentTurn>(gameState).Player);

		world.Dispose();
	}

	[Fact]
	public void GamePhase_CanBeResetToSelectingPiece()
	{
		// Arrange
		var world = new World();
		var gameState = world.CreateEntity();
		world.Set(gameState, new ChessGameState());
		world.Set(gameState, new CurrentGamePhase(GamePhase.GameOver)); // Simulate game over

		// Act - Reset to selecting piece phase
		world.Set(gameState, new CurrentGamePhase(GamePhase.SelectingPiece));

		// Assert
		Assert.Equal(GamePhase.SelectingPiece, world.Get<CurrentGamePhase>(gameState).Phase);

		world.Dispose();
	}

	[Fact]
	public void CursorEntity_CanBeDestroyed_AndRecreated()
	{
		// Arrange
		var world = new World();
		var filter = world.FilterBuilder.Include<Cursor>().Build();

		var cursor = world.CreateEntity();
		world.Set(cursor, new Cursor());
		world.Set(cursor, new Position(100, 100));
		world.Set(cursor, new DestroyedOnReset());

		// Act - Destroy and recreate (simulating reset)
		world.Destroy(cursor);
		var newCursor = world.CreateEntity();
		world.Set(newCursor, new Cursor());
		world.Set(newCursor, new Position(0, 0)); // Reset position
		world.Set(newCursor, new DestroyedOnReset());

		// Assert - Only one cursor exists
		int count = 0;
		Entity? foundCursor = null;
		foreach (var entity in filter.Entities)
		{
			count++;
			foundCursor = entity;
		}

		Assert.Equal(1, count);
		Assert.NotNull(foundCursor);
		var position = world.Get<Position>(foundCursor.Value);
		Assert.Equal(0, position.X);
		Assert.Equal(0, position.Y);

		world.Dispose();
	}

	[Fact]
	public void BoardPosition_ValidatesCorrectly()
	{
		// Test valid positions
		Assert.True(new BoardPosition(0, 0).IsValid());
		Assert.True(new BoardPosition(7, 7).IsValid());
		Assert.True(new BoardPosition(3, 4).IsValid());

		// Test invalid positions
		Assert.False(new BoardPosition(-1, 0).IsValid());
		Assert.False(new BoardPosition(0, -1).IsValid());
		Assert.False(new BoardPosition(8, 0).IsValid());
		Assert.False(new BoardPosition(0, 8).IsValid());
	}

	// === New Tests for Component-Based Input State Tracking ===

	[Fact]
	public void ResetButtonWasPressed_CanBeSetAndRemoved()
	{
		// Arrange
		var world = new World();
		var gameState = world.CreateEntity();
		world.Set(gameState, new ChessGameState());

		// Act - Set button pressed state
		world.Set(gameState, new ResetButtonWasPressed());

		// Assert - Component exists
		Assert.True(world.Has<ResetButtonWasPressed>(gameState));

		// Act - Remove button pressed state (button released)
		world.Remove<ResetButtonWasPressed>(gameState);

		// Assert - Component removed
		Assert.False(world.Has<ResetButtonWasPressed>(gameState));

		world.Dispose();
	}

	[Fact]
	public void ConfirmButtonWasPressed_CanBeSetAndRemoved()
	{
		// Arrange
		var world = new World();
		var gameState = world.CreateEntity();
		world.Set(gameState, new ChessGameState());

		// Act - Set button pressed state
		world.Set(gameState, new ConfirmButtonWasPressed());

		// Assert
		Assert.True(world.Has<ConfirmButtonWasPressed>(gameState));

		// Act - Remove button pressed state
		world.Remove<ConfirmButtonWasPressed>(gameState);

		// Assert
		Assert.False(world.Has<ConfirmButtonWasPressed>(gameState));

		world.Dispose();
	}

	[Fact]
	public void CancelButtonWasPressed_CanBeSetAndRemoved()
	{
		// Arrange
		var world = new World();
		var gameState = world.CreateEntity();
		world.Set(gameState, new ChessGameState());

		// Act - Set button pressed state
		world.Set(gameState, new CancelButtonWasPressed());

		// Assert
		Assert.True(world.Has<CancelButtonWasPressed>(gameState));

		// Act - Remove button pressed state
		world.Remove<CancelButtonWasPressed>(gameState);

		// Assert
		Assert.False(world.Has<CancelButtonWasPressed>(gameState));

		world.Dispose();
	}

	[Fact]
	public void InputStateComponents_SimulateButtonPressDetection()
	{
		// Arrange
		var world = new World();
		var gameState = world.CreateEntity();
		world.Set(gameState, new ChessGameState());

		// Simulate frame 1: button not pressed
		var buttonPressed = false;
		var buttonWasPressed = world.Has<ResetButtonWasPressed>(gameState);

		// Assert - No press detected
		Assert.False(buttonPressed);
		Assert.False(buttonWasPressed);

		// Simulate frame 2: button pressed (rising edge)
		buttonPressed = true;
		buttonWasPressed = world.Has<ResetButtonWasPressed>(gameState);

		// Assert - New press detected (rising edge)
		Assert.True(buttonPressed && !buttonWasPressed);

		// Update component state
		world.Set(gameState, new ResetButtonWasPressed());

		// Simulate frame 3: button still held
		buttonPressed = true;
		buttonWasPressed = world.Has<ResetButtonWasPressed>(gameState);

		// Assert - No new press (button held)
		Assert.False(buttonPressed && !buttonWasPressed);

		// Simulate frame 4: button released
		buttonPressed = false;
		world.Remove<ResetButtonWasPressed>(gameState);
		buttonWasPressed = world.Has<ResetButtonWasPressed>(gameState);

		// Assert - Button released
		Assert.False(buttonPressed);
		Assert.False(buttonWasPressed);

		world.Dispose();
	}

	// === New Tests for DestroyedOnReset Pattern ===

	[Fact]
	public void DestroyedOnReset_CanBeAppliedToEntity()
	{
		// Arrange
		var world = new World();
		var entity = world.CreateEntity();

		// Act
		world.Set(entity, new DestroyedOnReset());

		// Assert
		Assert.True(world.Has<DestroyedOnReset>(entity));

		world.Dispose();
	}

	[Fact]
	public void DestroyedOnReset_FilterCanFindMarkedEntities()
	{
		// Arrange
		var world = new World();
		var filter = world.FilterBuilder.Include<DestroyedOnReset>().Build();

		// Create entities with and without DestroyedOnReset
		var markedEntity1 = world.CreateEntity();
		world.Set(markedEntity1, new DestroyedOnReset());

		var markedEntity2 = world.CreateEntity();
		world.Set(markedEntity2, new DestroyedOnReset());

		var unmarkedEntity = world.CreateEntity();
		world.Set(unmarkedEntity, new ChessGameState()); // No DestroyedOnReset

		// Act - Count entities in filter
		int count = 0;
		foreach (var _ in filter.Entities)
		{
			count++;
		}

		// Assert - Only marked entities found
		Assert.Equal(2, count);

		world.Dispose();
	}

	[Fact]
	public void DestroyedOnReset_AllMarkedEntitiesCanBeDestroyed()
	{
		// Arrange
		var world = new World();
		var filter = world.FilterBuilder.Include<DestroyedOnReset>().Build();
		var allPiecesFilter = world.FilterBuilder.Include<ChessPiece>().Build();

		// Create 10 entities marked for reset
		for (int i = 0; i < 10; i++)
		{
			var entity = world.CreateEntity();
			world.Set(entity, new ChessPiece(PieceType.Pawn, Player.White));
			world.Set(entity, new DestroyedOnReset());
		}

		// Create 5 entities NOT marked for reset
		for (int i = 0; i < 5; i++)
		{
			var entity = world.CreateEntity();
			world.Set(entity, new ChessPiece(PieceType.King, Player.Black));
			// No DestroyedOnReset component
		}

		// Verify 10 entities marked
		int markedCount = 0;
		var entitiesToDestroy = new List<Entity>();
		foreach (var entity in filter.Entities)
		{
			markedCount++;
			entitiesToDestroy.Add(entity);
		}
		Assert.Equal(10, markedCount);

		// Act - Destroy all marked entities
		foreach (var entity in entitiesToDestroy)
		{
			world.Destroy(entity);
		}

		// Assert - All marked entities destroyed
		int remainingMarked = 0;
		foreach (var _ in filter.Entities)
		{
			remainingMarked++;
		}
		Assert.Equal(0, remainingMarked);

		// Verify unmarked entities still exist
		int remainingTotal = 0;
		foreach (var _ in allPiecesFilter.Entities)
		{
			remainingTotal++;
		}
		Assert.Equal(5, remainingTotal);

		world.Dispose();
	}

	[Fact]
	public void ResetSimulation_CompleteWorkflow()
	{
		// Arrange - Create a "game in progress" state
		var world = new World();

		// Create filters BEFORE creating entities
		var gameStateFilter = world.FilterBuilder.Include<ChessGameState>().Build();
		var pieceFilter = world.FilterBuilder.Include<ChessPiece>().Build();
		var highlightFilter = world.FilterBuilder.Include<ValidMoveHighlight>().Build();
		var resetFilter = world.FilterBuilder.Include<DestroyedOnReset>().Build();

		// Create game state (Black's turn, AI thinking)
		var oldGameState = world.CreateEntity();
		world.Set(oldGameState, new ChessGameState());
		world.Set(oldGameState, new CurrentTurn(Player.Black));
		world.Set(oldGameState, new CurrentGamePhase(GamePhase.AIThinking));
		world.Set(oldGameState, new DestroyedOnReset()); // Marked for reset

		// Create some pieces
		for (int i = 0; i < 30; i++) // Simulate some pieces captured
		{
			var piece = world.CreateEntity();
			world.Set(piece, new ChessPiece(PieceType.Pawn, i < 15 ? Player.White : Player.Black));
			world.Set(piece, new BoardPosition(i % 8, i / 8));
			world.Set(piece, new DestroyedOnReset()); // Marked for reset
		}

		// Add some game state
		var square = world.CreateEntity();
		world.Set(square, new BoardPosition(2, 3));
		world.Set(square, new ValidMoveHighlight());
		world.Set(square, new DestroyedOnReset()); // Marked for reset

		// Count entities marked for reset
		int markedForReset = 0;
		foreach (var _ in resetFilter.Entities)
		{
			markedForReset++;
		}
		Assert.Equal(32, markedForReset); // 1 gameState + 30 pieces + 1 square

		// Act - Simulate reset using DestroyedOnReset pattern
		world.Send(new ResetGameMessage());

		// Destroy all entities marked with DestroyedOnReset
		var entitiesToDestroy = new List<Entity>();
		foreach (var entity in resetFilter.Entities)
		{
			entitiesToDestroy.Add(entity);
		}
		foreach (var entity in entitiesToDestroy)
		{
			world.Destroy(entity);
		}

		// Create fresh game state
		var newGameState = world.CreateEntity();
		world.Set(newGameState, new ChessGameState());
		world.Set(newGameState, new CurrentTurn(Player.White));
		world.Set(newGameState, new CurrentGamePhase(GamePhase.SelectingPiece));
		world.Set(newGameState, new DestroyedOnReset());

		// Create all 32 pieces
		for (int i = 0; i < 32; i++)
		{
			var piece = world.CreateEntity();
			world.Set(piece, new ChessPiece(PieceType.Pawn, i < 16 ? Player.White : Player.Black));
			world.Set(piece, new BoardPosition(i % 8, i < 16 ? i / 8 : 6 + (i - 16) / 8));
			world.Set(piece, new HasNotMoved());
			world.Set(piece, new DestroyedOnReset());
		}

		// Assert - Verify reset state
		Assert.True(world.SomeMessage<ResetGameMessage>());

		int gameStateCount = 0;
		Entity? currentGameState = null;
		foreach (var entity in gameStateFilter.Entities)
		{
			gameStateCount++;
			currentGameState = entity;
		}

		Assert.Equal(1, gameStateCount);
		Assert.NotNull(currentGameState);
		Assert.Equal(Player.White, world.Get<CurrentTurn>(currentGameState.Value).Player);
		Assert.Equal(GamePhase.SelectingPiece, world.Get<CurrentGamePhase>(currentGameState.Value).Phase);

		int pieceCount = 0;
		int piecesWithNotMoved = 0;
		foreach (var entity in pieceFilter.Entities)
		{
			pieceCount++;
			if (world.Has<HasNotMoved>(entity))
				piecesWithNotMoved++;
		}

		Assert.Equal(32, pieceCount);
		Assert.Equal(32, piecesWithNotMoved);

		int highlightCount = 0;
		foreach (var _ in highlightFilter.Entities)
		{
			highlightCount++;
		}
		Assert.Equal(0, highlightCount);

		// Verify all new entities are marked for reset
		int newMarkedCount = 0;
		foreach (var _ in resetFilter.Entities)
		{
			newMarkedCount++;
		}
		Assert.Equal(33, newMarkedCount); // 1 gameState + 32 pieces

		world.Dispose();
	}

	[Fact]
	public void InputStateComponents_ClearOnReset()
	{
		// Arrange
		var world = new World();
		var oldGameState = world.CreateEntity();
		world.Set(oldGameState, new ChessGameState());
		world.Set(oldGameState, new ResetButtonWasPressed());
		world.Set(oldGameState, new ConfirmButtonWasPressed());
		world.Set(oldGameState, new CancelButtonWasPressed());
		world.Set(oldGameState, new DestroyedOnReset());

		// Act - Simulate reset
		world.Destroy(oldGameState);
		var newGameState = world.CreateEntity();
		world.Set(newGameState, new ChessGameState());
		world.Set(newGameState, new DestroyedOnReset());
		// Don't set any button state components

		// Assert - New game state has no input state
		Assert.False(world.Has<ResetButtonWasPressed>(newGameState));
		Assert.False(world.Has<ConfirmButtonWasPressed>(newGameState));
		Assert.False(world.Has<CancelButtonWasPressed>(newGameState));

		world.Dispose();
	}
}
