using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Reflection.PortableExecutable;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace NoitaMap;

internal class Viewer : Game
{
    private readonly GraphicsDeviceManager Graphics;

    private readonly string WorldPath;

    private readonly ConcurrentQueue<Chunk> LoadingChunks = new ConcurrentQueue<Chunk>();

    private int LoadedChunks = 0;

    private int TotalChunks = 0;

    private bool LoadedAllChunks = false;

    private readonly Dictionary<Vector2, Chunk> Chunks = new Dictionary<Vector2, Chunk>();

    private readonly List<PhysicsObject> PhysicsObjects = new List<PhysicsObject>();

    private SpriteBatch? ChunkSpriteBatch;

    private SpriteBatch? PhysicsObjectsSpriteBatch;

    private SpriteBatch? FontSpriteBatch;

    private Vector2 ViewOffset = Vector2.Zero;

    private Vector2 ViewScale = Vector2.One;

    private Matrix ViewMatrix => Matrix.CreateTranslation(-ViewOffset.X, -ViewOffset.Y, 0) * Matrix.CreateScale(ViewScale.X, ViewScale.Y, 1f);

    private Vector2 MouseTranslateOrigin = Vector2.Zero;

    [NotNull]
    private SpriteFont? NoitaFont = null;

    public Viewer(string[] args)
    {
        string localLowPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + "Low";

        WorldPath = Path.Combine(localLowPath, "Nolla_Games_Noita\\save00\\world");

        if (args.Length == 1)
        {
            WorldPath = args[0];
        }
        else if (args.Length > 1)
        {
            Console.WriteLine("You may only specify world file path as the first argument.");
            throw new Exception();
        }

        Graphics = new GraphicsDeviceManager(this)
        {
            PreferredBackBufferWidth = 1280,
            PreferredBackBufferHeight = 720,
            GraphicsProfile = GraphicsProfile.HiDef
        };

        Graphics.ApplyChanges();

        Window.AllowUserResizing = true;

        IsMouseVisible = true;
    }

    protected override void LoadContent()
    {
        GraphicsDeviceProvider.GraphicsDevice = Graphics.GraphicsDevice;

        ChunkSpriteBatch = new SpriteBatch(GraphicsDeviceProvider.GraphicsDevice);

        PhysicsObjectsSpriteBatch = new SpriteBatch(GraphicsDeviceProvider.GraphicsDevice);

        NoitaFont = NoitaFontLoader.Load();

        // Init material provider before doing multithreaded work
        MaterialProvider.GetMaterial("");

        Task.Run(() =>
        {
            string[] chunkPaths = Directory.EnumerateFiles(WorldPath, "world_*_*.png_petri").ToArray();

            TotalChunks = chunkPaths.Length;

            Parallel.ForEach(chunkPaths, chunkPath =>
            {
                Chunk chunk = ChunkRenderer.RenderChunk(chunkPath);

                LoadingChunks.Enqueue(chunk);

                Interlocked.Increment(ref LoadedChunks);
            });

            LoadedAllChunks = true;
        });
    }

    protected override void Update(GameTime gameTime)
    {
        InputSystem.Update();

        Vector2 originalScaledMouse = Vector2.Transform(InputSystem.MousePosition, Matrix.Invert(Matrix.CreateScale(ViewScale.X, ViewScale.Y, 1f)));

        ViewScale += InputSystem.ScrollDelta * (ViewScale / 1000f);
        ViewScale = Vector2.Clamp(ViewScale, new Vector2(0.1f, 0.1f), new Vector2(10f, 10f));

        Vector2 currentScaledMouse = Vector2.Transform(InputSystem.MousePosition, Matrix.Invert(Matrix.CreateScale(ViewScale.X, ViewScale.Y, 1f)));

        // Zoom in on where the mouse is
        ViewOffset += originalScaledMouse - currentScaledMouse;

        if (InputSystem.LeftMousePressed)
        {
            MouseTranslateOrigin = Vector2.Transform(InputSystem.MousePosition, Matrix.Invert(Matrix.CreateScale(ViewScale.X, ViewScale.Y, 1f))) + ViewOffset;
        }

        if (InputSystem.LeftMouseDown)
        {
            Vector2 currentMousePosition = Vector2.Transform(InputSystem.MousePosition, Matrix.Invert(Matrix.CreateScale(ViewScale.X, ViewScale.Y, 1f))) + ViewOffset;

            ViewOffset += MouseTranslateOrigin - currentMousePosition;
        }
    }

    protected override void Draw(GameTime gameTime)
    {
        ChunkSpriteBatch ??= new SpriteBatch(GraphicsDeviceProvider.GraphicsDevice);
        PhysicsObjectsSpriteBatch ??= new SpriteBatch(GraphicsDeviceProvider.GraphicsDevice);
        FontSpriteBatch ??= new SpriteBatch(GraphicsDeviceProvider.GraphicsDevice);

        GraphicsDeviceProvider.GraphicsDevice.Clear(Color.LightPink);

        while (LoadingChunks.TryDequeue(out Chunk? chunk))
        {
            Chunks.Add(chunk.Position, chunk);
            PhysicsObjects.AddRange(chunk.PhysicsObjects);
        }

        ChunkSpriteBatch.Begin(samplerState: SamplerState.PointClamp, transformMatrix: ViewMatrix);

        foreach (Chunk chunk in Chunks.Values)
        {
            ChunkSpriteBatch.Draw(chunk.Texture, chunk.Position, Color.White);
        }

        ChunkSpriteBatch.End();

        PhysicsObjectsSpriteBatch.Begin(samplerState: SamplerState.PointClamp, transformMatrix: ViewMatrix);

        foreach (PhysicsObject physicsObject in PhysicsObjects)
        {
            PhysicsObjectsSpriteBatch.Draw(physicsObject.Texture, physicsObject.Position, null, Color.White, physicsObject.Rotation, Vector2.Zero, 1f, SpriteEffects.None, 0f);
        }

        PhysicsObjectsSpriteBatch.End();

        FontSpriteBatch.Begin(samplerState: SamplerState.PointClamp);

        if (!LoadedAllChunks)
        {
            DrawText($"{LoadedChunks} / {TotalChunks} Chunks Loaded ({MathF.Floor((float)LoadedChunks / TotalChunks * 100f)}%)", new Vector2(10f, 10f));
        }

        FontSpriteBatch.End();
    }

    private void DrawText(string text, Vector2 position, float scale = 3f)
    {
        FontSpriteBatch?.DrawString(NoitaFont, text, position, Color.White, 0f, Vector2.Zero, 3f, SpriteEffects.None, 0f);
    }

    private Chunk? GetChunkContaining(Vector2 position)
    {
        Chunks.TryGetValue(Vector2.Floor(position / 512), out Chunk? chunk);

        return chunk;
    }
}
