using System.Numerics;
using ImGuiNET;
using NoitaMap.Graphics;
using Veldrid;

namespace NoitaMap.Viewer;

public class ImGuiRenderer : IDisposable
{
    private readonly GraphicsDevice GraphicsDevice;

    private readonly nint Context;

    private Vector2 WindowSize;

    private bool FrameBegun = false;

    private DeviceBuffer VertexBuffer;

    private DeviceBuffer IndexBuffer;

    private DeviceBuffer ProjectionConstantBuffer;

    private Texture FontTexture;

    private Shader VertexShader;

    private Shader PixelShader;

    private ResourceLayout Layout;

    private ResourceLayout TextureLayout;

    private Pipeline ImGuiPipeline;

    private ResourceSet MainResourceSet;

    private ResourceSet FontTextureResourceSet;

    private readonly Dictionary<TextureView, ResourceSetInfo> SetsByView = new Dictionary<TextureView, ResourceSetInfo>();

    private readonly Dictionary<Texture, TextureView> AutoViewsByTexture = new Dictionary<Texture, TextureView>();

    private readonly Dictionary<nint, ResourceSetInfo> ViewsById = new Dictionary<nint, ResourceSetInfo>();

    private readonly List<IDisposable> OwnedResources = new List<IDisposable>();

    private nint TextureId = 100;

    private readonly nint FontAtlasId = 1;

    private bool Disposed = false;

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
    public ImGuiRenderer(GraphicsDevice graphicsDevice, OutputDescription outputDescription, int width, int height)
#pragma warning restore CS8618
    {
        GraphicsDevice = graphicsDevice;

        Context = ImGui.CreateContext();

        ImGui.GetIO().Fonts.Flags |= ImFontAtlasFlags.NoBakedLines;

        WindowSize = new Vector2(width, height);

        ImGui.GetIO().Fonts.AddFontDefault();

        CreateDeviceResources(outputDescription);

        FontAssets.AddImGuiFont();

        RecreateFontDeviceTexture();
    }

    public unsafe void RecreateFontDeviceTexture()
    {
        ImGuiIOPtr io = ImGui.GetIO();

        io.Fonts.GetTexDataAsRGBA32(out byte* pixels, out int width, out int height);

        io.Fonts.SetTexID(FontAtlasId);

        GraphicsDevice.WaitForIdle();

        FontTexture?.Dispose();
        FontTexture = GraphicsDevice.ResourceFactory.CreateTexture(TextureDescription.Texture2D((uint)width, (uint)height, 1, 1, PixelFormat.R8_G8_B8_A8_UNorm, TextureUsage.Sampled));
        FontTexture.Name = "ImGui.NET Font Texture";
        GraphicsDevice.UpdateTexture(FontTexture, (nint)pixels, (uint)(4 * width * height), 0, 0, 0, (uint)width, (uint)height, 1, 0, 0);

        FontTextureResourceSet?.Dispose();
        FontTextureResourceSet = GraphicsDevice.ResourceFactory.CreateResourceSet(new ResourceSetDescription(TextureLayout, FontTexture));
        FontTextureResourceSet.Name = "ImGui.NET Font Texture Resource Set";

        io.Fonts.ClearTexData();
    }

    public void BeginFrame(float deltaTime, InputSnapshot inputSnapshot)
    {
        if (FrameBegun)
            throw new InvalidOperationException("Frame already begun");

        FrameBegun = true;

        ImGuiIOPtr io = ImGui.GetIO();

        io.DisplaySize = WindowSize;
        io.DisplayFramebufferScale = Vector2.One;
        io.DeltaTime = deltaTime;

        foreach (KeyEvent keyEvent in inputSnapshot.KeyEvents)
        {
            io.AddKeyEvent(KeyTranslator.GetKey(keyEvent.Key), keyEvent.Down);
        }

        io.MousePos = InputSystem.MousePosition;

        io.MouseDown[(int)ImGuiMouseButton.Left] = inputSnapshot.IsMouseDown(MouseButton.Left);
        io.MouseDown[(int)ImGuiMouseButton.Right] = inputSnapshot.IsMouseDown(MouseButton.Right);
        io.MouseDown[(int)ImGuiMouseButton.Middle] = inputSnapshot.IsMouseDown(MouseButton.Middle);

        ImGui.NewFrame();
    }

    public void EndFrame(CommandList commandList)
    {
        if (!FrameBegun)
            throw new InvalidOperationException("Frame not begun");

        ImGui.EndFrame();

        ImGui.Render();
        RenderImDrawData(ImGui.GetDrawData(), commandList);

        FrameBegun = false;
    }

    /// <summary>
    /// Gets or creates a handle for a texture to be drawn with ImGui.
    /// Pass the returned handle to Image() or ImageButton().
    /// </summary>
    public nint GetOrCreateImGuiBinding(ResourceFactory factory, TextureView textureView)
    {
        if (!SetsByView.TryGetValue(textureView, out ResourceSetInfo rsi))
        {
            TextureId++;

            ResourceSet resourceSet = factory.CreateResourceSet(new ResourceSetDescription(TextureLayout, textureView));
            resourceSet.Name = $"ImGui.NET {textureView.Name} Resource Set";
            rsi = new ResourceSetInfo(TextureId, resourceSet);

            SetsByView.Add(textureView, rsi);
            ViewsById.Add(rsi.ImGuiBinding, rsi);
            OwnedResources.Add(resourceSet);
        }

        return rsi.ImGuiBinding;
    }

    public void RemoveImGuiBinding(TextureView textureView)
    {
        if (SetsByView.TryGetValue(textureView, out ResourceSetInfo rsi))
        {
            SetsByView.Remove(textureView);
            ViewsById.Remove(rsi.ImGuiBinding);
            OwnedResources.Remove(rsi.ResourceSet);
            rsi.ResourceSet.Dispose();
        }
    }

    /// <summary>
    /// Gets or creates a handle for a texture to be drawn with ImGui.
    /// Pass the returned handle to Image() or ImageButton().
    /// </summary>
    public nint GetOrCreateImGuiBinding(ResourceFactory factory, Texture texture)
    {
        if (!AutoViewsByTexture.TryGetValue(texture, out TextureView? textureView))
        {
            textureView = factory.CreateTextureView(texture);
            textureView.Name = $"ImGui.NET {texture.Name} View";
            AutoViewsByTexture.Add(texture, textureView);
            OwnedResources.Add(textureView);
        }

        return GetOrCreateImGuiBinding(factory, textureView);
    }

    public void RemoveImGuiBinding(Texture texture)
    {
        if (AutoViewsByTexture.TryGetValue(texture, out TextureView? textureView))
        {
            AutoViewsByTexture.Remove(texture);
            OwnedResources.Remove(textureView);
            textureView.Dispose();
            RemoveImGuiBinding(textureView);
        }
    }

    public void HandleResize(int width, int height)
    {
        WindowSize = new Vector2(width, height);
    }

    public ResourceSet GetImageResourceSet(nint id)
    {
        if (!ViewsById.TryGetValue(id, out ResourceSetInfo resourceSetInfo))
        {
            throw new InvalidOperationException($"No registered ImGui binding (id = {id})");
        }

        return resourceSetInfo.ResourceSet;
    }

    private unsafe void RenderImDrawData(ImDrawDataPtr draw_data, CommandList cl)
    {
        uint vertexOffsetInVertices = 0;
        uint indexOffsetInElements = 0;

        if (draw_data.CmdListsCount == 0)
        {
            return;
        }

        uint totalVBSize = (uint)(draw_data.TotalVtxCount * sizeof(ImDrawVert));
        if (totalVBSize > VertexBuffer.SizeInBytes)
        {
            VertexBuffer.Dispose();
            VertexBuffer = GraphicsDevice.ResourceFactory.CreateBuffer(new BufferDescription((uint)(totalVBSize * 1.5f), BufferUsage.VertexBuffer | BufferUsage.Dynamic));
            VertexBuffer.Name = $"ImGui.NET Vertex Buffer";
        }

        uint totalIBSize = (uint)(draw_data.TotalIdxCount * sizeof(ushort));
        if (totalIBSize > IndexBuffer.SizeInBytes)
        {
            IndexBuffer.Dispose();
            IndexBuffer = GraphicsDevice.ResourceFactory.CreateBuffer(new BufferDescription((uint)(totalIBSize * 1.5f), BufferUsage.IndexBuffer | BufferUsage.Dynamic));
            IndexBuffer.Name = $"ImGui.NET Index Buffer";
        }

        for (int i = 0; i < draw_data.CmdListsCount; i++)
        {
            ImDrawListPtr cmd_list = draw_data.CmdListsRange[i];

            cl.UpdateBuffer(
                VertexBuffer,
                vertexOffsetInVertices * (uint)sizeof(ImDrawVert),
                cmd_list.VtxBuffer.Data,
                (uint)(cmd_list.VtxBuffer.Size * sizeof(ImDrawVert)));

            cl.UpdateBuffer(
                IndexBuffer,
                indexOffsetInElements * sizeof(ushort),
                cmd_list.IdxBuffer.Data,
                (uint)(cmd_list.IdxBuffer.Size * sizeof(ushort)));

            vertexOffsetInVertices += (uint)cmd_list.VtxBuffer.Size;
            indexOffsetInElements += (uint)cmd_list.IdxBuffer.Size;
        }

        // Setup orthographic projection matrix into our constant buffer
        {
            var io = ImGui.GetIO();

            Matrix4x4 mvp = Matrix4x4.CreateOrthographicOffCenter(
                0f,
                io.DisplaySize.X,
                0.0f,
                io.DisplaySize.Y,
                -1.0f,
                1.0f);

            GraphicsDevice.UpdateBuffer(ProjectionConstantBuffer, 0, ref mvp);
        }

        cl.SetVertexBuffer(0, VertexBuffer);
        cl.SetIndexBuffer(IndexBuffer, IndexFormat.UInt16);
        cl.SetPipeline(ImGuiPipeline);
        cl.SetGraphicsResourceSet(0, MainResourceSet);

        draw_data.ScaleClipRects(ImGui.GetIO().DisplayFramebufferScale);

        // Render command lists
        int vtx_offset = 0;
        int idx_offset = 0;
        for (int n = 0; n < draw_data.CmdListsCount; n++)
        {
            ImDrawListPtr cmd_list = draw_data.CmdListsRange[n];
            for (int cmd_i = 0; cmd_i < cmd_list.CmdBuffer.Size; cmd_i++)
            {
                ImDrawCmdPtr pcmd = cmd_list.CmdBuffer[cmd_i];
                if (pcmd.UserCallback != nint.Zero)
                {
                    throw new NotImplementedException();
                }
                else
                {
                    if (pcmd.TextureId != nint.Zero)
                    {
                        if (pcmd.TextureId == FontAtlasId)
                        {
                            cl.SetGraphicsResourceSet(1, FontTextureResourceSet);
                        }
                        else
                        {
                            cl.SetGraphicsResourceSet(1, GetImageResourceSet(pcmd.TextureId));
                        }
                    }

                    cl.SetScissorRect(
                        0,
                        (uint)pcmd.ClipRect.X,
                        (uint)pcmd.ClipRect.Y,
                        (uint)(pcmd.ClipRect.Z - pcmd.ClipRect.X),
                        (uint)(pcmd.ClipRect.W - pcmd.ClipRect.Y));

                    cl.DrawIndexed(pcmd.ElemCount, 1, pcmd.IdxOffset + (uint)idx_offset, (int)(pcmd.VtxOffset + vtx_offset), 0);
                }
            }

            idx_offset += cmd_list.IdxBuffer.Size;
            vtx_offset += cmd_list.VtxBuffer.Size;
        }
    }

    public void CreateDeviceResources(OutputDescription outputDescription)
    {
        ResourceFactory factory = GraphicsDevice.ResourceFactory;
        VertexBuffer = factory.CreateBuffer(new BufferDescription(10000, BufferUsage.VertexBuffer | BufferUsage.Dynamic));
        VertexBuffer.Name = "ImGui.NET Vertex Buffer";
        IndexBuffer = factory.CreateBuffer(new BufferDescription(2000, BufferUsage.IndexBuffer | BufferUsage.Dynamic));
        IndexBuffer.Name = "ImGui.NET Index Buffer";

        ProjectionConstantBuffer = factory.CreateBuffer(new BufferDescription(64, BufferUsage.UniformBuffer | BufferUsage.Dynamic));
        ProjectionConstantBuffer.Name = "ImGui.NET Projection Buffer";

        Shader[] shaders = ShaderLoader.Load(GraphicsDevice, "ImGui/PixelShader", "ImGui/VertexShader");
        VertexShader = shaders[0];
        PixelShader = shaders[1];

        VertexLayoutDescription[] vertexLayouts = new VertexLayoutDescription[]
        {
                new VertexLayoutDescription(
                    new VertexElementDescription("in_position", VertexElementSemantic.Position, VertexElementFormat.Float2),
                    new VertexElementDescription("in_texCoord", VertexElementSemantic.TextureCoordinate, VertexElementFormat.Float2),
                    new VertexElementDescription("in_color", VertexElementSemantic.Color, VertexElementFormat.Byte4_Norm))
        };

        Layout = factory.CreateResourceLayout(new ResourceLayoutDescription(
            new ResourceLayoutElementDescription("ProjectionMatrixBuffer", ResourceKind.UniformBuffer, ShaderStages.Vertex),
            new ResourceLayoutElementDescription("MainSampler", ResourceKind.Sampler, ShaderStages.Fragment)));
        Layout.Name = "ImGui.NET Resource Layout";
        TextureLayout = factory.CreateResourceLayout(new ResourceLayoutDescription(
            new ResourceLayoutElementDescription("MainTexture", ResourceKind.TextureReadOnly, ShaderStages.Fragment)));
        TextureLayout.Name = "ImGui.NET Texture Layout";

        GraphicsPipelineDescription pd = new GraphicsPipelineDescription(
            BlendStateDescription.SingleAlphaBlend,
            new DepthStencilStateDescription(false, false, ComparisonKind.Always),
            new RasterizerStateDescription(FaceCullMode.None, PolygonFillMode.Solid, FrontFace.Clockwise, true, true),
            PrimitiveTopology.TriangleList,
            new ShaderSetDescription(
                vertexLayouts,
                new[] { VertexShader, PixelShader }),
            new ResourceLayout[] { Layout, TextureLayout },
            outputDescription,
            ResourceBindingModel.Default);
        ImGuiPipeline = factory.CreateGraphicsPipeline(ref pd);
        ImGuiPipeline.Name = "ImGui.NET Pipeline";

        MainResourceSet = factory.CreateResourceSet(new ResourceSetDescription(Layout,
            ProjectionConstantBuffer,
            GraphicsDevice.PointSampler));
        MainResourceSet.Name = "ImGui.NET Main Resource Set";

        RecreateFontDeviceTexture();
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!Disposed)
        {
            foreach (IDisposable disposable in OwnedResources)
            {
                disposable.Dispose();
            }

            Layout.Dispose();

            TextureLayout.Dispose();

            FontTextureResourceSet.Dispose();

            VertexBuffer.Dispose();

            IndexBuffer.Dispose();

            MainResourceSet.Dispose();

            ProjectionConstantBuffer.Dispose();

            FontTexture.Dispose();

            VertexShader.Dispose();

            PixelShader.Dispose();

            ImGuiPipeline.Dispose();

            Disposed = true;
        }
    }

    public void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }

    private static byte[] LoadEmbeddedShaderCode(ResourceFactory factory, string name, ShaderStages stage)
    {
        switch (factory.BackendType)
        {
            case GraphicsBackend.Direct3D11:
                {
                    if (stage == ShaderStages.Vertex)
                    {
                        name += "-legacy";
                    }
                    return GetEmbeddedResourceBytes($"{name}.hlsl.bytes");
                }
            case GraphicsBackend.OpenGL:
                {
                    if (stage == ShaderStages.Vertex)
                    {
                        name += "-legacy";
                    }
                    return GetEmbeddedResourceBytes($"{name}.glsl");
                }
            case GraphicsBackend.Vulkan:
                {
                    return GetEmbeddedResourceBytes($"{name}.spv");
                }
            case GraphicsBackend.Metal:
                {
                    return GetEmbeddedResourceBytes($"{name}.metallib");
                }
            default:
                throw new NotImplementedException();
        }
    }

    private static byte[] GetEmbeddedResourceBytes(string resourceName)
    {
        using Stream s = typeof(ImGuiRenderer).Assembly.GetManifestResourceStream(resourceName)!;

        byte[] ret = new byte[s.Length];
        s.Read(ret, 0, (int)s.Length);

        return ret;
    }

    private struct ResourceSetInfo
    {
        public readonly nint ImGuiBinding;

        public readonly ResourceSet ResourceSet;

        public ResourceSetInfo(nint imGuiBinding, ResourceSet resourceSet)
        {
            ImGuiBinding = imGuiBinding;
            ResourceSet = resourceSet;
        }
    }
}
