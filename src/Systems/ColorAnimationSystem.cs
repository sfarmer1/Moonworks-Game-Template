using System;
using MoonTools.ECS;
using MoonWorks.Graphics;
using Tactician.Components;
using Filter = MoonTools.ECS.Filter;

namespace Tactician.Systems;

public class ColorAnimationSystem : MoonTools.ECS.System {
    private readonly Filter _colorAnimationFilter;

    public ColorAnimationSystem(World world) : base(world) {
        _colorAnimationFilter = FilterBuilder.Include<ColorBlend>().Include<ColorSpeed>().Build();
    }

    public override void Update(TimeSpan delta) {
        var dt = (float)delta.TotalSeconds;

        foreach (var colorAnimationEntity in _colorAnimationFilter.Entities) {
            var color = Get<ColorBlend>(colorAnimationEntity).Color;
            var colorSpeed = Get<ColorSpeed>(colorAnimationEntity);

            var newColor = new Color(
                (color.R / 255f + colorSpeed.RedSpeed * dt) % 1f,
                (color.G / 255f + colorSpeed.GreenSpeed * dt) % 1f,
                (color.B / 255f + colorSpeed.BlueSpeed * dt) % 1f
            );

            Set(colorAnimationEntity, new ColorBlend(newColor));
        }
    }
}