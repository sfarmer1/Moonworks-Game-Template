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
	private readonly Filter _gameStateFilter;
	private readonly Filter _selectedPieceFilter;

	public ChessTurnSystem(World world, ChessBoardSystem boardSystem, MoveValidationSystem validationSystem) : base(world)
	{
		_boardSystem = boardSystem;
		_validationSystem = validationSystem;
		_gameStateFilter = FilterBuilder.Include<ChessGameState>().Build();
		_selectedPieceFilter = FilterBuilder.Include<SelectedPieceRef>().Build();
	}

	public override void Update(TimeSpan delta)
	{
		// Get the game state entity
		Entity? gameStateEntity = null;
		foreach (var entity in _gameStateFilter.Entities)
		{
			gameStateEntity = entity;
			break;
		}

		if (!gameStateEntity.HasValue)
			return;

		var gameState = gameStateEntity.Value;

		// Check for game over conditions at the start of each turn
		if (Has<CurrentTurn>(gameState))
		{
			var currentPlayer = Get<CurrentTurn>(gameState).Player;

			if (_validationSystem.IsCheckmate(currentPlayer))
			{
				Set(gameState, new CurrentGamePhase(GamePhase.GameOver));
				var winner = currentPlayer == Player.White ? Player.Black : Player.White;
				Logger.LogInfo($"Checkmate! {winner} wins!");
				Send(new CheckmateMessage(winner));
				return;
			}

			if (_validationSystem.IsStalemate(currentPlayer))
			{
				Set(gameState, new CurrentGamePhase(GamePhase.GameOver));
				Logger.LogInfo("Stalemate!");
				Send(new StalemateMessage());
				return;
			}

			// Update check status
			if (_validationSystem.IsInCheck(currentPlayer))
			{
				Logger.LogInfo($"{currentPlayer} is in check!");
				// TODO: Set InCheck component on king entity
			}
		}
	}

	// Called by other systems to initialize the game state
	public void InitializeGameState(Entity gameStateEntity)
	{
		Set(gameStateEntity, new ChessGameState());
		Set(gameStateEntity, new CurrentTurn(Player.White));
		Set(gameStateEntity, new CurrentGamePhase(GamePhase.SelectingPiece));
		Set(gameStateEntity, new AiConfig(false, Player.Black));
		Set(gameStateEntity, new DestroyedOnReset());
	}

	// Called by ChessMoveExecutionSystem after a move is executed
	public void EndTurn(Entity gameStateEntity)
	{
		if (!Has<CurrentTurn>(gameStateEntity))
			return;

		// Remove selected piece reference
		if (Has<SelectedPieceRef>(gameStateEntity))
			Remove<SelectedPieceRef>(gameStateEntity);

		// Switch player
		var currentPlayer = Get<CurrentTurn>(gameStateEntity).Player;
		var nextPlayer = currentPlayer == Player.White ? Player.Black : Player.White;
		Set(gameStateEntity, new CurrentTurn(nextPlayer));

		// Reset to selecting piece phase
		Set(gameStateEntity, new CurrentGamePhase(GamePhase.SelectingPiece));

		Logger.LogInfo($"Turn: {nextPlayer}");

		// Check if it's AI's turn
		if (Has<AiConfig>(gameStateEntity))
		{
			var aiConfig = Get<AiConfig>(gameStateEntity);
			if (aiConfig.IsEnabled && nextPlayer == aiConfig.AiPlayer)
			{
				Set(gameStateEntity, new CurrentGamePhase(GamePhase.AIThinking));
				Logger.LogInfo("AI is thinking...");
			}
		}
	}
}
