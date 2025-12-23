using System;
using MoonTools.ECS;
using Tactician.Components;

namespace Tactician.Systems;

public class UpdateSpriteAnimationSystem : MoonTools.ECS.System {
    private readonly Filter _spriteAnimationFilter;

    public UpdateSpriteAnimationSystem(World world) : base(world) {
        _spriteAnimationFilter = FilterBuilder
            .Include<SpriteAnimation>()
            .Include<Position>()
            .Build();
    }

    public override void Update(TimeSpan delta) {
        foreach (var entity in _spriteAnimationFilter.Entities) {
            UpdateSpriteAnimation(entity, (float)delta.TotalSeconds);
        }
    }

    public void UpdateSpriteAnimation(Entity entity, float dt) {
        var spriteAnimation = Get<SpriteAnimation>(entity).Update(dt);
        Set(entity, spriteAnimation);

        if (spriteAnimation.Finished) {
            /*
            if (Has<DestroyOnAnimationFinish>(entity))
            {
                Destroy(entity);
            }
            */
        }
    }
}