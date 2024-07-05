using System.Collections.Concurrent;
using System.Numerics;
using NoitaMap.Graphics;
using NoitaMap.Logging;
using Veldrid;

namespace NoitaMap.Map;

public class AreaContainer : IRenderable
{
    private readonly ConcurrentQueue<AreaEntity> ThreadedAreaQueue = new ConcurrentQueue<AreaEntity>();

    public readonly List<AreaEntity> AreaEntities = new List<AreaEntity>();

    public IReadOnlyList<AreaEntitySprite> AreaEntitySprites => AreaSpriteAtlas.AtlasObjects;

    private readonly QuadObjectAtlasBuffer<AreaEntitySprite> AreaSpriteAtlas;

    private bool Disposed;

    public AreaContainer(Renderer renderer)
    {
        AreaSpriteAtlas = new QuadObjectAtlasBuffer<AreaEntitySprite>(renderer);
    }

    public void LoadArea(string path)
    {
        byte[]? decompressedData = NoitaFile.LoadCompressedFile(path);

        using (MemoryStream ms = new MemoryStream(decompressedData))
        {
            using BinaryReader reader = new BinaryReader(ms);

            int numberThatsZeroIThink = reader.ReadBEInt32();

            int numberThatsInPathName = reader.ReadBEInt32();

            int numberThatsOneIthink = reader.ReadBEInt32();

            int numberOfPositions = reader.ReadBEInt32();

            Dictionary<int, List<Vector2>> positionData = new Dictionary<int, List<Vector2>>();

            for (int i = 0; i < numberOfPositions; i++)
            {
                int index = reader.ReadBEInt32();
                float x = reader.ReadBESingle();
                float y = reader.ReadBESingle();

                if (!positionData.TryGetValue(index, out List<Vector2>? positions))
                {
                    positionData.Add(index, positions = new List<Vector2>());
                }

                positions.Add(new Vector2(x, y));
            }

            int numberOfEntityXml = reader.ReadBEInt32();

            for (int i = 0; i < numberOfEntityXml; i++)
            {
                string xmlFilePath = reader.ReadNoitaString() ?? "";
                int bigNumberIdk = reader.ReadBEInt32();
                int smallerNumberMaybeHealthIdk = reader.ReadBEInt32();

                foreach (Vector2 pos in positionData[i])
                {
                    ThreadedAreaQueue.Enqueue(new AreaEntity(xmlFilePath, pos));

                    AreaSpriteAtlas.AddAtlasObject(new AreaEntitySprite(xmlFilePath, pos));
                }
            }
        }

        decompressedData = null;
    }

    public void Update()
    {
        while (ThreadedAreaQueue.TryDequeue(out AreaEntity? area))
        {
            AreaEntities.Add(area);
        }

        AreaSpriteAtlas.Update();
    }

    public void Render(CommandList commandList)
    {
        AreaSpriteAtlas.Draw(commandList);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!Disposed)
        {
            AreaSpriteAtlas.Dispose();

            Disposed = true;
        }
    }

    public void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }
}
