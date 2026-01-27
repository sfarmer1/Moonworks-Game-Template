using System;
using MoonTools.ECS;
using MoonWorks.Input;
using Tactician.Components;

namespace Tactician.Systems;

public class ChessInputSystem : MoonTools.ECS.System
{
	private readonly ChessBoardSystem _boardSystem;
	private readonly ChessTurnSystem _turnSystem;
	private readonly Filter _selectedSquareFilter;

	private bool _confirmWasPressed;
	private bool _cancelWasPressed;

	public ChessInputSystem(World world, ChessBoardSystem boardSystem, ChessTurnSystem turnSystem) : base(world)
	{
		_boardSystem = boardSystem;
		_turnSystem = turnSystem;
		_selectedSquareFilter = FilterBuilder
			.Include<Selected>()
			.Include<BoardPosition>()
			.Build();
	}

	public override void Update(TimeSpan delta)
	{
		// Don't process input during AI turn or game over
		if (_turnSystem.CurrentPhase == GamePhase.AIThinking || _turnSystem.CurrentPhase == GamePhase.GameOver)
			return;

		// Read confirm button (Space, Enter, or A button on gamepad)
		var confirmPressed = Inputs.Keyboard.IsPressed(KeyCode.Space) ||
		                     Inputs.Keyboard.IsPressed(KeyCode.Return) ||
		                     (Inputs.GamepadExists(0) && Inputs.GetGamepad(0).IsPressed(GamepadButton.South));

		// Read cancel button (Escape, Backspace, or B button on gamepad)
		var cancelPressed = Inputs.Keyboard.IsPressed(KeyCode.Escape) ||
		                    Inputs.Keyboard.IsPressed(KeyCode.Backspace) ||
		                    (Inputs.GamepadExists(0) && Inputs.GetGamepad(0).IsPressed(GamepadButton.East));

		// Handle confirm (only on new press, not held)
		if (confirmPressed && !_confirmWasPressed)
		{
			HandleConfirm();
		}
		_confirmWasPressed = confirmPressed;

		// Handle cancel (only on new press, not held)
		if (cancelPressed && !_cancelWasPressed)
		{
			HandleCancel();
		}
		_cancelWasPressed = cancelPressed;
	}

	private void HandleConfirm()
	{
		// Get the currently selected square
		foreach (var selectedSquare in _selectedSquareFilter.Entities)
		{
			var boardPos = Get<BoardPosition>(selectedSquare);

			if (_turnSystem.CurrentPhase == GamePhase.SelectingPiece)
			{
				// Try to select the piece at this square
				var pieceEntity = _boardSystem.GetPieceAt(boardPos);
				if (pieceEntity.HasValue)
				{
					_turnSystem.SelectPiece(pieceEntity.Value);
				}
			}
			else if (_turnSystem.CurrentPhase == GamePhase.SelectingDestination)
			{
				// Try to move to this square
				_turnSystem.TryExecuteMove(boardPos);
			}

			break; // Only process first selected square
		}
	}

	private void HandleCancel()
	{
		// Cancel current action
		if (_turnSystem.CurrentPhase == GamePhase.SelectingDestination)
		{
			_turnSystem.DeselectPiece();
		}
	}
}
