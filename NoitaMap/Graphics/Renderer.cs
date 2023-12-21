using System.Diagnostics;
using System.Numerics;
using ImGuiNET;
using NoitaMap.Graphics;
using NoitaMap.Logging;
using NoitaMap.Map;
using NoitaMap.Viewer;
using NoitaMap.Map.Entities;
using NoitaMap.Startup;
using Silk.NET.Maths;
using Silk.NET.Windowing;
using Silk.NET.Windowing.Sdl;
using Veldrid;

namespace NoitaMap.Graphics;

public class Renderer : IDisposable
{
    public readonly IWindow Window;

    public GraphicsDevice GraphicsDevice;

    private CommandList MainCommandList;

    private Framebuffer MainFrameBuffer;

    private Pipeline MainPipeline;

    private ResourceLayout VertexResourceLayout;

    private ResourceLayout PixelSamplerResourceLayout;

    private ResourceLayout PixelTextureResourceLayout;

    private ResourceSet VertexResourceSet;

    private ResourceSet PixelSamplerResourceSet;

    public MaterialProvider MaterialProvider;

    public ConstantBuffer<VertexConstantBuffer> ConstantBuffer;

    public ImGuiRenderer ImGuiRenderer;

    public List<IRenderable> Renderables = new List<IRenderable>();

    private bool Disposed;

    public Renderer(IWindow window, GraphicsDevice graphicsDevice)
    {
        Window = window;

        GraphicsDevice = graphicsDevice;

        MainCommandList = GraphicsDevice.ResourceFactory.CreateCommandList();

        MainFrameBuffer = GraphicsDevice.MainSwapchain.Framebuffer;

        Shader[] shaders = ShaderLoader.Load(GraphicsDevice, "Map/PixelShader", "Map/VertexShader");

        VertexResourceLayout = GraphicsDevice.ResourceFactory.CreateResourceLayout(new ResourceLayoutDescription()
        {
            Elements =
            [
                new ResourceLayoutElementDescription("VertexShaderBuffer", ResourceKind.UniformBuffer, ShaderStages.Vertex)
            ]
        });

        PixelSamplerResourceLayout = GraphicsDevice.ResourceFactory.CreateResourceLayout(new ResourceLayoutDescription()
        {
            Elements =
            [
                new ResourceLayoutElementDescription("PointSamplerView", ResourceKind.Sampler, ShaderStages.Fragment)
            ]
        });

        PixelTextureResourceLayout = GraphicsDevice.ResourceFactory.CreateResourceLayout(new ResourceLayoutDescription()
        {
            Elements =
            [
                new ResourceLayoutElementDescription("MainTextureView", ResourceKind.TextureReadOnly, ShaderStages.Fragment)
            ]
        });

        MainPipeline = CreatePipeline(shaders);

        foreach (Shader shader in shaders)
        {
            shader.Dispose();
        }

        MaterialProvider = new MaterialProvider();

        ConstantBuffer = new ConstantBuffer<VertexConstantBuffer>(GraphicsDevice);

        ConstantBuffer.Data.ViewProjection = Matrix4x4.CreateOrthographic(Window.Size.X, Window.Size.Y, 0f, 1f);

        VertexResourceSet = GraphicsDevice.ResourceFactory.CreateResourceSet(new ResourceSetDescription(VertexResourceLayout, ConstantBuffer.DeviceBuffer));

        PixelSamplerResourceSet = GraphicsDevice.ResourceFactory.CreateResourceSet(new ResourceSetDescription(PixelSamplerResourceLayout, GraphicsDevice.PointSampler));

        ImGuiRenderer = new ImGuiRenderer(GraphicsDevice, MainFrameBuffer.OutputDescription, Window.Size.X, Window.Size.Y);

        Window.Resize += HandleResize;
    }

    private Pipeline CreatePipeline(Shader[] shaders)
    {
        return GraphicsDevice.ResourceFactory.CreateGraphicsPipeline(new GraphicsPipelineDescription()
        {
            BlendState = BlendStateDescription.SingleAlphaBlend,
            DepthStencilState = new DepthStencilStateDescription()
            {
                DepthComparison = ComparisonKind.Less,
                DepthTestEnabled = true,
                DepthWriteEnabled = true
            },
            Outputs = MainFrameBuffer.OutputDescription,
            PrimitiveTopology = PrimitiveTopology.TriangleList,
            RasterizerState = new RasterizerStateDescription()
            {
                CullMode = FaceCullMode.None,
                FillMode = PolygonFillMode.Solid,
                FrontFace = FrontFace.Clockwise,
            },
            ShaderSet = new ShaderSetDescription()
            {
                Shaders = shaders,
                VertexLayouts =
                [
                    new VertexLayoutDescription
                    (
                        new VertexElementDescription("position",    VertexElementFormat.Float3, VertexElementSemantic.TextureCoordinate),
                        new VertexElementDescription("uv",          VertexElementFormat.Float2, VertexElementSemantic.TextureCoordinate)
                    ),
                    new VertexLayoutDescription
                    (
                        stride: 80,
                        instanceStepRate: 6,
                        new VertexElementDescription("worldMatrix", VertexElementFormat.Float4, VertexElementSemantic.TextureCoordinate),
                        new VertexElementDescription("worldMatrix", VertexElementFormat.Float4, VertexElementSemantic.TextureCoordinate),
                        new VertexElementDescription("worldMatrix", VertexElementFormat.Float4, VertexElementSemantic.TextureCoordinate),
                        new VertexElementDescription("worldMatrix", VertexElementFormat.Float4, VertexElementSemantic.TextureCoordinate),
                        new VertexElementDescription("texPos",      VertexElementFormat.Float2, VertexElementSemantic.TextureCoordinate),
                        new VertexElementDescription("texSize",     VertexElementFormat.Float2, VertexElementSemantic.TextureCoordinate)
                    ),
                ],
            },
            ResourceLayouts = [VertexResourceLayout, PixelSamplerResourceLayout, PixelTextureResourceLayout],
        });
    }

    private Vector2 MouseTranslateOrigin = Vector2.Zero;

    public Vector2 ViewScale { get; private set; } = Vector2.One;

    public Vector2 ViewOffset { get; private set; } = Vector2.Zero;

    public Matrix4x4 View =>
            Matrix4x4.CreateTranslation(new Vector3(-ViewOffset, 0f)) *
            Matrix4x4.CreateScale(new Vector3(ViewScale, 1f));

    public Matrix4x4 Projection =>
        Matrix4x4.CreateOrthographic(Window.Size.X, Window.Size.Y, 0f, 1f);

    public void Update()
    {
        Vector2 originalScaledMouse = ScalePosition(InputSystem.MousePosition);

        ViewScale += new Vector2(InputSystem.ScrollDelta) * (ViewScale / 10f);
        ViewScale = Vector2.Clamp(ViewScale, new Vector2(0.01f, 0.01f), new Vector2(20f, 20f));

        Vector2 currentScaledMouse = ScalePosition(InputSystem.MousePosition);

        // Zoom in on where the mouse is
        ViewOffset += originalScaledMouse - currentScaledMouse;

        if (InputSystem.LeftMousePressed)
        {
            MouseTranslateOrigin = ScalePosition(InputSystem.MousePosition) + ViewOffset;
        }

        if (InputSystem.LeftMouseDown)
        {
            Vector2 currentMousePosition = ScalePosition(InputSystem.MousePosition) + ViewOffset;

            ViewOffset += MouseTranslateOrigin - currentMousePosition;
        }

        foreach (IRenderable renderable in Renderables)
        {
            renderable.Update();
        }
    }

    private Vector2 ScalePosition(Vector2 position)
    {
        return position / ViewScale;
    }

    public void Render()
    {
        ConstantBuffer.Data.ViewProjection =
            View *
            Projection *
            Matrix4x4.CreateTranslation(-1f, -1f, 0f);

        ConstantBuffer.Update();

        MainCommandList.Begin();

        MainCommandList.SetFramebuffer(MainFrameBuffer);

        MainCommandList.ClearColorTarget(0, RgbaFloat.CornflowerBlue);

        MainCommandList.SetPipeline(MainPipeline);

        MainCommandList.SetGraphicsResourceSet(0, VertexResourceSet);

        MainCommandList.SetGraphicsResourceSet(1, PixelSamplerResourceSet);

        foreach (IRenderable renderable in Renderables)
        {
            renderable.Render(MainCommandList);
        }

        // DrawUI();

        // ImGuiRenderer.EndFrame(MainCommandList);

        MainCommandList.End();

        GraphicsDevice.SubmitCommands(MainCommandList);

        GraphicsDevice.SwapBuffers();
    }

    public ResourceSet CreateTextureBinding(Texture texture)
    {
        return GraphicsDevice.ResourceFactory.CreateResourceSet(new ResourceSetDescription()
        {
            BoundResources = [texture],
            Layout = PixelTextureResourceLayout
        });
    }

    private void HandleResize(Vector2D<int> size)
    {
        GraphicsDevice.ResizeMainWindow((uint)size.X, (uint)size.Y);

        MainFrameBuffer = GraphicsDevice.MainSwapchain.Framebuffer;

        // ChunkContainer.HandleResize();

        ImGuiRenderer.HandleResize(size.X, size.Y);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!Disposed)
        {
            MainFrameBuffer.Dispose();

            ConstantBuffer.Dispose();

            MainPipeline.Dispose();

            foreach (IRenderable renderable in Renderables)
            {
                renderable.Dispose();
            }

            VertexResourceLayout.Dispose();

            PixelSamplerResourceLayout.Dispose();

            PixelTextureResourceLayout.Dispose();

            MainCommandList.Dispose();

            GraphicsDevice.Dispose();

            Disposed = true;
        }
    }

    public void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }
}