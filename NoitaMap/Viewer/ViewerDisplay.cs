using System.Data;
using System.Numerics;
using NoitaMap.Graphics;
using NoitaMap.Map;
using Silk.NET.Input;
using Silk.NET.Maths;
using Silk.NET.Windowing;
using Silk.NET.Windowing.Extensions.Veldrid;
using Veldrid;

namespace NoitaMap.Viewer;

public class ViewerDisplay : IDisposable
{
    private readonly IWindow Window;

    public readonly GraphicsDevice GraphicsDevice;

    private readonly CommandList MainCommandList;

    private Framebuffer MainFrameBuffer;

    private readonly Pipeline MainPipeline;

    private readonly ChunkContainer ChunkContainer;

    public readonly ConstantBuffer<VertexConstantBuffer> ConstantBuffer;

    public readonly MaterialProvider MaterialProvider;

    private ResourceLayoutDescription ResourceLayout;

    public string WorldPath;

    private bool Disposed;

    public ViewerDisplay()
    {
        WindowOptions windowOptions = new WindowOptions()
        {
            API = GraphicsAPI.None,
            Title = "Noita Map Viewer",
            Size = new Vector2D<int>(1280, 720),
            IsContextControlDisabled = true
        };

        GraphicsDeviceOptions graphicsOptions = new GraphicsDeviceOptions()
        {
#if DEBUG
            Debug = true,
#endif
            SyncToVerticalBlank = true,
            HasMainSwapchain = true,
        };

        VeldridWindow.CreateWindowAndGraphicsDevice(windowOptions, graphicsOptions, out Window, out GraphicsDevice);

        Window.IsContextControlDisabled = true;

        MainCommandList = GraphicsDevice.ResourceFactory.CreateCommandList();

        MainFrameBuffer = GraphicsDevice.MainSwapchain.Framebuffer;

        string localLowPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + "Low";

        WorldPath = Path.Combine(localLowPath, "Nolla_Games_Noita\\save00\\world");

        (Shader[] shaders, VertexElementDescription[] vertexElements, ResourceLayoutDescription[] resourceLayout) = ShaderLoader.Load(GraphicsDevice, "PixelShader", "VertexShader");

        ResourceLayout = resourceLayout.First();

        MainPipeline = CreatePipeline(shaders, vertexElements, resourceLayout);

        MaterialProvider = new MaterialProvider();

        ConstantBuffer = new ConstantBuffer<VertexConstantBuffer>(GraphicsDevice);

        ConstantBuffer.Data.ViewProjection = Matrix4x4.CreateOrthographic(Window.Size.X, Window.Size.Y, 0f, 1f);

        ConstantBuffer.Update();

        ChunkContainer = new ChunkContainer(this);

        Window.Center();

        InputSystem.SetInputContext(Window.CreateInput());

        Window.Render += x => Render();

        Window.Update += x => Update();

        Window.Resize += HandleResize;
    }

    public void Start()
    {
        Task.Run(() =>
        {
            const int ChunksPerThread = 16;

            string[] chunkPaths = Directory.EnumerateFiles(WorldPath, "world_*_*.png_petri").ToArray();

            // Split up all of the paths into a collection of (at most) 16 paths for each thread to process
            // This is so that each thread can process 16 chunks at once, rather than having too many threads
            string[][] threadedChunkPaths = new string[(int)MathF.Ceiling((float)chunkPaths.Length / (float)ChunksPerThread)][];

            int total = 0;
            for (int i = 0; i < threadedChunkPaths.Length; i++)
            {
                int chunkCountForThread = Math.Min(chunkPaths.Length - total, ChunksPerThread);
                threadedChunkPaths[i] = new string[chunkCountForThread];

                for (int j = 0; j < chunkCountForThread; j++)
                {
                    threadedChunkPaths[i][j] = chunkPaths[total];

                    total++;
                }
            }

            Parallel.ForEach(threadedChunkPaths, chunkPaths =>
            {
                for (int i = 0; i < chunkPaths.Length; i++)
                {
                    ChunkContainer.LoadChunk(chunkPaths[i]);
                }
            });
        });

        Window.Run();
    }

    private Vector2 MouseTranslateOrigin = Vector2.Zero;

    private Vector2 ViewScale = Vector2.One;

    private Vector2 ViewOffset = Vector2.Zero;

    private void Update()
    {
        InputSystem.Update();

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
    }

    private Vector2 ScalePosition(Vector2 position)
    {
        return position / ViewScale;
    }

    private void Render()
    {
        ConstantBuffer.Data.ViewProjection =
            Matrix4x4.CreateTranslation(new Vector3(-ViewOffset, 0f)) *
            Matrix4x4.CreateScale(new Vector3(ViewScale, 1f)) *
            Matrix4x4.CreateOrthographic(Window.Size.X, Window.Size.Y, 0f, 1f) *
            Matrix4x4.CreateTranslation(-1f, -1f, 0f);

        ConstantBuffer.Update();

        MainCommandList.Begin();

        MainCommandList.SetFramebuffer(MainFrameBuffer);

        MainCommandList.ClearColorTarget(0, RgbaFloat.CornflowerBlue);

        MainCommandList.SetPipeline(MainPipeline);

        ChunkContainer.Draw(MainCommandList);

        MainCommandList.End();

        GraphicsDevice.SubmitCommands(MainCommandList);

        GraphicsDevice.SwapBuffers();
    }

    private Pipeline CreatePipeline(Shader[] shaders, VertexElementDescription[] vertexElements, ResourceLayoutDescription[] resourceLayout)
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
                FrontFace = FrontFace.Clockwise
            },
            ShaderSet = new ShaderSetDescription()
            {
                Shaders = shaders,
                VertexLayouts = new VertexLayoutDescription[] { new VertexLayoutDescription(vertexElements) }
            },
            ResourceLayouts = resourceLayout.Select(x => GraphicsDevice.ResourceFactory.CreateResourceLayout(x)).ToArray()
        });
    }

    public ResourceSet CreateResourceSet(Texture texture)
    {
        return GraphicsDevice.ResourceFactory.CreateResourceSet(new ResourceSetDescription()
        {
            BoundResources = new BindableResource[] { GraphicsDevice.ResourceFactory.CreateTextureView(texture), GraphicsDevice.PointSampler, ConstantBuffer.DeviceBuffer },
            Layout = GraphicsDevice.ResourceFactory.CreateResourceLayout(ResourceLayout)
        });
    }

    private void HandleResize(Vector2D<int> size)
    {
        GraphicsDevice.ResizeMainWindow((uint)size.X, (uint)size.Y);

        MainFrameBuffer = GraphicsDevice.MainSwapchain.Framebuffer;

        // We call render to be more responsive when resizing.. or something like that
        Render();
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!Disposed)
        {
            Window.Dispose();

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

    public struct VertexConstantBuffer
    {
        public Matrix4x4 ViewProjection; // 64 bytes

        public Matrix4x4 World; // 128 bytes
    }
}

public struct Vertex
{
    public Vector3 Position;

    public Vector2 UV;
}
