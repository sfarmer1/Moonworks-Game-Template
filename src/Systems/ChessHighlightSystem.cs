using System;
using MoonTools.ECS;
using MoonWorks.Graphics;
using Tactician.Components;
using Tactician.Content;
using Filter = MoonTools.ECS.Filter;

namespace Tactician.Systems;

public class ChessHighlightSystem : MoonTools.ECS.System
{
	private readonly ChessTurnSystem _turnSystem;
	private readonly Filter _validMoveHighlightFilter;
	private readonly Filter _activePieceFilter;
	private readonly Filter _inCheckFilter;

	public ChessHighlightSystem(World world, ChessTurnSystem turnSystem) : base(world)
	{
		_turnSystem = turnSystem;
		_validMoveHighlightFilter = FilterBuilder.Include<ValidMoveHighlight>().Build();
		_activePieceFilter = FilterBuilder.Include<ActivePiece>().Build();
		_inCheckFilter = FilterBuilder.Include<InCheck>().Build();
	}

	public override void Update(TimeSpan delta)
	{
		// Highlight valid move squares (green tint)
		foreach (var square in _validMoveHighlightFilter.Entities)
		{
			// Add a highlight overlay
			if (!Has<ColorBlend>(square))
				continue;

			var baseColor = Get<ColorBlend>(square).Color;
			var highlightColor = new Color(
				baseColor.R * 0.7f + 0.3f,
				baseColor.G * 0.7f + 0.3f,
				baseColor.B * 0.5f,
				1f
			);
			Set(square, new ColorBlend(highlightColor));
		}

		// Highlight active piece (yellow tint)
		foreach (var piece in _activePieceFilter.Entities)
		{
			if (!Has<ColorBlend>(piece))
				Set(piece, new ColorBlend(new Color(1f, 1f, 0.5f, 1f)));
			else
			{
				var currentColor = Get<ColorBlend>(piece).Color;
				Set(piece, new ColorBlend(new Color(
					currentColor.R * 0.8f + 0.2f,
					currentColor.G * 0.8f + 0.2f,
					currentColor.B * 0.6f,
					1f
				)));
			}
		}

		// Highlight kings in check (red tint)
		foreach (var king in _inCheckFilter.Entities)
		{
			if (!Has<ColorBlend>(king))
				Set(king, new ColorBlend(new Color(1f, 0.3f, 0.3f, 1f)));
		}
	}
}
