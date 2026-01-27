using System;
using MoonTools.ECS;
using MoonWorks.Input;
using Tactician.Components;

namespace Tactician.Systems;

public class ChessInputSystem : MoonTools.ECS.System
{
	private readonly ChessBoardSystem _boardSystem;
	private readonly MoveValidationSystem _validationSystem;
	private readonly Inputs _inputs;
	private readonly Filter _gameStateFilter;
	private readonly Filter _selectedSquareFilter;
	private readonly Filter _validMoveHighlightFilter;

	private bool _confirmWasPressed;
	private bool _cancelWasPressed;

	public ChessInputSystem(World world, ChessBoardSystem boardSystem, MoveValidationSystem validationSystem, Inputs inputs) : base(world)
	{
		_boardSystem = boardSystem;
		_validationSystem = validationSystem;
		_inputs = inputs;
		_gameStateFilter = FilterBuilder.Include<ChessGameState>().Build();
		_selectedSquareFilter = FilterBuilder
			.Include<Selected>()
			.Include<BoardPosition>()
			.Build();
		_validMoveHighlightFilter = FilterBuilder.Include<ValidMoveHighlight>().Build();
	}

	public override void Update(TimeSpan delta)
	{
		// Get game state
		Entity? gameStateEntity = null;
		foreach (var entity in _gameStateFilter.Entities)
		{
			gameStateEntity = entity;
			break;
		}

		if (!gameStateEntity.HasValue)
			return;

		var gameState = gameStateEntity.Value;

		// Don't process input during AI turn or game over
		if (Has<CurrentGamePhase>(gameState))
		{
			var phase = Get<CurrentGamePhase>(gameState).Phase;
			if (phase == GamePhase.AIThinking || phase == GamePhase.GameOver)
				return;
		}

		// Read confirm button (Space, Enter, or A button on gamepad)
		var confirmPressed = _inputs.Keyboard.IsPressed(KeyCode.Space) ||
		                     _inputs.Keyboard.IsPressed(KeyCode.Return) ||
		                     (_inputs.GamepadExists(0) && _inputs.GetGamepad(0).A.IsPressed);

		// Read cancel button (Escape, Backspace, or B button on gamepad)
		var cancelPressed = _inputs.Keyboard.IsPressed(KeyCode.Escape) ||
		                    _inputs.Keyboard.IsPressed(KeyCode.Backspace) ||
		                    (_inputs.GamepadExists(0) && _inputs.GetGamepad(0).B.IsPressed);

		// Handle confirm (only on new press, not held)
		if (confirmPressed && !_confirmWasPressed)
		{
			HandleConfirm(gameState);
		}
		_confirmWasPressed = confirmPressed;

		// Handle cancel (only on new press, not held)
		if (cancelPressed && !_cancelWasPressed)
		{
			HandleCancel(gameState);
		}
		_cancelWasPressed = cancelPressed;
	}

	private void HandleConfirm(Entity gameState)
	{
		if (!Has<CurrentGamePhase>(gameState) || !Has<CurrentTurn>(gameState))
			return;

		var phase = Get<CurrentGamePhase>(gameState).Phase;
		var currentPlayer = Get<CurrentTurn>(gameState).Player;

		// Get the currently selected square
		foreach (var selectedSquare in _selectedSquareFilter.Entities)
		{
			var boardPos = Get<BoardPosition>(selectedSquare);

			if (phase == GamePhase.SelectingPiece)
			{
				// Try to select the piece at this square
				var pieceEntity = _boardSystem.GetPieceAt(boardPos);
				if (pieceEntity.HasValue && Has<ChessPiece>(pieceEntity.Value))
				{
					var piece = Get<ChessPiece>(pieceEntity.Value);

					// Only select pieces belonging to current player
					if (piece.Owner != currentPlayer)
					{
						MoonWorks.Logger.LogInfo("Cannot select opponent's piece");
						break;
					}

					// Get valid moves
					var validMoves = _validationSystem.GetValidMoves(pieceEntity.Value);
					if (validMoves.Count == 0)
					{
						MoonWorks.Logger.LogInfo("Selected piece has no valid moves");
						break;
					}

					// Set selected piece and change phase
					Set(gameState, new SelectedPieceRef(pieceEntity.Value));
					Set(gameState, new CurrentGamePhase(GamePhase.SelectingDestination));
					Set(pieceEntity.Value, new ActivePiece());

					// Highlight valid move squares
					foreach (var move in validMoves)
					{
						var square = _boardSystem.GetSquareAt(move.To);
						Set(square, new ValidMoveHighlight());
					}

					MoonWorks.Logger.LogInfo($"Selected {piece.Type} with {validMoves.Count} valid moves");
				}
			}
			else if (phase == GamePhase.SelectingDestination)
			{
				// Check if this square is a valid move target
				if (Has<ValidMoveHighlight>(selectedSquare))
				{
					// Get the selected piece
					var selectedPieceRef = Get<SelectedPieceRef>(gameState);
					var pieceEntity = selectedPieceRef.PieceEntity;

					if (!Has<BoardPosition>(pieceEntity))
						break;

					var fromPos = Get<BoardPosition>(pieceEntity);

					// Find the matching move
					var validMoves = _validationSystem.GetValidMoves(pieceEntity);
					foreach (var move in validMoves)
					{
						if (move.To.File == boardPos.File && move.To.Rank == boardPos.Rank)
						{
							// Set pending move for execution system to process
							Set(gameState, new PendingMove(move));
							MoonWorks.Logger.LogInfo($"Move queued: {move.PieceType} from ({move.From.File},{move.From.Rank}) to ({move.To.File},{move.To.Rank})");
							break;
						}
					}
				}
			}

			break; // Only process first selected square
		}
	}

	private void HandleCancel(Entity gameState)
	{
		if (!Has<CurrentGamePhase>(gameState))
			return;

		var phase = Get<CurrentGamePhase>(gameState).Phase;

		// Cancel current action
		if (phase == GamePhase.SelectingDestination)
		{
			// Clear selected piece
			if (Has<SelectedPieceRef>(gameState))
			{
				var selectedPieceRef = Get<SelectedPieceRef>(gameState);
				if (Has<ActivePiece>(selectedPieceRef.PieceEntity))
					Remove<ActivePiece>(selectedPieceRef.PieceEntity);
				Remove<SelectedPieceRef>(gameState);
			}

			// Clear valid move highlights
			foreach (var highlightEntity in _validMoveHighlightFilter.Entities)
			{
				Remove<ValidMoveHighlight>(highlightEntity);
			}

			// Return to selecting piece phase
			Set(gameState, new CurrentGamePhase(GamePhase.SelectingPiece));
			MoonWorks.Logger.LogInfo("Deselected piece");
		}
	}
}
