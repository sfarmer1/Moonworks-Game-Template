using System;
using System.Collections.Generic;
using MoonTools.ECS;
using MoonWorks;
using Tactician.Components;
using Tactician.Data;
using Tactician.Messages;

namespace Tactician.Systems;

public class ChessTurnSystem : MoonTools.ECS.System
{
	private readonly ChessBoardSystem _boardSystem;
	private readonly MoveValidationSystem _validationSystem;

	private Player _currentPlayer;
	private GamePhase _gamePhase;
	private Entity? _selectedPiece;
	private List<ChessMove> _validMoves;
	private bool _isAIEnabled;
	private Player _aiPlayer;

	private readonly Filter _activePieceFilter;
	private readonly Filter _validMoveHighlightFilter;
	private readonly Filter _selectedFilter;

	public Player CurrentPlayer => _currentPlayer;
	public GamePhase CurrentPhase => _gamePhase;
	public Entity? SelectedPiece => _selectedPiece;
	public List<ChessMove> ValidMoves => _validMoves ?? new List<ChessMove>();
	public bool IsAIEnabled => _isAIEnabled;
	public Player AIPlayer => _aiPlayer;

	public ChessTurnSystem(World world, ChessBoardSystem boardSystem, MoveValidationSystem validationSystem) : base(world)
	{
		_boardSystem = boardSystem;
		_validationSystem = validationSystem;
		_currentPlayer = Player.White;
		_gamePhase = GamePhase.SelectingPiece;
		_validMoves = new List<ChessMove>();
		_isAIEnabled = false;
		_aiPlayer = Player.Black;

		_activePieceFilter = FilterBuilder.Include<ActivePiece>().Build();
		_validMoveHighlightFilter = FilterBuilder.Include<ValidMoveHighlight>().Build();
		_selectedFilter = FilterBuilder.Include<Selected>().Build();
	}

	public void EnableAI(Player aiPlayer)
	{
		_isAIEnabled = true;
		_aiPlayer = aiPlayer;
	}

	public void DisableAI()
	{
		_isAIEnabled = false;
	}

	public void SelectPiece(Entity pieceEntity)
	{
		if (_gamePhase != GamePhase.SelectingPiece)
			return;

		if (!Has<ChessPiece>(pieceEntity))
			return;

		var piece = Get<ChessPiece>(pieceEntity);
		if (piece.Owner != _currentPlayer)
		{
			Logger.LogInfo($"Cannot select opponent's piece");
			return;
		}

		// Deselect previous piece if any
		if (_selectedPiece.HasValue)
			DeselectPiece();

		_selectedPiece = pieceEntity;
		World.Set(pieceEntity, new ActivePiece());

		// Calculate valid moves
		_validMoves = _validationSystem.GetValidMoves(pieceEntity);

		if (_validMoves.Count == 0)
		{
			Logger.LogInfo("Selected piece has no valid moves");
			DeselectPiece();
			return;
		}

		// Highlight valid move squares
		foreach (var move in _validMoves)
		{
			var square = _boardSystem.GetSquareAt(move.To);
			World.Set(square, new ValidMoveHighlight());
		}

		_gamePhase = GamePhase.SelectingDestination;
		Logger.LogInfo($"Selected {piece.Type} with {_validMoves.Count} valid moves");
	}

	public void DeselectPiece()
	{
		if (!_selectedPiece.HasValue)
			return;

		// Remove ActivePiece component
		if (Has<ActivePiece>(_selectedPiece.Value))
			World.Remove<ActivePiece>(_selectedPiece.Value);

		// Remove all valid move highlights
		foreach (var highlightEntity in _validMoveHighlightFilter.Entities)
		{
			World.Remove<ValidMoveHighlight>(highlightEntity);
		}

		_selectedPiece = null;
		_validMoves.Clear();
		_gamePhase = GamePhase.SelectingPiece;
		Logger.LogInfo("Deselected piece");
	}

	public bool TryExecuteMove(BoardPosition targetPosition)
	{
		if (_gamePhase != GamePhase.SelectingDestination || !_selectedPiece.HasValue)
			return false;

		// Find the move that matches the target position
		ChessMove? selectedMove = null;
		foreach (var move in _validMoves)
		{
			if (move.To.File == targetPosition.File && move.To.Rank == targetPosition.Rank)
			{
				selectedMove = move;
				break;
			}
		}

		if (!selectedMove.HasValue)
		{
			Logger.LogInfo("Invalid move target");
			return false;
		}

		// Move execution will be handled by ChessMoveExecutionSystem
		Send(new ExecuteMoveMessage(selectedMove.Value));

		// Clear selection
		DeselectPiece();

		// End turn
		EndTurn();

		return true;
	}

	private void EndTurn()
	{
		// Switch player
		_currentPlayer = _currentPlayer == Player.White ? Player.Black : Player.White;
		_gamePhase = GamePhase.SelectingPiece;

		// Check for check/checkmate
		if (_validationSystem.IsCheckmate(_currentPlayer))
		{
			_gamePhase = GamePhase.GameOver;
			var winner = _currentPlayer == Player.White ? Player.Black : Player.White;
			Logger.LogInfo($"Checkmate! {winner} wins!");
			Send(new CheckmateMessage(winner));
			return;
		}

		if (_validationSystem.IsStalemate(_currentPlayer))
		{
			_gamePhase = GamePhase.GameOver;
			Logger.LogInfo("Stalemate!");
			Send(new StalemateMessage());
			return;
		}

		if (_validationSystem.IsInCheck(_currentPlayer))
		{
			Logger.LogInfo($"{_currentPlayer} is in check!");
			Send(new CheckMessage(_currentPlayer));
		}

		// If AI's turn, switch to AI thinking phase
		if (_isAIEnabled && _currentPlayer == _aiPlayer)
		{
			_gamePhase = GamePhase.AIThinking;
			Logger.LogInfo("AI is thinking...");
		}

		Logger.LogInfo($"Turn: {_currentPlayer}");
	}

	public void HandleAIMove(ChessMove move)
	{
		if (_gamePhase != GamePhase.AIThinking)
			return;

		Send(new ExecuteMoveMessage(move));
		EndTurn();
	}

	public override void Update(TimeSpan delta)
	{
		// No per-frame updates needed, actions are event-driven
	}
}
