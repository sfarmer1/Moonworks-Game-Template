using System;
using MoonTools.ECS;
using MoonWorks;
using Tactician.Components;

namespace Tactician.Systems;

public class CursorSystem : MoonTools.ECS.System
{
	private readonly Filter _cursorFilter;
	private readonly Filter _selectedThingFilter;

	public CursorSystem(World world) : base(world)
	{
		_cursorFilter = FilterBuilder
			.Include<Cursor>()
			.Include<CanReceiveDirectionalInput>()
			.Include<Position>()
			.Build();


		_selectedThingFilter = FilterBuilder
			.Include<Selected>()
			.Include<Position>()
			.Build();
	}

	// Use keyboard input to move selector cursor over connected selectable objects
	// moves based on the direction of the keyboard input
	public override void Update(TimeSpan delta)
	{
		foreach (var cursor in _cursorFilter.Entities)
		{
			var cursorPosition = Get<Position>(cursor);
			foreach (var selectedThing in _selectedThingFilter.Entities)
			{
				var selectedThingPosition = Get<Position>(selectedThing);
				if (selectedThingPosition != cursorPosition)
				{
					continue;
				}

				//
				// We have the correct cursor and currently selected thing
				//

				var inputState = Get<CanReceiveDirectionalInput>(cursor);

				Direction direction;
				if (inputState.Left.IsPressed)
					direction = Direction.Left;
				else if (inputState.Right.IsPressed)
					direction = Direction.Right;
				else if (inputState.Up.IsPressed)
					direction = Direction.Up;
				else if (inputState.Down.IsPressed)
					direction = Direction.Down;
				else
					direction = Direction.None;

				var newPosition = direction switch {
					Direction.Left when HasOutRelation<GamepadNavLeft>(selectedThing) =>
						GetPositionFromRelation<GamepadNavLeft>(selectedThing),
					Direction.Right when HasOutRelation<GamepadNavRight>(selectedThing) =>
						GetPositionFromRelation<GamepadNavRight>(selectedThing),
					Direction.Up when HasOutRelation<GamepadNavUp>(selectedThing) =>
						GetPositionFromRelation<GamepadNavUp>(selectedThing),
					Direction.Down when HasOutRelation<GamepadNavDown>(selectedThing) =>
						GetPositionFromRelation<GamepadNavDown>(selectedThing),
					Direction.None =>
						cursorPosition,
					_ =>
						throw new ArgumentOutOfRangeException()
				};

				Logger.LogInfo($"Moving cursor to {newPosition}");

				Set<Position>(cursor, newPosition);
			}
		}
	}

	private Position GetPositionFromRelation<T>(in Entity entity) where T : unmanaged
	{
		var outEntity = OutRelationSingleton<T>(entity);

		var result = Get<Position>(outEntity);
		return result;
	}
}