using System;
using MoonTools.ECS;
using MoonWorks.Graphics;
using Tactician.Components;
using Filter = MoonTools.ECS.Filter;

namespace Tactician.Systems;

public class ChessHighlightSystem : MoonTools.ECS.System
{
	private readonly Filter _validMoveHighlightFilter;
	private readonly Filter _activePieceFilter;
	private readonly Filter _inCheckFilter;

	public ChessHighlightSystem(World world) : base(world)
	{
		_validMoveHighlightFilter = FilterBuilder.Include<ValidMoveHighlight>().Include<ColorBlend>().Build();
		_activePieceFilter = FilterBuilder.Include<ActivePiece>().Build();
		_inCheckFilter = FilterBuilder.Include<InCheck>().Build();
	}

	public override void Update(TimeSpan delta)
	{
		// Highlight valid move squares with green tint (already set by ChessInputSystem via ColorBlend on squares)
		// The squares already have a base color, ValidMoveHighlight is just a marker

		// Highlight active piece with brighter color
		foreach (var piece in _activePieceFilter.Entities)
		{
			if (!Has<ColorBlend>(piece))
				Set(piece, new ColorBlend(new Color(1f, 1f, 0.7f, 1f)));
		}

		// Highlight kings in check with red tint
		foreach (var king in _inCheckFilter.Entities)
		{
			Set(king, new ColorBlend(new Color(1f, 0.3f, 0.3f, 1f)));
		}
	}
}
