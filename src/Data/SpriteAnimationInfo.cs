using System.Collections.Generic;
using Tactician.Components;

namespace Tactician.Data;

public readonly record struct SpriteAnimationInfoID(int ID);

public class SpriteAnimationInfo {
    // FIXME: maaaaybe this shouldn't be static
    private static readonly List<SpriteAnimationInfo> IDLookup = new();
    public readonly SpriteAnimationInfoID ID;

    public SpriteAnimationInfo(
        string name,
        Sprite[] frames,
        int frameRate,
        int originX,
        int originY
    ) {
        Name = name;
        Frames = frames;
        FrameRate = frameRate;
        OriginX = originX;
        OriginY = originY;

        lock (IDLookup) {
            ID = new SpriteAnimationInfoID(IDLookup.Count);
            IDLookup.Add(this);
        }
    }

    public string Name { get; }
    public Sprite[] Frames { get; }
    public int FrameRate { get; }
    public int OriginX { get; }
    public int OriginY { get; }

    public static SpriteAnimationInfo FromID(SpriteAnimationInfoID id) {
        return IDLookup[id.ID];
    }
}