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

				var newPosition  = new Position();
				var newSelection = new Entity();
				switch (direction)
				{
					case Direction.Left when HasOutRelation<GamepadNavLeft>(selectedThing):
						newPosition = GetPositionFromRelation<GamepadNavLeft>(selectedThing);
						newPosition = newPosition.SetX(newPosition.X + (int)MathF.Round(ChessConstants.TILE_SIZE * 0.5f));
						newPosition = newPosition.SetY(newPosition.Y   + (int)MathF.Round(ChessConstants.TILE_SIZE * 0.5f));
						World.Remove<Selected>(selectedThing);
						newSelection = OutRelationSingleton<GamepadNavLeft>(selectedThing);
						World.Set(newSelection, new Selected());
						Set(cursor, newPosition);
						Logger.LogInfo($"Moving cursor left to {newPosition}");
						break;
					case Direction.Right when HasOutRelation<GamepadNavRight>(selectedThing):
						newPosition = GetPositionFromRelation<GamepadNavRight>(selectedThing);
						newPosition = newPosition.SetX(newPosition.X + (int)MathF.Round(ChessConstants.TILE_SIZE * 0.5f));
						newPosition = newPosition.SetY(newPosition.Y   + (int)MathF.Round(ChessConstants.TILE_SIZE * 0.5f));
						World.Remove<Selected>(selectedThing);
						newSelection = OutRelationSingleton<GamepadNavRight>(selectedThing);
						World.Set(newSelection, new Selected());
						Set(cursor, newPosition);
						Logger.LogInfo($"Moving cursor right to {newPosition}");
						break;
					case Direction.Up when HasOutRelation<GamepadNavUp>(selectedThing):
						newPosition = GetPositionFromRelation<GamepadNavUp>(selectedThing);
						newPosition = newPosition.SetX(newPosition.X + (int)MathF.Round(ChessConstants.TILE_SIZE * 0.5f));
						newPosition = newPosition.SetY(newPosition.Y   + (int)MathF.Round(ChessConstants.TILE_SIZE * 0.5f));
						World.Remove<Selected>(selectedThing);
						newSelection = OutRelationSingleton<GamepadNavUp>(selectedThing);
						World.Set(newSelection, new Selected());
						Set(cursor, newPosition);
						Logger.LogInfo($"Moving cursor up to {newPosition}");
						break;
					case Direction.Down when HasOutRelation<GamepadNavDown>(selectedThing):
						newPosition = GetPositionFromRelation<GamepadNavDown>(selectedThing);
						newPosition = newPosition.SetX(newPosition.X + (int)MathF.Round(ChessConstants.TILE_SIZE * 0.5f));
						newPosition = newPosition.SetY(newPosition.Y   + (int)MathF.Round(ChessConstants.TILE_SIZE * 0.5f));
						World.Remove<Selected>(selectedThing);
						newSelection = OutRelationSingleton<GamepadNavDown>(selectedThing);
						World.Set(newSelection, new Selected());
						Set(cursor, newPosition);
						break;
					case Direction.None:
						newPosition = cursorPosition;
						break;
					default:
						break;
				}

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