using System;
using System.Numerics;
using MoonTools.ECS;
using Tactician.Components;
using Tactician.Data;
using Tactician.Messages;

namespace Tactician.Systems;

public class SpriteAnimationSystem : MoonTools.ECS.System
{
    private readonly Filter _spriteAnimationFilter;

    public SpriteAnimationSystem(World world) : base(world)
    {
        _spriteAnimationFilter = FilterBuilder
            .Include<SpriteAnimation>()
            .Build();
    }

    public override void Update(TimeSpan delta)
    {
        // Update sprite animation frame
        foreach (var entity in _spriteAnimationFilter.Entities)
        {
            var dt = (float)delta.TotalSeconds;
            var spriteAnimation = Get<SpriteAnimation>(entity).Update(dt);
            Set(entity, spriteAnimation);
        }
    }
}