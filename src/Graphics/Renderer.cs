using System.Numerics;
using MoonTools.ECS;
using MoonWorks;
using MoonWorks.Graphics;
using MoonWorks.Graphics.Font;
using MoonWorks.Storage;
using Tactician.Content;
using Tactician.Components;
using Tactician.Data;
using Filter = MoonTools.ECS.Filter;

namespace Tactician.Graphics;

public class Renderer : MoonTools.ECS.Renderer {
    private readonly SpriteBatch _artSpriteBatch;
    private readonly Texture _depthTexture;
    private readonly GraphicsDevice _graphicsDevice;

    private readonly Sampler _pointSampler;

    private readonly Filter _rectangleFilter;

    private readonly Texture _renderTexture;
    private readonly Filter _spriteAnimationFilter;

    private readonly Texture _spriteAtlasTexture;
    private readonly TextBatch _textBatch;
    private readonly Filter _textFilter;
    private readonly GraphicsPipeline _textPipeline;

    public Renderer(World world, GraphicsDevice graphicsDevice, TitleStorage titleStorage, TextureFormat swapchainFormat) : base(world) {
        _graphicsDevice = graphicsDevice;

        _rectangleFilter = FilterBuilder.Include<Rectangle>().Include<Position>().Include<DrawAsRectangle>().Build();
        _textFilter = FilterBuilder.Include<Text>().Include<Position>().Build();
        _spriteAnimationFilter = FilterBuilder.Include<SpriteAnimation>().Include<Position>().Build();

        _renderTexture = Texture.Create2D(_graphicsDevice, "Render Texture", Dimensions.GAME_W, Dimensions.GAME_H,
            swapchainFormat, TextureUsageFlags.ColorTarget | TextureUsageFlags.Sampler);
        _depthTexture = Texture.Create2D(_graphicsDevice, "Depth Texture", Dimensions.GAME_W, Dimensions.GAME_H,
            TextureFormat.D16Unorm, TextureUsageFlags.DepthStencilTarget);

        _spriteAtlasTexture = TextureAtlases.TP_Sprites.Texture;

        _textPipeline = GraphicsPipeline.Create(
            _graphicsDevice,
            new GraphicsPipelineCreateInfo {
                TargetInfo = new GraphicsPipelineTargetInfo {
                    DepthStencilFormat = TextureFormat.D16Unorm,
                    HasDepthStencilTarget = true,
                    ColorTargetDescriptions = [
                        new ColorTargetDescription {
                            Format = swapchainFormat,
                            BlendState = ColorTargetBlendState.PremultipliedAlphaBlend
                        }
                    ]
                },
                DepthStencilState = new DepthStencilState {
                    EnableDepthTest = true,
                    EnableDepthWrite = true,
                    CompareOp = CompareOp.LessOrEqual
                },
                VertexShader = _graphicsDevice.TextVertexShader,
                FragmentShader = _graphicsDevice.TextFragmentShader,
                VertexInputState = _graphicsDevice.TextVertexInputState,
                RasterizerState = RasterizerState.CCW_CullNone,
                PrimitiveType = PrimitiveType.TriangleList,
                MultisampleState = MultisampleState.None,
                Name = "Text Pipeline"
            }
        );
        _textBatch = new TextBatch(_graphicsDevice);

        _pointSampler = Sampler.Create(_graphicsDevice, SamplerCreateInfo.PointClamp);

        _artSpriteBatch = new SpriteBatch(_graphicsDevice, titleStorage, swapchainFormat, TextureFormat.D16Unorm);
    }

    public void Render(Window window) {
        var commandBuffer = _graphicsDevice.AcquireCommandBuffer();

        var swapchainTexture = commandBuffer.AcquireSwapchainTexture(window);

        if (swapchainTexture != null) {
            _artSpriteBatch.Start();

            foreach (var entity in _rectangleFilter.Entities) {
                var position = Get<Position>(entity);
                var rectangle = Get<Rectangle>(entity);
                var orientation = Has<Orientation>(entity) ? Get<Orientation>(entity).Angle : 0.0f;
                var color = Has<ColorBlend>(entity) ? Get<ColorBlend>(entity).Color : Color.White;
                var depth = -2f;
                if (Has<Depth>(entity)) depth = -Get<Depth>(entity).Value;

                var sprite = SpriteAnimations.Pixel.Frames[0];
                _artSpriteBatch.Add(new Vector3(position.X + rectangle.X, position.Y + rectangle.Y, depth), orientation,
                    new Vector2(rectangle.Width, rectangle.Height), color, sprite.UV.LeftTop, sprite.UV.Dimensions);
            }

            foreach (var entity in _spriteAnimationFilter.Entities) {
                if (HasOutRelation<DontDraw>(entity))
                    continue;

                var position = Get<Position>(entity);
                var animation = Get<SpriteAnimation>(entity);
                var sprite = animation.CurrentSprite;
                var origin = animation.Origin;
                var depth = -1f;
                var color = Color.White;

                var scale = Vector2.One;
                if (Has<SpriteScale>(entity)) {
                    scale *= Get<SpriteScale>(entity).Scale;
                    origin *= scale;
                }

                var offset = -origin - new Vector2(sprite.FrameRect.X, sprite.FrameRect.Y) * scale;

                if (Has<ColorBlend>(entity)) color = Get<ColorBlend>(entity).Color;

                if (Has<ColorFlicker>(entity)) {
                    var colorFlicker = Get<ColorFlicker>(entity);
                    if (colorFlicker.ElapsedFrames % 2 == 0) color = colorFlicker.Color;
                }

                if (Has<Depth>(entity)) depth = -Get<Depth>(entity).Value;

                _artSpriteBatch.Add(new Vector3(position.X + offset.X, position.Y + offset.Y, depth), 0,
                    new Vector2(sprite.SliceRect.W, sprite.SliceRect.H) * scale, color, sprite.UV.LeftTop,
                    sprite.UV.Dimensions);
            }

            _textBatch.Start();
            foreach (var entity in _textFilter.Entities) {
                if (HasOutRelation<DontDraw>(entity))
                    continue;

                var text = Get<Text>(entity);
                var position = Get<Position>(entity);

                var str = TextStorage.GetString(text.TextID);
                var font = Fonts.FromID(text.FontID);
                var color = Has<Color>(entity) ? Get<Color>(entity) : Color.White;
                var depth = -1f;

                if (Has<ColorBlend>(entity)) color = Get<ColorBlend>(entity).Color;

                if (Has<Depth>(entity)) depth = -Get<Depth>(entity).Value;

                if (Has<TextDropShadow>(entity)) {
                    var dropShadow = Get<TextDropShadow>(entity);

                    var dropShadowPosition = position + new Position(dropShadow.OffsetX, dropShadow.OffsetY);

                    _textBatch.Add(
                        font,
                        str,
                        text.Size,
                        Matrix4x4.CreateTranslation(dropShadowPosition.X, dropShadowPosition.Y, depth - 1),
                        new Color((float)0, 0, 0, color.A),
                        text.HorizontalAlignment,
                        text.VerticalAlignment
                    );
                }

                _textBatch.Add(
                    font,
                    str,
                    text.Size,
                    Matrix4x4.CreateTranslation(position.X, position.Y, depth),
                    color,
                    text.HorizontalAlignment,
                    text.VerticalAlignment
                );
            }

            _artSpriteBatch.Upload(commandBuffer);
            _textBatch.UploadBufferData(commandBuffer);

            var renderPass = commandBuffer.BeginRenderPass(
                new DepthStencilTargetInfo(_depthTexture, 1, 0),
                new ColorTargetInfo(_renderTexture, Color.Black)
            );

            var viewProjectionMatrices = new ViewProjectionMatrices(GetCameraMatrix(), GetProjectionMatrix());

            if (_artSpriteBatch.InstanceCount > 0)
                _artSpriteBatch.Render(renderPass, _spriteAtlasTexture, _pointSampler, viewProjectionMatrices);

            renderPass.BindGraphicsPipeline(_textPipeline);
            _textBatch.Render(renderPass, GetCameraMatrix() * GetProjectionMatrix());

            commandBuffer.EndRenderPass(renderPass);

            commandBuffer.Blit(_renderTexture, swapchainTexture, MoonWorks.Graphics.Filter.Nearest);
        }

        _graphicsDevice.Submit(commandBuffer);
    }

    public Matrix4x4 GetCameraMatrix() {
        return Matrix4x4.Identity;
    }

    public Matrix4x4 GetProjectionMatrix() {
        return Matrix4x4.CreateOrthographicOffCenter(
            0,
            Dimensions.GAME_W,
            Dimensions.GAME_H,
            0,
            0.01f,
            1000
        );
    }
}