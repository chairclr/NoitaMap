using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using NoitaMap.Graphics;
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

public class EntityContainer : IRenderable
{
    private readonly ConcurrentQueue<Entity> ThreadedEntityQueue = new ConcurrentQueue<Entity>();

    public readonly List<Entity> Entities = new List<Entity>();

    private readonly QuadObjectAtlasBuffer<PixelSpriteComponent> PixelSpriteAtlas;

    private readonly QuadObjectAtlasBuffer<SpriteComponent> RegularSpriteAtlas;

    public IReadOnlyList<PixelSpriteComponent> PixelSprites => PixelSpriteAtlas.AtlasObjects;

    public IReadOnlyList<SpriteComponent> RegularSprites => RegularSpriteAtlas.AtlasObjects;

    private bool Disposed;

    public EntityContainer(Renderer renderer)
    {
        PixelSpriteAtlas = new QuadObjectAtlasBuffer<PixelSpriteComponent>(renderer);

        RegularSpriteAtlas = new QuadObjectAtlasBuffer<SpriteComponent>(renderer);
    }

    public void LoadEntities(string path)
    {
        byte[]? decompressedData = NoitaFile.LoadCompressedFile(path);

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

            while (ms.Position < ms.Length)
            {
                Entity entity = new Entity(schema);

                entity.Deserialize(reader);

                foreach (Component component in entity.Components)
                {
                    if (component is PixelSpriteComponent pixelSprite)
                    {
                        PixelSpriteAtlas.AddAtlasObject(pixelSprite);
                    }

                    if (component is SpriteComponent sprite)
                    {
                        if (!sprite.Tags.Contains("item_unidentified"))
                        {
                            RegularSpriteAtlas.AddAtlasObject(sprite);
                        }
                    }
                }

                ThreadedEntityQueue.Enqueue(entity);

                int thoseFourBytes = reader.ReadBEInt32();
            }

            if (ms.Position != ms.Length)
            {
                Logger.LogWarning($"Failed to fully read {path}");
                throw new Exception();
            }
        }

        decompressedData = null;

    }

    public void Update()
    {
        while (ThreadedEntityQueue.TryDequeue(out Entity? entity))
        {
            Entities.Add(entity);
        }

        PixelSpriteAtlas.Update();

        RegularSpriteAtlas.Update();
    }

    public void Render(CommandList commandList)
    {
        PixelSpriteAtlas.Draw(commandList);

        RegularSpriteAtlas.Draw(commandList);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!Disposed)
        {
            PixelSpriteAtlas.Dispose();

            RegularSpriteAtlas.Dispose();

            Disposed = true;
        }
    }

    public void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }
}
