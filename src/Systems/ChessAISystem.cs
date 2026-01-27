using System;
using System.Threading.Tasks;
using MoonTools.ECS;
using MoonWorks;
using Tactician.AI;
using Tactician.Data;

namespace Tactician.Systems;

public class ChessAISystem : MoonTools.ECS.System
{
	private readonly ChessTurnSystem _turnSystem;
	private IChessAI _ai;
	private Task<ChessMove> _thinkingTask;
	private DateTime _thinkingStartTime;
	private const double MinThinkingTimeSeconds = 0.5; // Minimum time AI "thinks" for visual feedback

	public ChessAISystem(World world, ChessTurnSystem turnSystem) : base(world)
	{
		_turnSystem = turnSystem;
	}

	public void SetAI(IChessAI ai)
	{
		_ai = ai;
	}

	public override void Update(TimeSpan delta)
	{
		if (_turnSystem.CurrentPhase != GamePhase.AIThinking)
		{
			_thinkingTask = null;
			return;
		}

		// Start thinking if not already started
		if (_thinkingTask == null && _ai != null)
		{
			_thinkingStartTime = DateTime.Now;
			_thinkingTask = Task.Run(() => _ai.GetBestMove());
			Logger.LogInfo("AI started thinking...");
		}

		// Check if thinking is complete
		if (_thinkingTask != null && _thinkingTask.IsCompleted)
		{
			// Ensure minimum thinking time has passed (for visual feedback)
			var elapsedTime = (DateTime.Now - _thinkingStartTime).TotalSeconds;
			if (elapsedTime >= MinThinkingTimeSeconds)
			{
				var move = _thinkingTask.Result;

				if (move.IsValid())
				{
					Logger.LogInfo($"AI chose move: {move.PieceType} from ({move.From.File},{move.From.Rank}) to ({move.To.File},{move.To.Rank})");
					_turnSystem.HandleAIMove(move);
				}
				else
				{
					Logger.LogError("AI returned invalid move!");
				}

				_thinkingTask = null;
			}
		}
	}
}
