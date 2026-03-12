using System.Collections.Generic;
using MoonTools.ECS;
using Tactician.Components;
using Tactician.Data;

namespace Tactician.Serialization;

/// <summary>
/// Manages turn-by-turn history for replay and rewind functionality.
/// Records game state after each turn and allows stepping backward/forward through time.
/// Recording is triggered by TurnCompletedMessage for efficiency (not checked every frame).
/// </summary>
public class TurnHistoryManager
{
	private readonly SaveLoadManager _saveLoadManager;
	private readonly List<ChessSaveData> _history;
	private int _currentHistoryIndex;
	private bool _isViewingHistory;

	public TurnHistoryManager(SaveLoadManager saveLoadManager)
	{
		_saveLoadManager = saveLoadManager;
		_history = new List<ChessSaveData>();
		_currentHistoryIndex = -1;
		_isViewingHistory = false;
	}

	/// <summary>
	/// Gets whether the player is currently viewing a past state (not at the latest turn).
	/// </summary>
	public bool IsViewingHistory => _isViewingHistory;

	/// <summary>
	/// Gets the current turn number being viewed.
	/// </summary>
	public int CurrentTurn => _currentHistoryIndex;

	/// <summary>
	/// Gets the total number of recorded turns.
	/// </summary>
	public int TotalTurns => _history.Count;

	/// <summary>
	/// Records the current game state as a new history entry.
	/// This should be called after each completed turn.
	/// Only records if the state has actually changed from the last recorded state.
	/// </summary>
	public void RecordCurrentState()
	{
		var saveData = _saveLoadManager.ExtractSaveData();

		// Check if state actually changed (don't create duplicate entries)
		if (_history.Count > 0 && !_isViewingHistory)
		{
			var lastState = _history[_history.Count - 1];
			if (StatesAreEqual(lastState, saveData))
			{
				// State hasn't changed, don't record
				return;
			}
		}

		// If we're viewing history and record a new state, we're creating a new branch
		// Discard all future history from this point
		if (_isViewingHistory && _currentHistoryIndex < _history.Count - 1)
		{
			_history.RemoveRange(_currentHistoryIndex + 1, _history.Count - _currentHistoryIndex - 1);
			MoonWorks.Logger.LogInfo($"Branched history: discarded {_history.Count - _currentHistoryIndex - 1} future states");
		}

		_history.Add(saveData);
		_currentHistoryIndex = _history.Count - 1;
		_isViewingHistory = false;

		MoonWorks.Logger.LogInfo($"Recorded turn {_currentHistoryIndex} (total: {_history.Count} states)");
	}

	/// <summary>
	/// Compares two save states for equality (same pieces, same positions, same turn).
	/// </summary>
	private bool StatesAreEqual(ChessSaveData state1, ChessSaveData state2)
	{
		// Quick checks first
		if (state1.Pieces.Count != state2.Pieces.Count) return false;
		if (state1.GameState.CurrentTurn != state2.GameState.CurrentTurn) return false;
		if (state1.GameState.Phase != state2.GameState.Phase) return false;

		// Check all pieces match
		for (int i = 0; i < state1.Pieces.Count; i++)
		{
			var p1 = state1.Pieces[i];
			var p2 = state2.Pieces[i];

			if (p1.Type != p2.Type || p1.Owner != p2.Owner ||
			    p1.File != p2.File || p1.Rank != p2.Rank ||
			    p1.HasNotMoved != p2.HasNotMoved ||
			    p1.IsEnPassantTarget != p2.IsEnPassantTarget)
			{
				return false;
			}
		}

		return true;
	}

	/// <summary>
	/// Step backward one turn in history.
	/// Returns the game state to restore, or null if already at the beginning.
	/// </summary>
	public ChessSaveData StepBackward()
	{
		if (_currentHistoryIndex <= 0)
		{
			MoonWorks.Logger.LogInfo("Already at the beginning of history");
			return null;
		}

		_currentHistoryIndex--;
		_isViewingHistory = (_currentHistoryIndex < _history.Count - 1);

		MoonWorks.Logger.LogInfo($"Stepped back to turn {_currentHistoryIndex}/{_history.Count - 1}");
		return _history[_currentHistoryIndex];
	}

	/// <summary>
	/// Step forward one turn in history.
	/// Returns the game state to restore, or null if already at the latest state.
	/// </summary>
	public ChessSaveData StepForward()
	{
		if (_currentHistoryIndex >= _history.Count - 1)
		{
			MoonWorks.Logger.LogInfo("Already at the latest recorded state");
			return null;
		}

		_currentHistoryIndex++;
		_isViewingHistory = (_currentHistoryIndex < _history.Count - 1);

		MoonWorks.Logger.LogInfo($"Stepped forward to turn {_currentHistoryIndex}/{_history.Count - 1}");
		return _history[_currentHistoryIndex];
	}

	/// <summary>
	/// Returns to the present (latest recorded state).
	/// </summary>
	public ChessSaveData ReturnToPresent()
	{
		if (!_isViewingHistory)
		{
			return null;
		}

		_currentHistoryIndex = _history.Count - 1;
		_isViewingHistory = false;

		MoonWorks.Logger.LogInfo($"Returned to present (turn {_currentHistoryIndex})");
		return _history[_currentHistoryIndex];
	}

	/// <summary>
	/// Clears all recorded history.
	/// </summary>
	public void ClearHistory()
	{
		_history.Clear();
		_currentHistoryIndex = -1;
		_isViewingHistory = false;
		MoonWorks.Logger.LogInfo("Cleared turn history");
	}

	/// <summary>
	/// Gets the number of turns the player can step backward.
	/// </summary>
	public int CanStepBackwardCount => _currentHistoryIndex;

	/// <summary>
	/// Gets the number of turns the player can step forward.
	/// </summary>
	public int CanStepForwardCount => _history.Count - 1 - _currentHistoryIndex;
}
