using System;
using System.IO;
using System.Numerics;
using System.Runtime.InteropServices;
using MoonWorks.Graphics;
using Buffer = MoonWorks.Graphics.Buffer;

namespace Tactician.Graphics;

public class SpriteBatch {
    public uint InstanceCount => (uint)_instanceIndex;

    private const int MAX_SPRITE_COUNT = 8192;
    private readonly GraphicsDevice _graphicsDevice;
    private readonly ComputePipeline _computePipeline;
    private readonly GraphicsPipeline _graphicsPipeline;
    private readonly Buffer _instanceBuffer;
    private readonly TransferBuffer _instanceTransferBuffer;
    private readonly Buffer _quadIndexBuffer;
    private readonly Buffer _quadVertexBuffer;

    private int _instanceIndex;

	public SpriteBatch(GraphicsDevice graphicsDevice, MoonWorks.Storage.TitleStorage titleStorage, TextureFormat renderTextureFormat, TextureFormat? depthTextureFormat = null)
	{
		_graphicsDevice = graphicsDevice;

        var shaderContentPath = System.IO.Path.Combine(
            "Content",
			"Shaders"
        );

        _computePipeline = ShaderCross.Create(_graphicsDevice, titleStorage, System.IO.Path.Combine(shaderContentPath, "SpriteBatch.comp.hlsl.spv"), "main", ShaderCross.ShaderFormat.SPIRV);

		var vertShader = ShaderCross.Create(_graphicsDevice, titleStorage, System.IO.Path.Combine(shaderContentPath, "SpriteBatch.vert.hlsl.spv"), "main", ShaderCross.ShaderFormat.SPIRV, ShaderStage.Vertex);
		var fragShader = ShaderCross.Create(_graphicsDevice, titleStorage, System.IO.Path.Combine(shaderContentPath, "SpriteBatch.frag.hlsl.spv"), "main", ShaderCross.ShaderFormat.SPIRV, ShaderStage.Fragment);

        var createInfo = new GraphicsPipelineCreateInfo {
            TargetInfo = new GraphicsPipelineTargetInfo {
                ColorTargetDescriptions = [
                    new ColorTargetDescription {
                        Format = renderTextureFormat,
                        BlendState = ColorTargetBlendState.NonPremultipliedAlphaBlend
                    }
                ]
            },
            DepthStencilState = DepthStencilState.Disable,
            MultisampleState = MultisampleState.None,
            PrimitiveType = PrimitiveType.TriangleList,
            RasterizerState = RasterizerState.CCW_CullNone,
            VertexInputState = VertexInputState.CreateSingleBinding<PositionTextureColorVertex>(),
            VertexShader = vertShader,
            FragmentShader = fragShader,
            Name = "SpriteBatch Pipeline"
        };

        if (depthTextureFormat.HasValue) {
            createInfo.TargetInfo.DepthStencilFormat = depthTextureFormat.Value;
            createInfo.TargetInfo.HasDepthStencilTarget = true;

            createInfo.DepthStencilState = new DepthStencilState {
                EnableDepthTest = true,
                EnableDepthWrite = true,
                CompareOp = CompareOp.LessOrEqual
            };
        }

        _graphicsPipeline = GraphicsPipeline.Create(
            graphicsDevice,
            createInfo
        );

        fragShader.Dispose();
        vertShader.Dispose();

        _instanceTransferBuffer = TransferBuffer.Create<SpriteInstanceData>(graphicsDevice,
            "SpriteBatch InstanceTransferBuffer", TransferBufferUsage.Upload, MAX_SPRITE_COUNT);

        _instanceBuffer = Buffer.Create<SpriteInstanceData>(graphicsDevice,
            BufferUsageFlags.Vertex | BufferUsageFlags.ComputeStorageRead, MAX_SPRITE_COUNT);
        _instanceIndex = 0;

        var spriteIndexTransferBuffer = TransferBuffer.Create<uint>(
            graphicsDevice,
            "SpriteIndex TransferBuffer",
            TransferBufferUsage.Upload,
            MAX_SPRITE_COUNT * 6
        );

        _quadVertexBuffer = Buffer.Create<PositionTextureColorVertex>(
            graphicsDevice,
            "Quad Vertex",
            BufferUsageFlags.ComputeStorageWrite | BufferUsageFlags.Vertex,
            MAX_SPRITE_COUNT * 4
        );

        _quadIndexBuffer = Buffer.Create<uint>(
            graphicsDevice,
            "Quad Index",
            BufferUsageFlags.Index,
            MAX_SPRITE_COUNT * 6
        );

        var indexSpan = spriteIndexTransferBuffer.Map<uint>(false);

        for (int i = 0, j = 0; i < MAX_SPRITE_COUNT * 6; i += 6, j += 4) {
            indexSpan[i] = (uint)j;
            indexSpan[i + 1] = (uint)j + 1;
            indexSpan[i + 2] = (uint)j + 2;
            indexSpan[i + 3] = (uint)j + 3;
            indexSpan[i + 4] = (uint)j + 2;
            indexSpan[i + 5] = (uint)j + 1;
        }

        spriteIndexTransferBuffer.Unmap();

        var cmdbuf = graphicsDevice.AcquireCommandBuffer();
        var copyPass = cmdbuf.BeginCopyPass();
        copyPass.UploadToBuffer(spriteIndexTransferBuffer, _quadIndexBuffer, false);
        cmdbuf.EndCopyPass(copyPass);
        graphicsDevice.Submit(cmdbuf);

        spriteIndexTransferBuffer.Dispose();
    }

    // Call this before adding sprites
    public void Start() {
        _instanceIndex = 0;
        _instanceTransferBuffer.Map(true);
    }

    // Add a sprite to the batch
    public void Add(
        Vector3 position,
        float rotation,
        Vector2 size,
        Color color,
        Vector2 leftTopUV,
        Vector2 dimensionsUV
    ) {
        var left = leftTopUV.X;
        var top = leftTopUV.Y;
        var right = leftTopUV.X + dimensionsUV.X;
        var bottom = leftTopUV.Y + dimensionsUV.Y;

        var instanceDatas = _instanceTransferBuffer.MappedSpan<SpriteInstanceData>();
        instanceDatas[_instanceIndex].Translation = position;
        instanceDatas[_instanceIndex].Rotation = rotation;
        instanceDatas[_instanceIndex].Scale = size;
        instanceDatas[_instanceIndex].Color = color.ToVector4();
        instanceDatas[_instanceIndex].UV0 = leftTopUV;
        instanceDatas[_instanceIndex].UV1 = new Vector2(right, top);
        instanceDatas[_instanceIndex].UV2 = new Vector2(left, bottom);
        instanceDatas[_instanceIndex].UV3 = new Vector2(right, bottom);
        _instanceIndex += 1;
    }

    // Call this outside of any pass
    public void Upload(CommandBuffer commandBuffer) {
        _instanceTransferBuffer.Unmap();

        if (InstanceCount > 0) {
            var copyPass = commandBuffer.BeginCopyPass();
            copyPass.UploadToBuffer(new TransferBufferLocation(_instanceTransferBuffer),
                new BufferRegion(_instanceBuffer, 0, (uint)(Marshal.SizeOf<SpriteInstanceData>() * InstanceCount)),
                true);
            commandBuffer.EndCopyPass(copyPass);

            var computePass = commandBuffer.BeginComputePass(
                new StorageBufferReadWriteBinding(_quadVertexBuffer, true)
            );
            computePass.BindComputePipeline(_computePipeline);
            computePass.BindStorageBuffers(_instanceBuffer);
            computePass.Dispatch((InstanceCount + 63) / 64, 1, 1);
            commandBuffer.EndComputePass(computePass);
        }
    }

    public void Render(RenderPass renderPass, Texture texture, Sampler sampler,
        ViewProjectionMatrices viewProjectionMatrices) {
        renderPass.BindGraphicsPipeline(_graphicsPipeline);
        renderPass.BindFragmentSamplers(new TextureSamplerBinding(texture, sampler));
        renderPass.BindVertexBuffers(new BufferBinding(_quadVertexBuffer));
        renderPass.BindIndexBuffer(_quadIndexBuffer, IndexElementSize.ThirtyTwo);
        renderPass.CommandBuffer.PushVertexUniformData(viewProjectionMatrices.View * viewProjectionMatrices.Projection);
        renderPass.DrawIndexedPrimitives(InstanceCount * 6, 1, 0, 0, 0);
    }
}

[StructLayout(LayoutKind.Explicit, Size = 48)]
internal struct PositionTextureColorVertex : IVertexType {
    [FieldOffset(0)] public Vector4 Position;

    [FieldOffset(16)] public Vector2 TexCoord;

    [FieldOffset(32)] public Vector4 Color;

    public static VertexElementFormat[] Formats { get; } = [
        VertexElementFormat.Float4,
        VertexElementFormat.Float2,
        VertexElementFormat.Float4
    ];

    public static uint[] Offsets { get; } = [
        0,
        16,
        32
    ];
}

[StructLayout(LayoutKind.Explicit, Size = 80)]
public record struct SpriteInstanceData {
    [FieldOffset(0)] public Vector3 Translation;
    [FieldOffset(12)] public float Rotation;
    [FieldOffset(16)] public Vector2 Scale;
    [FieldOffset(32)] public Vector4 Color;
    [FieldOffset(48)] public Vector2 UV0;
    [FieldOffset(56)] public Vector2 UV1;
    [FieldOffset(64)] public Vector2 UV2;
    [FieldOffset(72)] public Vector2 UV3;
}

public readonly record struct ViewProjectionMatrices(Matrix4x4 View, Matrix4x4 Projection);