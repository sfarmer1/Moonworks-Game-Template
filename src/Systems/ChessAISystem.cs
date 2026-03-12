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

	/// <summary>
	/// Resets the AI's internal thinking state.
	/// Useful when restoring game state or after unexpected state changes.
	/// </summary>
	public void ResetThinkingState()
	{
		_isThinking = false;
		_thinkingTimer = 0f;
		Logger.LogInfo("AI thinking state reset");
	}

	public override void Update(TimeSpan delta)
	{
		if (_ai == null)
		{
			Logger.LogWarn("AI is null, skipping update");
			return;
		}

		var foundGameState = false;
		foreach (var gameState in _gameStateFilter.Entities)
		{
			foundGameState = true;
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

				Logger.LogInfo($"AI thinking... timer={_thinkingTimer:F2}s, required={MinThinkingTimeSeconds}s");

				// Execute move after minimum thinking time
				if (_thinkingTimer >= MinThinkingTimeSeconds)
				{
					Logger.LogInfo("AI timer complete, getting best move...");
					var move = _ai.GetBestMove();
					Logger.LogInfo($"AI GetBestMove returned: valid={move.IsValid()}");

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
					Logger.LogInfo("AI finished thinking");
				}
			}
			else
			{
				// Reset thinking state when not in AI thinking phase
				if (_isThinking)
				{
					Logger.LogInfo("AI thinking reset (phase changed)");
				}
				_isThinking = false;
			}

			break; // Only one game state entity
		}

		if (!foundGameState)
		{
			Logger.LogError("AI system: No game state entity found in filter!");
		}
	}
}
