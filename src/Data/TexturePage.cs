using System;
using System.Collections.Generic;
using MoonWorks.Graphics;
using Tactician.Components;

namespace Tactician.Data;

public readonly record struct TexturePageID(int ID);

public class TexturePage {
    private static readonly List<TexturePage> IDLookup = new();
    private readonly Dictionary<string, SpriteAnimationInfo> _animationInfos = new();
    public readonly TexturePageID ID;

    private readonly Dictionary<string, Sprite> _sprites = new();

    public TexturePage(string jsonFilename) {
        lock (IDLookup) {
            ID = new TexturePageID(IDLookup.Count);
            IDLookup.Add(this);
        }

        JsonFilename = jsonFilename;
    }

    public string JsonFilename { get; private set; }
    public CramTextureAtlasData AtlasData { get; private set; }
    public Texture Texture { get; private set; }
    public uint Width => (uint)AtlasData.Width;
    public uint Height => (uint)AtlasData.Height;

    public static TexturePage FromID(TexturePageID id) {
        return IDLookup[id.ID];
    }

    public void Load(GraphicsDevice graphicsDevice, CramTextureAtlasData data) {
        AtlasData = data;
        foreach (var image in AtlasData.Images) AddSprite(image);

        foreach (var (name, spriteAnimation) in AtlasData.Animations) {
            var frames = new List<Sprite>();

            foreach (var frame in spriteAnimation.Frames) frames.Add(GetSprite(frame));

            var spriteAnimationInfo = new SpriteAnimationInfo(
                name,
                frames.ToArray(),
                spriteAnimation.FrameRate,
                spriteAnimation.XOrigin,
                spriteAnimation.YOrigin
            );

            _animationInfos.Add(name, spriteAnimationInfo);
        }

        Texture = Texture.Create2D(
            graphicsDevice,
            data.Name,
            (uint)AtlasData.Width,
            (uint)AtlasData.Height,
            TextureFormat.R8G8B8A8Unorm,
            TextureUsageFlags.Sampler
        );
    }

    public void LoadImage(GraphicsDevice graphicsDevice, ReadOnlySpan<byte> data) {
        var resourceUploader = new ResourceUploader(graphicsDevice);
        resourceUploader.SetTextureDataFromCompressed(new TextureRegion(Texture), data);
        resourceUploader.Upload();
        resourceUploader.Dispose();
    }

    private void Unload() {
        Texture.Dispose();
        Texture = null;
    }

    private void AddSprite(CramTextureAtlasImageData imageData) {
        var sliceRect = new Rect {
            X = imageData.X,
            Y = imageData.Y,
            W = imageData.W,
            H = imageData.H
        };
        var frameRect = new Rect {
            X = imageData.TrimOffsetX,
            Y = imageData.TrimOffsetY,
            W = imageData.UntrimmedWidth,
            H = imageData.UntrimmedHeight
        };
        var sprite = new Sprite(this, sliceRect, frameRect);

        _sprites.Add(imageData.Name, sprite);
    }

    public SpriteAnimationInfo GetSpriteAnimationInfo(string name) {
        return _animationInfos[name];
    }

    public Sprite GetSprite(string name) {
        return _sprites[name];
    }
}