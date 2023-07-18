using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using NoitaMap.Game.Graphics;
using NoitaMap.Game.Materials;
using osu.Framework.Allocation;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Primitives;
using osu.Framework.Graphics.Shaders;
using osuTK;

namespace NoitaMap.Game.Map;

public partial class ChunkContainer : Drawable, ITexturedShaderDrawable
{
    private static readonly Regex ChunkPositionRegex = new Regex("world_(?<x>-?\\d+)_(?<y>-?\\d+)\\.png_petri", RegexOptions.Compiled);

    public IShader TextureShader { get; protected set; }

    [Resolved]
    private MaterialProvider MaterialProvider { get; set; }

    public Dictionary<Vector2, Chunk> Chunks = new Dictionary<Vector2, Chunk>();

    public ConcurrentQueue<Chunk> FinishedChunks = new ConcurrentQueue<Chunk>();

    public Vector2 ViewOffset = Vector2.Zero;

    public Vector2 ViewScale = Vector2.One;

    public Matrix4 ViewMatrix => Matrix4.CreateTranslation(-ViewOffset.X, -ViewOffset.Y, 0f) * Matrix4.CreateScale(ViewScale.X, ViewScale.Y, 1f);

    [BackgroundDependencyLoader]
    private void Load(ShaderManager shaders)
    {
        TextureShader = shaders.Load("ChunkView", "ChunkView");
    }

    public void LoadChunk(string chunkFilePath)
    {
        Vector2 chunkPosition = GetChunkPositionFromPath(chunkFilePath);

        Chunk chunk = new Chunk(chunkPosition, MaterialProvider!);

        byte[]? decompressedData = NoitaDecompressor.ReadAndDecompressChunk(chunkFilePath);

        using (MemoryStream ms = new MemoryStream(decompressedData))
        {
            using BinaryReader reader = new BinaryReader(ms);

            int version = reader.ReadBEInt32();
            int width = reader.ReadBEInt32();
            int height = reader.ReadBEInt32();

            if (version != 24 || width != Chunk.ChunkWidth || height != Chunk.ChunkHeight)
            {
                throw new InvalidDataException($"Chunk header was not correct. Version = {version} Width = {width} Height = {height}");
            }

            chunk.Deserialize(reader);
        }

        decompressedData = null;

        FinishedChunks.Enqueue(chunk);
    }

    protected override DrawNode CreateDrawNode()
    {
        return new ChunkContainerDrawNode(this);
    }

    private static Vector2 GetChunkPositionFromPath(string filePath)
    {
        string fileName = Path.GetFileName(filePath);

        Match match = ChunkPositionRegex.Match(fileName);

        return new Vector2(int.Parse(match.Groups["x"].Value), int.Parse(match.Groups["y"].Value));
    }
}
