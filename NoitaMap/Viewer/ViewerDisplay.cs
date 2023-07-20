using System.Collections.Concurrent;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using NoitaMap.Graphics;
using NoitaMap.Map;
using Silk.NET.Maths;
using Silk.NET.Windowing;
using Silk.NET.Windowing.Extensions.Veldrid;
using Veldrid;
using Veldrid.SPIRV;
using Vortice.Direct3D11;

namespace NoitaMap.Viewer;

public class ViewerDisplay : IDisposable
{
    private readonly IWindow Window;

    public readonly GraphicsDevice GraphicsDevice;

    private readonly CommandList MainCommandList;

    private Framebuffer MainFrameBuffer;

    private Pipeline MainPipeline;

    private readonly ChunkContainer ChunkContainer;

    public readonly ConstantBuffer<VertexConstantBuffer> ConstantBuffer;

    private readonly StagingResourcePool StagingResourcePool;

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
            Size = new Vector2D<int>(1280, 720)
        };

        GraphicsDeviceOptions graphicsOptions = new GraphicsDeviceOptions()
        {
#if DEBUG
            Debug = true,
#endif
            SyncToVerticalBlank = true,
            HasMainSwapchain = true
        };

        VeldridWindow.CreateWindowAndGraphicsDevice(windowOptions, graphicsOptions, out Window, out GraphicsDevice);

        MainCommandList = GraphicsDevice.ResourceFactory.CreateCommandList();

        MainFrameBuffer = GraphicsDevice.MainSwapchain.Framebuffer;

        StagingResourcePool = new StagingResourcePool(GraphicsDevice);

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

        Window.Render += x => Render();

        Window.Resize += HandleResize;
    }

    public void Start()
    {
        Task.Run(() =>
        {
            string[] chunkPaths = Directory.EnumerateFiles(WorldPath, "world_*_*.png_petri").ToArray();

            Parallel.ForEach(chunkPaths, chunkPath =>
            {
                ChunkContainer.LoadChunk(chunkPath);
            });
        });

        Window.Run();
    }

    private void Render()
    {
        ConstantBuffer.Data.ViewProjection = Matrix4x4.CreateOrthographic(Window.Size.X, Window.Size.Y, 0f, 1f);

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
            MainCommandList.Dispose();

            GraphicsDevice.Dispose();

            Window.Dispose();

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
        public Matrix4x4 ViewProjection;
        public Matrix4x4 World;
    }
}

public struct Vertex
{
    public Vector3 Position;

    public Vector2 UV;
}
