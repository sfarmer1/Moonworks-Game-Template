using System;
using System.Numerics;
using MoonTools.ECS;
using Tactician.Components;
using Tactician.Data;
using Tactician.Messages;

namespace Tactician.Systems;

public class DirectionalAnimationSystem : MoonTools.ECS.System {
    private readonly Filter _directionFilter;

    public DirectionalAnimationSystem(World world) : base(world) {
        _directionFilter = FilterBuilder
            .Include<LastDirection>()
            .Include<DirectionalSprites>()
            .Build();
    }

    public override void Update(TimeSpan delta) {
        foreach (var entity in _directionFilter.Entities) {
            var direction = Get<LastDirection>(entity).Direction;
            var animations = Get<DirectionalSprites>(entity);

            SpriteAnimationInfo animation;

            if (direction.X > 0) {
                if (direction.Y > 0)
                    animation = SpriteAnimationInfo.FromID(animations.DownRight);
                else if (direction.Y < 0)
                    animation = SpriteAnimationInfo.FromID(animations.UpRight);
                else
                    animation = SpriteAnimationInfo.FromID(animations.Right);
            }
            else if (direction.X < 0) {
                if (direction.Y > 0)
                    animation = SpriteAnimationInfo.FromID(animations.DownLeft);
                else if (direction.Y < 0)
                    animation = SpriteAnimationInfo.FromID(animations.UpLeft);
                else
                    animation = SpriteAnimationInfo.FromID(animations.Left);
            }
            else {
                if (direction.Y > 0)
                    animation = SpriteAnimationInfo.FromID(animations.Down);
                else if (direction.Y < 0)
                    animation = SpriteAnimationInfo.FromID(animations.Up);
                else
                    animation = Get<SpriteAnimation>(entity).SpriteAnimationInfo;
            }

            var velocity = Has<Velocity>(entity) ? Get<Velocity>(entity) : Vector2.Zero;

            var framerate = Get<SpriteAnimation>(entity).FrameRate;

            if (Has<AdjustFramerateToSpeed>(entity)) {
                framerate = (int)(velocity.Length() / 20f);
                if (Has<FunnyRunTimer>(entity)) framerate = 25;
            }

            if (direction.LengthSquared() > 0) {
                Send(new SetAnimationMessage(
                    entity,
                    new SpriteAnimation(animation, framerate, true)
                ));
            }
            else {
                framerate = 0;
                Send(new SetAnimationMessage(
                    entity,
                    new SpriteAnimation(animation, framerate, true, 0),
                    true
                ));
            }
        }
    }
}