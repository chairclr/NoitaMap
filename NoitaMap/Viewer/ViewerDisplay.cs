using System.Diagnostics;
using System.Numerics;
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

    public Renderer Renderer;

    private ChunkContainer ChunkContainer;
    
    private WorldPixelScenes WorldPixelScenes;

    private EntityContainer EntityContainer;

    private int TotalChunkCount = 0;

    private int LoadedChunks = 0;

    private bool Disposed;

#pragma warning disable CS8618
    public ViewerDisplay()
#pragma warning restore CS8618
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

        Window.Render += (double d) => 
        {
            DeltaTimeWatch.Stop();

            float deltaTime = (float)DeltaTimeWatch.Elapsed.TotalSeconds;

            DeltaTimeWatch.Restart();

            InputSystem.Update(Window);

            Renderer!.ImGuiRenderer.BeginFrame(deltaTime);

            Renderer!.Update();

            DrawUI();

            Renderer!.Render();
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

        VeldridWindow.CreateGraphicsDevice(Window, graphicsOptions, out GraphicsDevice);

        Renderer = new Renderer(Window, GraphicsDevice);

        IRenderable[] renderables =
        [
            WorldPixelScenes = new WorldPixelScenes(Renderer),
            EntityContainer = new EntityContainer(Renderer),
            ChunkContainer = new ChunkContainer(Renderer),
            Renderer.ImGuiRenderer
        ];

        Renderer.Renderables.AddRange<IRenderable>(renderables);

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
                    EntityContainer.LoadEntities(path);
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

    public Stopwatch DeltaTimeWatch = Stopwatch.StartNew();

    protected virtual void Dispose(bool disposing)
    {
        if (!Disposed)
        {
            GraphicsDevice.WaitForIdle();

            Renderer.Dispose();

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
