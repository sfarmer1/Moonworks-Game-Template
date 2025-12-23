using System;
using MoonTools.ECS;
using Tactician.Components;
using Tactician.Messages;

namespace Tactician.Systems;

public class SetSpriteAnimationSystem(World world) : MoonTools.ECS.System(world) {
    public override void Update(TimeSpan delta) {
        foreach (var message in ReadMessages<SetAnimationMessage>())
            if (Has<SpriteAnimation>(message.Entity)) {
                var currentAnimation = Get<SpriteAnimation>(message.Entity);

                if (currentAnimation.SpriteAnimationInfoID ==
                    message.Animation.SpriteAnimationInfoID) {
                    if (currentAnimation.FrameRate != message.Animation.FrameRate)
                        Set(message.Entity, currentAnimation.ChangeFramerate(message.Animation.FrameRate));
                    else if (message.ForceUpdate) Set(message.Entity, message.Animation);
                }
                else {
                    Set(message.Entity, message.Animation);
                }
            }
            else {
                Set(message.Entity, message.Animation);
            }
    }
}