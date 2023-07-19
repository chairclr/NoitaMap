using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using NoitaMap.Game.Materials;
using osu.Framework.Allocation;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Screens;
using osuTK;

namespace NoitaMap.Game.Map;

public partial class ChunkContainer : CompositeDrawable
{
    private static readonly Regex ChunkPositionRegex = new Regex("world_(?<x>-?\\d+)_(?<y>-?\\d+)\\.png_petri", RegexOptions.Compiled);

    [Resolved]
    private MaterialProvider MaterialProvider { get; set; }

    protected Dictionary<Vector2, Chunk> InternalChunks = new Dictionary<Vector2, Chunk>();

    public IReadOnlyDictionary<Vector2, Chunk> Chunks => InternalChunks;

    public List<PhysicsObject> PhysicsObjects = new List<PhysicsObject>();

    public ConcurrentQueue<Chunk> FinishedChunks = new ConcurrentQueue<Chunk>();

    private Vector2 ViewOffsetBacking = Vector2.Zero;

    public Vector2 ViewOffset
    {
        get => ViewOffsetBacking;

        set
        {
            ViewOffsetBacking = value;

            Invalidate(Invalidation.DrawInfo);
        }
    }

    private Vector2 ViewScaleBacking = Vector2.One;

    public Vector2 ViewScale
    {
        get => ViewScaleBacking;

        set
        {
            ViewScaleBacking = value;

            Invalidate(Invalidation.DrawInfo);
        }
    }

    //public Matrix3 ViewMatrix
    //{
    //    get
    //    {
    //        Matrix3 transformation = Matrix3.Identity;
    //        MatrixExtensions.TranslateFromLeft(ref transformation, ViewOffset);
    //        return transformation * Matrix3.CreateScale(ViewScale.X, ViewScale.Y, 1f);
    //    }
    //}

    //public override DrawInfo DrawInfo => new DrawInfo(ViewMatrix, ViewMatrix.Inverted());

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

    public void AddChunk(Chunk chunk)
    {
        InternalChunks.Add(chunk.Position, chunk);

        AddInternal(chunk);
    }

    protected override void Update()
    {
        base.Update();

        while (FinishedChunks.TryDequeue(out Chunk? chunk))
        {
            AddChunk(chunk);
            //ChunkContainer.Chunks.Add(chunk.ChunkPosition, chunk);

            //if (chunk.PhysicsObjects is not null)
            //{
            //    ChunkContainer.PhysicsObjects.AddRange(chunk.PhysicsObjects);
            //}
        }

        Position = ViewOffset;
        Scale = ViewScale;
    }

    private static Vector2 GetChunkPositionFromPath(string filePath)
    {
        string fileName = Path.GetFileName(filePath);

        Match match = ChunkPositionRegex.Match(fileName);

        return new Vector2(int.Parse(match.Groups["x"].Value), int.Parse(match.Groups["y"].Value));
    }
}
