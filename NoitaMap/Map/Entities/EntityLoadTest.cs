using System.Diagnostics.Contracts;
using System.Numerics;
using System.Security.Cryptography;
using NoitaMap.Map.Components;

namespace NoitaMap.Map.Entities;

// String format:
// int length
// byte[length] text


// Entities format:
// int unknown (version likely? = 2)
// string schema file name
// int entityCount 
// Entity[entityCount] entities

public class EntityLoadTest
{
    public EntityLoadTest()
    {

    }

    public void Load(string path)
    {
        byte[]? decompressedData = path.EndsWith("-2003.bin") ? File.ReadAllBytes("entities/e-2003.bin") : NoitaDecompressor.ReadAndDecompressChunk(path);

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

                Console.WriteLine(
                   $"""
                    --- Entity ---
                    name: {entity.Name}
                    tags: {entity.Tags}
                    lifetime phase: {entity.LifetimePhase}
                    filename: {entity.FileName}
                    position: {entity.Position}
                    rotation: {entity.Rotation}
                    scale: {entity.Scale}
                    components ({entity.Components.Count}):
                    """);

                // + 4 bytes for funny
                reader.BaseStream.Position += 4;
            }
        }

        decompressedData = null;
    }
}
