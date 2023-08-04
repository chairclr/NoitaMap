using System.Diagnostics;
using System.Numerics;
using ImGuiNET;
using NoitaMap.Graphics;
using NoitaMap.Map;
using NoitaMap.Map.Entities;
using Veldrid;
using Veldrid.Sdl2;
using Veldrid.StartupUtilities;

namespace NoitaMap.Viewer;

public partial class ViewerDisplay : IDisposable
{
    private readonly Sdl2Window Window;

    public readonly GraphicsDevice GraphicsDevice;

    private readonly CommandList MainCommandList;

    private Framebuffer MainFrameBuffer;

    private readonly Pipeline MainPipeline;

    private readonly ResourceLayout VertexResourceLayout;

    private readonly ResourceLayout PixelSamplerResourceLayout;

    private readonly ResourceLayout PixelTextureResourceLayout;

    private readonly ResourceSet VertexResourceSet;

    private readonly ResourceSet PixelSamplerResourceSet;

    public readonly MaterialProvider MaterialProvider;

    public readonly ConstantBuffer<VertexConstantBuffer> ConstantBuffer;

    private readonly ChunkContainer ChunkContainer;

    private int TotalChunkCount = 0;

    private int LoadedChunks = 0;

    private readonly WorldPixelScenes WorldPixelScenes;

    private readonly EntityContainer Entities;

    private readonly ImGuiRenderer ImGuiRenderer;

    private bool Disposed;

    public ViewerDisplay()
    {
        WindowCreateInfo windowOptions = new WindowCreateInfo()
        {
            X = 50,
            Y = 50,
            WindowWidth = 1280,
            WindowHeight = 720,
            WindowInitialState = WindowState.Normal,
            WindowTitle = "Noita Map Viewer",
        };

        GraphicsDeviceOptions graphicsOptions = new GraphicsDeviceOptions()
        {
#if DEBUG
            Debug = true,
#endif
            SyncToVerticalBlank = true,
            HasMainSwapchain = true,
        };

        VeldridStartup.CreateWindowAndGraphicsDevice(windowOptions, graphicsOptions, out Window, out GraphicsDevice);

        MainCommandList = GraphicsDevice.ResourceFactory.CreateCommandList();

        MainFrameBuffer = GraphicsDevice.MainSwapchain.Framebuffer;

        Shader[] shaders = ShaderLoader.Load(GraphicsDevice, "PixelShader", "VertexShader");

        VertexResourceLayout = GraphicsDevice.ResourceFactory.CreateResourceLayout(new ResourceLayoutDescription()
        {
            Elements = new ResourceLayoutElementDescription[]
            {
                new ResourceLayoutElementDescription("VertexShaderBuffer", ResourceKind.UniformBuffer, ShaderStages.Vertex)
            }
        });

        PixelSamplerResourceLayout = GraphicsDevice.ResourceFactory.CreateResourceLayout(new ResourceLayoutDescription()
        {
            Elements = new ResourceLayoutElementDescription[]
            {
                new ResourceLayoutElementDescription("PointSamplerView", ResourceKind.Sampler, ShaderStages.Fragment)
            }
        });

        PixelTextureResourceLayout = GraphicsDevice.ResourceFactory.CreateResourceLayout(new ResourceLayoutDescription()
        {
            Elements = new ResourceLayoutElementDescription[]
            {
                new ResourceLayoutElementDescription("MainTextureView", ResourceKind.TextureReadOnly, ShaderStages.Fragment)
            }
        });

        MainPipeline = CreatePipeline(shaders);

        MaterialProvider = new MaterialProvider();

        ConstantBuffer = new ConstantBuffer<VertexConstantBuffer>(GraphicsDevice);

        ConstantBuffer.Data.ViewProjection = Matrix4x4.CreateOrthographic(Window.Width, Window.Height, 0f, 1f);

        VertexResourceSet = GraphicsDevice.ResourceFactory.CreateResourceSet(new ResourceSetDescription(VertexResourceLayout, ConstantBuffer.DeviceBuffer));

        PixelSamplerResourceSet = GraphicsDevice.ResourceFactory.CreateResourceSet(new ResourceSetDescription(PixelSamplerResourceLayout, GraphicsDevice.PointSampler));

        ChunkContainer = new ChunkContainer(this);

        WorldPixelScenes = new WorldPixelScenes(this);

        Entities = new EntityContainer(this);

        ImGuiRenderer = new ImGuiRenderer(GraphicsDevice, MainFrameBuffer.OutputDescription, Window.Width, Window.Height);

        // End frame because it starts a frame, which locks my font texture atlas
        ImGui.EndFrame();

        FontAssets.AddImGuiFont();

        ImGuiRenderer.RecreateFontDeviceTexture(GraphicsDevice);

        ImGui.NewFrame();

        Window.Resized += HandleResize;
    }

    public void Start()
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

                    Console.WriteLine($"Error decoding entity at path \"{path}\":");
                    Console.WriteLine(ex.ToString());
                }
            }
        });

        bool exit = false;

        Window.Closing += () => exit = true;

        while (!exit)
        {
            DeltaTimeWatch.Stop();

            float deltaTime = (float)DeltaTimeWatch.Elapsed.TotalSeconds;

            DeltaTimeWatch.Restart();

            InputSnapshot inputSnapshot = Window.PumpEvents();

            ImGuiIOPtr io = ImGui.GetIO();
            foreach (KeyEvent keyEvent in inputSnapshot.KeyEvents)
            {
                io.AddKeyEvent(KeyTranslator.GetKey(keyEvent.Key), keyEvent.Down);
            }

            ImGuiRenderer.Update(deltaTime, inputSnapshot);

            InputSystem.Update(inputSnapshot);

            Update();

            Render();
        }
    }

    private Vector2 MouseTranslateOrigin = Vector2.Zero;

    private Vector2 ViewScale = Vector2.One;

    private Vector2 ViewOffset = Vector2.Zero;

    public Matrix4x4 View =>
            Matrix4x4.CreateTranslation(new Vector3(-ViewOffset, 0f)) *
            Matrix4x4.CreateScale(new Vector3(ViewScale, 1f));

    public Matrix4x4 Projection =>
        Matrix4x4.CreateOrthographic(Window.Width, Window.Height, 0f, 1f);

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

        ImGuiRenderer.Render(GraphicsDevice, MainCommandList);

        StatisticTimer timer = new StatisticTimer("Main Command List").Begin();

        MainCommandList.End();

        GraphicsDevice.SubmitCommands(MainCommandList);

        timer.End(StatisticMode.OncePerFrame);

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
                FrontFace = FrontFace.Clockwise
            },
            ShaderSet = new ShaderSetDescription()
            {
                Shaders = shaders,
                VertexLayouts = new VertexLayoutDescription[]
                {
                    new VertexLayoutDescription
                    (
                        new VertexElementDescription("position", VertexElementFormat.Float3, VertexElementSemantic.TextureCoordinate),
                        new VertexElementDescription("uv", VertexElementFormat.Float2, VertexElementSemantic.TextureCoordinate)
                    ),
                    new VertexLayoutDescription
                    (
                        80, 6,
                        new VertexElementDescription("worldMatrix_0", VertexElementFormat.Float4, VertexElementSemantic.TextureCoordinate),
                        new VertexElementDescription("worldMatrix_1", VertexElementFormat.Float4, VertexElementSemantic.TextureCoordinate),
                        new VertexElementDescription("worldMatrix_2", VertexElementFormat.Float4, VertexElementSemantic.TextureCoordinate),
                        new VertexElementDescription("worldMatrix_3", VertexElementFormat.Float4, VertexElementSemantic.TextureCoordinate),
                        new VertexElementDescription("texPos", VertexElementFormat.Float2, VertexElementSemantic.TextureCoordinate),
                        new VertexElementDescription("texSize", VertexElementFormat.Float2, VertexElementSemantic.TextureCoordinate)
                    ),
                },
            },
            ResourceLayouts = new ResourceLayout[] { VertexResourceLayout, PixelSamplerResourceLayout, PixelTextureResourceLayout }
        });
    }

    public ResourceSet CreateTextureBinding(Texture texture)
    {
        return GraphicsDevice.ResourceFactory.CreateResourceSet(new ResourceSetDescription()
        {
            BoundResources = new BindableResource[] { GraphicsDevice.ResourceFactory.CreateTextureView(texture) },
            Layout = PixelTextureResourceLayout
        });
    }

    private void HandleResize()
    {
        GraphicsDevice.ResizeMainWindow((uint)Window.Width, (uint)Window.Height);

        MainFrameBuffer = GraphicsDevice.MainSwapchain.Framebuffer;

        ImGuiRenderer.WindowResized(Window.Width, Window.Height);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!Disposed)
        {
            //Window.Dispose();

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
}
