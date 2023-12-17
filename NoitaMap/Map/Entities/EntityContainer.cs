using System.Collections.Concurrent;
using NoitaMap.Graphics.Atlases;
using NoitaMap.Logging;
using NoitaMap.Map.Components;
using NoitaMap.Viewer;
using Veldrid;

namespace NoitaMap.Map.Entities;

// String format:
// int length
// byte[length] text

// Entities format:
// int unknown (version likely? = 2)
// string schema file name
// int entity count 
// Entity[entity count] entities

// Entity format:
// string name
// byte lifetime phase
// string file name
// string tags
// float x
// float y
// float scale x
// float scale y
// int component count
// Component[component count]

// Component format:
// string name
// byte always = 1
// bool enabled
// string tags
// Fields (see schema files)

public class EntityContainer
{
    private readonly ConcurrentQueue<Entity> ThreadedEntityQueue = new ConcurrentQueue<Entity>();

    private readonly List<Entity> Entities = new List<Entity>();

    private readonly PixelSpriteAtlasBuffer PixelSpriteAtlas;

    public IReadOnlyList<PixelSpriteComponent> PixelSprites => PixelSpriteAtlas.PixelSprites;

    public EntityContainer(ViewerDisplay viewerDisplay)
    {
        PixelSpriteAtlas = new PixelSpriteAtlasBuffer(viewerDisplay);
    }

    public void LoadEntities(string path)
    {
        StatisticTimer timer = new StatisticTimer("Load Entity").Begin();

        byte[]? decompressedData = NoitaDecompressor.ReadAndDecompressChunk(path);

        using (MemoryStream ms = new MemoryStream(decompressedData))
        {
            using BinaryReader reader = new BinaryReader(ms);

            int version = reader.ReadBEInt32();

            if (version != 2)
            {
                throw new Exception($"Version wasn't 2 (it was {version})");
            }

            string schemaFileName = reader.ReadNoitaString()!;

            ComponentSchema schema = ComponentSchema.GetSchema(schemaFileName);

            int entityCount = reader.ReadBEInt32();

            for (int i = 0; i < entityCount; i++)
            {
                Entity entity = new Entity(schema);

                entity.Deserialize(reader);

                foreach (Component component in entity.Components)
                {
                    if (component is PixelSpriteComponent pixelSprite)
                    {
                        PixelSpriteAtlas.AddPixelSprite(pixelSprite);
                    }
                }

                ThreadedEntityQueue.Enqueue(entity);

                // + 4 bytes for funny
                reader.BaseStream.Position += 4;
            }

            if (ms.Position != ms.Length)
            {
                Logger.LogWarning($"Failed to fully read {path}");
            }
        }

        decompressedData = null;

        timer.End(StatisticMode.Sum);
    }

    public void Update()
    {
        while (ThreadedEntityQueue.TryDequeue(out Entity? entity))
        {
            Entities.Add(entity);
        }

        PixelSpriteAtlas.Update();
    }

    public void Draw(CommandList commandList)
    {
        PixelSpriteAtlas.Draw(commandList);
    }
}
