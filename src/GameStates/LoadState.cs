using System;
using System.Diagnostics;
using System.Numerics;
using MoonWorks;
using MoonWorks.AsyncIO;
using MoonWorks.Graphics;
using MoonWorks.Graphics.Font;
using Tactician.Content;
using Tactician.Components;

namespace Tactician.GameStates;

public class LoadState : GameState {
    private readonly TacticianGame _game;
    private readonly GraphicsDevice _graphicsDevice;
    private readonly Stopwatch _loadTimer = new();
    private readonly TextBatch _textBatch;

    private readonly GraphicsPipeline _textPipeline;

    private readonly Stopwatch _timer = new();
    private readonly GameState _transitionState;
    private AsyncFileLoader _asyncFileLoader;

    public LoadState(TacticianGame game, GameState transitionState) {
        _game = game;
        _graphicsDevice = _game.GraphicsDevice;
        _asyncFileLoader = new AsyncFileLoader(_graphicsDevice);
        _transitionState = transitionState;

        _textPipeline = GraphicsPipeline.Create(
            _graphicsDevice,
            new GraphicsPipelineCreateInfo {
                TargetInfo = new GraphicsPipelineTargetInfo {
                    ColorTargetDescriptions = [
                        new ColorTargetDescription {
                            Format = game.MainWindow.SwapchainFormat,
                            BlendState = ColorTargetBlendState.PremultipliedAlphaBlend
                        }
                    ]
                },
                DepthStencilState = DepthStencilState.Disable,
                VertexShader = _graphicsDevice.TextVertexShader,
                FragmentShader = _graphicsDevice.TextFragmentShader,
                VertexInputState = _graphicsDevice.TextVertexInputState,
                RasterizerState = RasterizerState.CCW_CullNone,
                PrimitiveType = PrimitiveType.TriangleList,
                MultisampleState = MultisampleState.None
            }
        );
        _textBatch = new TextBatch(_graphicsDevice);
    }

    public override void Start() {
        _loadTimer.Start();
        TextureAtlases.EnqueueLoadAllImages(_asyncFileLoader);
        StaticAudioPacks.LoadAsync(_asyncFileLoader);
        StreamingAudio.LoadAsync(_asyncFileLoader);
        _asyncFileLoader.Submit();
        _timer.Start();
    }

    public override void Update(TimeSpan delta) {
        if (_asyncFileLoader.Status == AsyncFileLoaderStatus.Failed)
            // Uh oh, time to bail!
            throw new ApplicationException("Game assets could not be loaded!");

        if (_asyncFileLoader.Status == AsyncFileLoaderStatus.Complete) {
            if (_loadTimer.IsRunning) {
                _loadTimer.Stop();
                Logger.LogInfo($"Load finished in {_loadTimer.Elapsed.TotalMilliseconds}ms");
            }
            _timer.Stop();
            _game.SetState(_transitionState);
        }
    }

    public override void Draw(Window window, double alpha) {
        var commandBuffer = _graphicsDevice.AcquireCommandBuffer();

        var swapchainTexture = commandBuffer.AcquireSwapchainTexture(_game.MainWindow);
        if (swapchainTexture != null) {
            _textBatch.Start();
            AddStringToTextBatch("L", 60, new Position(1640, 1020), 1.2f + 4 * (float)_timer.Elapsed.TotalSeconds);
            AddStringToTextBatch("O", 60, new Position(1680, 1020), 1.0f + 4 * (float)_timer.Elapsed.TotalSeconds);
            AddStringToTextBatch("A", 60, new Position(1720, 1020), 0.8f + 4 * (float)_timer.Elapsed.TotalSeconds);
            AddStringToTextBatch("D", 60, new Position(1760, 1020), 0.6f + 4 * (float)_timer.Elapsed.TotalSeconds);
            AddStringToTextBatch("I", 60, new Position(1782, 1020), 0.4f + 4 * (float)_timer.Elapsed.TotalSeconds);
            AddStringToTextBatch("N", 60, new Position(1820, 1020), 0.2f + 4 * (float)_timer.Elapsed.TotalSeconds);
            AddStringToTextBatch("G", 60, new Position(1860, 1020), 0.0f + 4 * (float)_timer.Elapsed.TotalSeconds);
            _textBatch.UploadBufferData(commandBuffer);

            var renderPass = commandBuffer.BeginRenderPass(
                new ColorTargetInfo(swapchainTexture, Color.Black)
            );

            renderPass.BindGraphicsPipeline(_textPipeline);
            var hiResProjectionMatrix = Matrix4x4.CreateOrthographicOffCenter(
                0,
                1920,
                1080,
                0,
                0.01f,
                1000
            );
            _textBatch.Render(renderPass, hiResProjectionMatrix);

            commandBuffer.EndRenderPass(renderPass);
        }

        _graphicsDevice.Submit(commandBuffer);
    }

    public override void End() {
        _asyncFileLoader.Dispose();
        _asyncFileLoader = null;
        StaticAudioPacks.pack_0.SliceBuffers();
        StaticAudio.LoadAll();
        SpriteAnimations.LoadAll();
    }

    private void AddStringToTextBatch(string text, int pixelSize, Position position, float rotation) {
        _textBatch.Add(
            Fonts.FromID(Fonts.KosugiID),
            text,
            pixelSize,
            Matrix4x4.CreateRotationX(-rotation) * Matrix4x4.CreateTranslation(position.X, position.Y, -1),
            Color.White,
            HorizontalAlignment.Center,
            VerticalAlignment.Middle
        );
    }
}