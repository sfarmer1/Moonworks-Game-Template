using System;
using MoonTools.ECS;
using MoonWorks;
using Tactician.AI;
using Tactician.Data;
using Tactician.Components;

namespace Tactician.Systems;

public class ChessAISystem : MoonTools.ECS.System
{
	private IChessAI _ai;
	private readonly Filter _gameStateFilter;
	private float _thinkingTimer;
	private const float MinThinkingTimeSeconds = 0.5f; // Minimum time AI "thinks" for visual feedback
	private bool _isThinking;

	public ChessAISystem(World world) : base(world)
	{
		_gameStateFilter = FilterBuilder
			.Include<ChessGameState>()
			.Include<CurrentGamePhase>()
			.Build();
	}

	public void SetAI(IChessAI ai)
	{
		_ai = ai;
	}

	public override void Update(TimeSpan delta)
	{
		if (_ai == null)
			return;

		foreach (var gameState in _gameStateFilter.Entities)
		{
			var phase = Get<CurrentGamePhase>(gameState).Phase;

			if (phase == GamePhase.AIThinking)
			{
				if (!_isThinking)
				{
					// Start thinking
					_isThinking = true;
					_thinkingTimer = 0f;
					Logger.LogInfo("AI started thinking...");
				}

				// Accumulate thinking time
				_thinkingTimer += (float)delta.TotalSeconds;

				// Execute move after minimum thinking time
				if (_thinkingTimer >= MinThinkingTimeSeconds)
				{
					var move = _ai.GetBestMove();

					if (move.IsValid())
					{
						Logger.LogInfo($"AI chose move: {move.PieceType} from ({move.From.File},{move.From.Rank}) to ({move.To.File},{move.To.Rank})");
						Set(gameState, new PendingMove(move));
					}
					else
					{
						Logger.LogError("AI returned invalid move!");
					}

					_isThinking = false;
				}
			}
			else
			{
				_isThinking = false;
			}

			break; // Only one game state entity
		}
	}
}
