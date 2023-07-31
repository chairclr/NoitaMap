using System.Data;
using System.Diagnostics;
using System.Numerics;
using System.Runtime.InteropServices;
using ImGuiNET;
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

    public readonly MaterialProvider MaterialProvider;

    public readonly ConstantBuffer<VertexConstantBuffer> ConstantBuffer;

    private ResourceLayoutDescription ResourceLayout;

    private readonly ChunkContainer ChunkContainer;

    private int TotalChunkCount = 0;

    private int LoadedChunks = 0;

    private readonly WorldPixelScenes WorldPixelScenes;

    private readonly ImGuiRenderer ImGuiRenderer;

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

        WorldPixelScenes = new WorldPixelScenes(this);

        ImGuiRenderer = new ImGuiRenderer(GraphicsDevice, MainFrameBuffer.OutputDescription, Window.Size.X, Window.Size.Y);

        // End frame because it starts a frame, which locks my font texture atlas
        ImGui.EndFrame();

        FontAssets.AddImGuiFont();

        ImGuiRenderer.RecreateFontDeviceTexture(GraphicsDevice);

        ImGui.NewFrame();

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
            string[] chunkPaths = Directory.EnumerateFiles(WorldPath, "world_*_*.png_petri").ToArray();

            TotalChunkCount = chunkPaths.Length;

            int ChunksPerThread = (int)MathF.Ceiling((float)chunkPaths.Length / (float)(Environment.ProcessorCount - 2));

            // Split up all of the paths into a collection of (at most) ChunksPerThread paths for each thread to process
            // This is so that each thread can process ChunksPerThread chunks at once, rather than having too many threads
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

                    LoadedChunks++;
                }
            });
        });

        Task.Run(() =>
        {
            string path = Path.Combine(WorldPath, "world_pixel_scenes.bin");

            if (File.Exists(path))
            {
                StatisticTimer timer = new StatisticTimer("Load World Pixel Scenes").Begin();

                WorldPixelScenes.Load(path);

                timer.End(StatisticMode.Single);
            }
        });

        Window.Run();
    }

    private Vector2 MouseTranslateOrigin = Vector2.Zero;

    private Vector2 ViewScale = Vector2.One;

    private Vector2 ViewOffset = Vector2.Zero;

    public Matrix4x4 View =>
            Matrix4x4.CreateTranslation(new Vector3(-ViewOffset, 0f)) *
            Matrix4x4.CreateScale(new Vector3(ViewScale, 1f));

    public Matrix4x4 Projection =>
        Matrix4x4.CreateOrthographic(Window.Size.X, Window.Size.Y, 0f, 1f);

    public Stopwatch DeltaTimeWatch = Stopwatch.StartNew();

    private void Update()
    {
        DeltaTimeWatch.Stop();

        float deltaTime = (float)DeltaTimeWatch.Elapsed.TotalSeconds;

        DeltaTimeWatch.Restart();

        ImGuiRenderer.Update(deltaTime, InputSystem.GetInputSnapshot());

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

        ChunkContainer.Update();

        WorldPixelScenes.Update();

    }

    private Vector2 ScalePosition(Vector2 position)
    {
        return position / ViewScale;
    }

    private void Render()
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

        WorldPixelScenes.Draw(MainCommandList);

        ChunkContainer.Draw(MainCommandList);

        DrawUI();

        ImGuiRenderer.Render(GraphicsDevice, MainCommandList);

        StatisticTimer timer = new StatisticTimer("Main Command List").Begin();

        MainCommandList.End();

        GraphicsDevice.SubmitCommands(MainCommandList);

        timer.End(StatisticMode.OncePerFrame);
        
        GraphicsDevice.SwapBuffers();
    }

    private bool ShowMetrics = true;

    private void DrawUI()
    {
        ImGui.SetNextWindowPos(Vector2.Zero);
        ImGui.Begin("##StatusWindow", ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoBackground | ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.NoMouseInputs | ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoSavedSettings | ImGuiWindowFlags.NoScrollWithMouse | ImGuiWindowFlags.AlwaysAutoResize);

        ImGui.TextUnformatted($"Framerate:     {ImGui.GetIO().Framerate:F1}");

        ImGui.TextUnformatted($"Chunks Loaded: {LoadedChunks} / {TotalChunkCount}");

        if (ImGui.IsKeyPressed(ImGuiKey.F11))
        {
            ShowMetrics = !ShowMetrics;
        }

        if (ShowMetrics)
        {
            ImGui.TextUnformatted($"---- Metrics ----");
            foreach ((string name, Func<string> format) in Statistics.Metrics)
            {
                ImGui.TextUnformatted($"{name + ":",-20} {format()}");
            }

#if TIME_STATS
            ImGui.TextUnformatted($"---- Per Frame Times ----");
            foreach ((string name, TimeSpan time) in Statistics.OncePerFrameTimeStats)
            {
                ImGui.TextUnformatted($"{name + ":",-20} {time.TotalSeconds:F5}s");
            }

            ImGui.TextUnformatted($"---- Summed Times ----");
            foreach ((string name, TimeSpan time) in Statistics.SummedTimeStats)
            {
                ImGui.TextUnformatted($"{name + ":",-20} {time.TotalSeconds:F5}s");
            }

            ImGui.TextUnformatted($"---- Single Times ----");
            foreach ((string name, TimeSpan time) in Statistics.SingleTimeStats)
            {
                ImGui.TextUnformatted($"{name + ":",-20} {time.TotalSeconds:F5}s");
            }
#endif
        }

        ImGui.End();
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
                VertexLayouts = new VertexLayoutDescription[]
                {
                    new VertexLayoutDescription(vertexElements[..2]),
                    new VertexLayoutDescription(vertexElements[2..]) with
                        {
                            InstanceStepRate = 6
                        },

                }
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

        ImGuiRenderer.WindowResized(size.X,  size.Y);

        // We call render to be more responsive when resizing.. or something like that
        Update();
        Render();
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!Disposed)
        {
            Window.Dispose();

            ImGuiRenderer.Dispose();

            MainFrameBuffer.Dispose();

            ConstantBuffer.Dispose();

            MainPipeline.Dispose();

            ChunkContainer.Dispose();

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
    }
}

public struct Vertex
{
    public Vector3 Position;

    public Vector2 UV;
}

public struct VertexInstance
{
    public Matrix4x4 Transform;

    public Vector2 TexturePosition;

    public Vector2 TextureSize;
}
