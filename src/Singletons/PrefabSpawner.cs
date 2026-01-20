using System;
using MoonTools.ECS;
using Tactician.Components;
using Tactician.Systems;

namespace Tactician.Singletons;

public class PrefabSpawner(World world) : Manipulator(world)
{
	public const int TILE_SIZE = 64;
	public const int GRID_SIZE = 4;
	
	public Entity SpawnLevel_1()
	{
		var level = World.CreateEntity();
		var levelData = new Level4X4(
			TileType.Wall, 		TileType._,			TileType._,				TileType.Wall,
			TileType._,			TileType.PawnP2,	TileType._,				TileType._,
			TileType._, 		TileType._,			TileType.PawnP1,		TileType._,
			TileType.Wall, 		TileType._,			TileType._,				TileType.Wall
		);
		World.Set(level, levelData);

		// Define grid layout
		var centerX = GameDimensions.WIDTH * 0.5f;
		var centerY = GameDimensions.HEIGHT * 0.5f;
		var gridStartX = centerX - (GRID_SIZE * TILE_SIZE * 0.5f) + (TILE_SIZE * 0.5f);
		var gridStartY = centerY - (GRID_SIZE * TILE_SIZE * 0.5f) + (TILE_SIZE * 0.5f);

		// Spawn 16 tiles in a 4x4 grid
		var tileTypes = new[] {
			levelData.A1, levelData.A2, levelData.A3, levelData.A4,
			levelData.B1, levelData.B2, levelData.B3, levelData.B4,
			levelData.C1, levelData.C2, levelData.C3, levelData.C4,
			levelData.D1, levelData.D2, levelData.D3, levelData.D4
		};

		var tileEntityArray = new Entity[tileTypes.Length];
		
		
		for (var i = 0; i < tileTypes.Length; i++)
		{
			var row = i / GRID_SIZE;
			var col = i % GRID_SIZE;
			var tileType = tileTypes[i];

			var tile = World.CreateEntity();
			var posX = gridStartX + col * TILE_SIZE;
			var posY = gridStartY + row * TILE_SIZE;
			World.Set(tile, new Position(posX, posY));

			// Set sprite animation based on tile type
			switch (tileType)
			{
				case TileType.PawnP1:
					World.Set(tile, new SpriteAnimation(Content.SpriteAnimations.Char_Walk_Down));
					break;
				case TileType.PawnP2:
					World.Set(tile, new SpriteAnimation(Content.SpriteAnimations.Char2_Walk_Down));
					break;
				case TileType.Wall:
					World.Set(tile, new SpriteAnimation(Content.SpriteAnimations.NPC_Drone_Fly_Down));
					World.Set(tile, new Solid());
					break;
				case TileType._:
					World.Set(tile, new SpriteAnimation(Content.SpriteAnimations.Effect_SpinningSkull));
					break;
				default:
					throw new ArgumentOutOfRangeException();
			}

			if (i == 0)
			{
				World.Set(tile, new Selected());
				
				
				var cursor = World.CreateEntity();
				World.Set(cursor, new Cursor());
				World.Set(cursor, new Position(posX, posY));
				World.Set(cursor, new CanReceiveDirectionalInput());
				World.Set(cursor, new HasPlayerOwner(0));
				World.Set(cursor, new SpriteAnimation(Content.SpriteAnimations.Effect_SpinningCoin));
				World.Set(cursor, new Depth(-1));
			}
			
			tileEntityArray[i] = tile;
		}

		for (var i = 0; i < tileEntityArray.Length; i++)
		{
			var entity = tileEntityArray[i];
			var row    = i / GRID_SIZE;
			var column    = i % GRID_SIZE;

			if (column > 0)
			{
				var leftEntity = tileEntityArray[i - 1];
				World.Relate(entity, leftEntity, new GamepadNavLeft());
			}

			if (column < GRID_SIZE - 1)
			{
				var rightEntity = tileEntityArray[i + 1];
				World.Relate(entity, rightEntity, new GamepadNavRight());
			}
			
			if (row > 0)
			{
				var aboveEntity = tileEntityArray[i - GRID_SIZE];
				World.Relate(entity, aboveEntity, new GamepadNavUp());
			}

			if (row < GRID_SIZE - 1)
			{
				var belowEntity = tileEntityArray[i + GRID_SIZE];
				World.Relate(entity, belowEntity, new GamepadNavDown());
			}
		}

		return level;
	}
}