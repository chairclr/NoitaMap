using System.Diagnostics;
using System.Numerics;
using ImGuiNET;
using NoitaMap.Graphics;
using NoitaMap.Logging;
using NoitaMap.Map;
using NoitaMap.Map.Entities;
using NoitaMap.Startup;
using Silk.NET.Maths;
using Silk.NET.Windowing;
using Silk.NET.Windowing.Sdl;
using Veldrid;

namespace NoitaMap.Viewer;

public partial class ViewerDisplay : IDisposable
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

    private ChunkContainer ChunkContainer;

    private int TotalChunkCount = 0;

    private int LoadedChunks = 0;

    private WorldPixelScenes WorldPixelScenes;

    private EntityContainer Entities;

    public ImGuiRenderer ImGuiRenderer;

    private bool Disposed;

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
    public ViewerDisplay()
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
    {
        WindowOptions windowOptions = new WindowOptions()
        {
            Position = new Vector2D<int>(50, 50),
            Size = new Vector2D<int>(1280, 720),
            Title = "Noita Map Viewer",
            API = GraphicsAPI.None,
            IsVisible = true,
            ShouldSwapAutomatically = false
        };

        SdlWindowing.Use();

        Window = Silk.NET.Windowing.Window.Create(windowOptions);

        Window.Load += Load;

        Window.Resize += HandleResize;

        Window.Render += (double d) => 
        {
            DeltaTimeWatch.Stop();

            float deltaTime = (float)DeltaTimeWatch.Elapsed.TotalSeconds;

            DeltaTimeWatch.Restart();

            InputSystem.Update(Window);

            ImGuiRenderer!.BeginFrame(deltaTime);

            Update();

            Render();
        };

        Window.Run();
    }

    public void Load()
    {
        GraphicsDeviceOptions graphicsOptions = new GraphicsDeviceOptions()
        {
#if DEBUG
            Debug = true,
#endif
            SyncToVerticalBlank = true,
            HasMainSwapchain = true
        };

        VeldridWindow.CreateWindowAndGraphicsDevice(Window, graphicsOptions, out GraphicsDevice);

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

        ChunkContainer = new ChunkContainer(this);

        WorldPixelScenes = new WorldPixelScenes(this);

        Entities = new EntityContainer(this);

        ImGuiRenderer = new ImGuiRenderer(GraphicsDevice, MainFrameBuffer.OutputDescription, Window.Size.X, Window.Size.Y);

        StartLoading();
    }

    public void StartLoading()
    {
        Task.Run(() =>
        {
            string[] chunkPaths = Directory.EnumerateFiles(PathService.WorldPath, "world_*_*.png_petri").ToArray();

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
            string path = Path.Combine(PathService.WorldPath, "world_pixel_scenes.bin");

            if (File.Exists(path))
            {
                StatisticTimer timer = new StatisticTimer("Load World Pixel Scenes").Begin();

                WorldPixelScenes.Load(path);

                timer.End(StatisticMode.Single);
            }
        });

        Task.Run(() =>
        {
            string[] entityPaths = Directory.EnumerateFiles(PathService.WorldPath, "entities_*.bin").ToArray();

            foreach (string path in entityPaths)
            {
                StatisticTimer timer = new StatisticTimer("Load Entity").Begin();
                try
                {
                    Entities.LoadEntities(path);
                }
                catch (Exception ex)
                {
#if  DEBUG
                    byte[] decompressed = NoitaDecompressor.ReadAndDecompressChunk(path);

                    Directory.CreateDirectory("entity_error_logs");

                    File.WriteAllBytes($"entity_error_logs/{Path.GetFileNameWithoutExtension(path)}.bin", decompressed);
#endif

                    Logger.LogWarning($"Error decoding entity at path \"{path}\":");
                    Logger.LogWarning(ex);
                }
                timer.End(StatisticMode.Sum);
            }
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

    public Stopwatch DeltaTimeWatch = Stopwatch.StartNew();

    private void Update()
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

        ChunkContainer.Update();

        WorldPixelScenes.Update();

        Entities.Update();
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

        MainCommandList.SetGraphicsResourceSet(0, VertexResourceSet);

        MainCommandList.SetGraphicsResourceSet(1, PixelSamplerResourceSet);

        Entities.Draw(MainCommandList);

        WorldPixelScenes.Draw(MainCommandList);

        ChunkContainer.Draw(MainCommandList);

        DrawUI();

        ImGuiRenderer.EndFrame(MainCommandList);

        MainCommandList.End();

        GraphicsDevice.SubmitCommands(MainCommandList);

        GraphicsDevice.SwapBuffers();
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

        ChunkContainer.HandleResize();

        ImGuiRenderer.HandleResize(size.X, size.Y);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!Disposed)
        {
            GraphicsDevice.WaitForIdle();
            
            ImGuiRenderer.Dispose();

            MainFrameBuffer.Dispose();

            ConstantBuffer.Dispose();

            MainPipeline.Dispose();

            ChunkContainer.Dispose();

            Entities.Dispose();

            WorldPixelScenes.Dispose();

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
