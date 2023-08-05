using System.Numerics;
using System.Reflection;
using ImGuiNET;
using Veldrid;
using Vulkan;

namespace NoitaMap.Viewer;

public class ImGuiRenderer
{
    private readonly GraphicsDevice GraphicsDevice;

    private readonly nint Context;

    private Vector2 WindowSize;

    private bool FrameBegun = false;

    private DeviceBuffer _vertexBuffer;
    private DeviceBuffer _indexBuffer;
    private DeviceBuffer _projMatrixBuffer;
    private Texture _fontTexture;
    private Shader _vertexShader;
    private Shader _fragmentShader;
    private ResourceLayout _layout;
    private ResourceLayout _textureLayout;
    private Pipeline _pipeline;
    private ResourceSet _mainResourceSet;
    private ResourceSet _fontTextureResourceSet;

    private readonly Dictionary<TextureView, ResourceSetInfo> _setsByView = new Dictionary<TextureView, ResourceSetInfo>();
    private readonly Dictionary<Texture, TextureView> _autoViewsByTexture = new Dictionary<Texture, TextureView>();
    private readonly Dictionary<IntPtr, ResourceSetInfo> _viewsById = new Dictionary<IntPtr, ResourceSetInfo>();
    private readonly List<IDisposable> _ownedResources = new List<IDisposable>();
    private nint _lastAssignedID = 100;

    private nint FontAtlasId = 1;

    public ImGuiRenderer(GraphicsDevice graphicsDevice, OutputDescription outputDescription, int width, int height)
    {
        GraphicsDevice = graphicsDevice;

        Context = ImGui.CreateContext();

        ImGui.GetIO().Fonts.Flags |= ImFontAtlasFlags.NoBakedLines;

        WindowSize = new Vector2(width, height);

        FontAssets.AddImGuiFont();

        CreateDeviceResources(outputDescription);
    }

    public void RecreateFontDeviceTexture()
    {

    }

    public void BeginFrame(float deltaTime)
    {
        if (FrameBegun)
            throw new InvalidOperationException("Frame already begun");

        FrameBegun = true;

        ImGui.NewFrame();

        ImGuiIOPtr io = ImGui.GetIO();

        io.DisplaySize = WindowSize;
        io.DisplayFramebufferScale = Vector2.One;
        io.DeltaTime = deltaTime;
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
    public IntPtr GetOrCreateImGuiBinding(ResourceFactory factory, TextureView textureView)
    {
        if (!_setsByView.TryGetValue(textureView, out ResourceSetInfo rsi))
        {
            _lastAssignedID++;

            ResourceSet resourceSet = factory.CreateResourceSet(new ResourceSetDescription(_textureLayout, textureView));
            resourceSet.Name = $"ImGui.NET {textureView.Name} Resource Set";
            rsi = new ResourceSetInfo(_lastAssignedID, resourceSet);

            _setsByView.Add(textureView, rsi);
            _viewsById.Add(rsi.ImGuiBinding, rsi);
            _ownedResources.Add(resourceSet);
        }

        return rsi.ImGuiBinding;
    }

    public void RemoveImGuiBinding(TextureView textureView)
    {
        if (_setsByView.TryGetValue(textureView, out ResourceSetInfo rsi))
        {
            _setsByView.Remove(textureView);
            _viewsById.Remove(rsi.ImGuiBinding);
            _ownedResources.Remove(rsi.ResourceSet);
            rsi.ResourceSet.Dispose();
        }
    }

    /// <summary>
    /// Gets or creates a handle for a texture to be drawn with ImGui.
    /// Pass the returned handle to Image() or ImageButton().
    /// </summary>
    public IntPtr GetOrCreateImGuiBinding(ResourceFactory factory, Texture texture)
    {
        if (!_autoViewsByTexture.TryGetValue(texture, out TextureView? textureView))
        {
            textureView = factory.CreateTextureView(texture);
            textureView.Name = $"ImGui.NET {texture.Name} View";
            _autoViewsByTexture.Add(texture, textureView);
            _ownedResources.Add(textureView);
        }

        return GetOrCreateImGuiBinding(factory, textureView);
    }

    public void RemoveImGuiBinding(Texture texture)
    {
        if (_autoViewsByTexture.TryGetValue(texture, out TextureView? textureView))
        {
            _autoViewsByTexture.Remove(texture);
            _ownedResources.Remove(textureView);
            textureView.Dispose();
            RemoveImGuiBinding(textureView);
        }
    }

    public ResourceSet GetImageResourceSet(nint id)
    {
        if (!_viewsById.TryGetValue(id, out ResourceSetInfo resourceSetInfo))
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
        if (totalVBSize > _vertexBuffer.SizeInBytes)
        {
            _vertexBuffer.Dispose();
            _vertexBuffer = GraphicsDevice.ResourceFactory.CreateBuffer(new BufferDescription((uint)(totalVBSize * 1.5f), BufferUsage.VertexBuffer | BufferUsage.Dynamic));
            _vertexBuffer.Name = $"ImGui.NET Vertex Buffer";
        }

        uint totalIBSize = (uint)(draw_data.TotalIdxCount * sizeof(ushort));
        if (totalIBSize > _indexBuffer.SizeInBytes)
        {
            _indexBuffer.Dispose();
            _indexBuffer = GraphicsDevice.ResourceFactory.CreateBuffer(new BufferDescription((uint)(totalIBSize * 1.5f), BufferUsage.IndexBuffer | BufferUsage.Dynamic));
            _indexBuffer.Name = $"ImGui.NET Index Buffer";
        }

        for (int i = 0; i < draw_data.CmdListsCount; i++)
        {
            ImDrawListPtr cmd_list = draw_data.CmdListsRange[i];

            cl.UpdateBuffer(
                _vertexBuffer,
                vertexOffsetInVertices * (uint)sizeof(ImDrawVert),
                cmd_list.VtxBuffer.Data,
                (uint)(cmd_list.VtxBuffer.Size * sizeof(ImDrawVert)));

            cl.UpdateBuffer(
                _indexBuffer,
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
                io.DisplaySize.Y,
                0.0f,
                -1.0f,
                1.0f);

            GraphicsDevice.UpdateBuffer(_projMatrixBuffer, 0, ref mvp);
        }

        cl.SetVertexBuffer(0, _vertexBuffer);
        cl.SetIndexBuffer(_indexBuffer, IndexFormat.UInt16);
        cl.SetPipeline(_pipeline);
        cl.SetGraphicsResourceSet(0, _mainResourceSet);

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
                if (pcmd.UserCallback != IntPtr.Zero)
                {
                    throw new NotImplementedException();
                }
                else
                {
                    if (pcmd.TextureId != IntPtr.Zero)
                    {
                        if (pcmd.TextureId == FontAtlasId)
                        {
                            cl.SetGraphicsResourceSet(1, _fontTextureResourceSet);
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
        _vertexBuffer = factory.CreateBuffer(new BufferDescription(10000, BufferUsage.VertexBuffer | BufferUsage.Dynamic));
        _vertexBuffer.Name = "ImGui.NET Vertex Buffer";
        _indexBuffer = factory.CreateBuffer(new BufferDescription(2000, BufferUsage.IndexBuffer | BufferUsage.Dynamic));
        _indexBuffer.Name = "ImGui.NET Index Buffer";

        _projMatrixBuffer = factory.CreateBuffer(new BufferDescription(64, BufferUsage.UniformBuffer | BufferUsage.Dynamic));
        _projMatrixBuffer.Name = "ImGui.NET Projection Buffer";

        byte[] vertexShaderBytes = LoadEmbeddedShaderCode(factory, "imgui-vertex", ShaderStages.Vertex);
        byte[] fragmentShaderBytes = LoadEmbeddedShaderCode(factory, "imgui-frag", ShaderStages.Fragment);
        _vertexShader = factory.CreateShader(new ShaderDescription(ShaderStages.Vertex, vertexShaderBytes, GraphicsDevice.BackendType == GraphicsBackend.Vulkan ? "main" : "VS"));
        _vertexShader.Name = "ImGui.NET Vertex Shader";
        _fragmentShader = factory.CreateShader(new ShaderDescription(ShaderStages.Fragment, fragmentShaderBytes, GraphicsDevice.BackendType == GraphicsBackend.Vulkan ? "main" : "FS"));
        _fragmentShader.Name = "ImGui.NET Fragment Shader";

        VertexLayoutDescription[] vertexLayouts = new VertexLayoutDescription[]
        {
                new VertexLayoutDescription(
                    new VertexElementDescription("in_position", VertexElementSemantic.Position, VertexElementFormat.Float2),
                    new VertexElementDescription("in_texCoord", VertexElementSemantic.TextureCoordinate, VertexElementFormat.Float2),
                    new VertexElementDescription("in_color", VertexElementSemantic.Color, VertexElementFormat.Byte4_Norm))
        };

        _layout = factory.CreateResourceLayout(new ResourceLayoutDescription(
            new ResourceLayoutElementDescription("ProjectionMatrixBuffer", ResourceKind.UniformBuffer, ShaderStages.Vertex),
            new ResourceLayoutElementDescription("MainSampler", ResourceKind.Sampler, ShaderStages.Fragment)));
        _layout.Name = "ImGui.NET Resource Layout";
        _textureLayout = factory.CreateResourceLayout(new ResourceLayoutDescription(
            new ResourceLayoutElementDescription("MainTexture", ResourceKind.TextureReadOnly, ShaderStages.Fragment)));
        _textureLayout.Name = "ImGui.NET Texture Layout";

        GraphicsPipelineDescription pd = new GraphicsPipelineDescription(
            BlendStateDescription.SingleAlphaBlend,
            new DepthStencilStateDescription(false, false, ComparisonKind.Always),
            new RasterizerStateDescription(FaceCullMode.None, PolygonFillMode.Solid, FrontFace.Clockwise, true, true),
            PrimitiveTopology.TriangleList,
            new ShaderSetDescription(
                vertexLayouts,
                new[] { _vertexShader, _fragmentShader },
                new[]
                {
                        new SpecializationConstant(0, GraphicsDevice.IsClipSpaceYInverted),
                        new SpecializationConstant(1, false),
                }),
            new ResourceLayout[] { _layout, _textureLayout },
            outputDescription,
            ResourceBindingModel.Default);
        _pipeline = factory.CreateGraphicsPipeline(ref pd);
        _pipeline.Name = "ImGui.NET Pipeline";

        _mainResourceSet = factory.CreateResourceSet(new ResourceSetDescription(_layout,
            _projMatrixBuffer,
            GraphicsDevice.PointSampler));
        _mainResourceSet.Name = "ImGui.NET Main Resource Set";

        RecreateFontDeviceTexture();
    }

    private byte[] LoadEmbeddedShaderCode(ResourceFactory factory, string name, ShaderStages stage)
    {
        switch (factory.BackendType)
        {
            case GraphicsBackend.Direct3D11:
                {
                    string resourceName = name + ".hlsl.bytes";
                    return GetEmbeddedResourceBytes(resourceName);
                }
            case GraphicsBackend.OpenGL:
                {
                    string resourceName = name + ".glsl";
                    return GetEmbeddedResourceBytes(resourceName);
                }
            case GraphicsBackend.Vulkan:
                {
                    string resourceName = name + ".spv";
                    return GetEmbeddedResourceBytes(resourceName);
                }
            case GraphicsBackend.Metal:
                {
                    string resourceName = name + ".metallib";
                    return GetEmbeddedResourceBytes(resourceName);
                }
            default:
                throw new NotImplementedException();
        }
    }

    private string GetEmbeddedResourceText(string resourceName)
    {
        using (StreamReader sr = new StreamReader(typeof(ImGuiRenderer).Assembly.GetManifestResourceStream(resourceName)!))
        {
            return sr.ReadToEnd();
        }
    }

    private byte[] GetEmbeddedResourceBytes(string resourceName)
    {
        using (Stream s = typeof(ImGuiRenderer).Assembly.GetManifestResourceStream(resourceName)!)
        {
            byte[] ret = new byte[s.Length];
            s.Read(ret, 0, (int)s.Length);
            return ret;
        }
    }


    private struct ResourceSetInfo
    {
        public readonly IntPtr ImGuiBinding;

        public readonly ResourceSet ResourceSet;

        public ResourceSetInfo(IntPtr imGuiBinding, ResourceSet resourceSet)
        {
            ImGuiBinding = imGuiBinding;
            ResourceSet = resourceSet;
        }
    }
}
