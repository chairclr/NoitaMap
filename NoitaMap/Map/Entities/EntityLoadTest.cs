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
                string name = reader.ReadNoitaString()!;

                byte lifetimePhase = reader.ReadByte();

                string fileName = reader.ReadNoitaString()!;

                string tags = reader.ReadNoitaString()!;

                float x = reader.ReadBESingle();
                float y = reader.ReadBESingle();

                float scalex = reader.ReadBESingle();
                float scaley = reader.ReadBESingle();

                float rotation = reader.ReadBESingle();

                int componentCount = reader.ReadBEInt32();

                Console.WriteLine(
                   $"""
                    --- Entity ---
                    name: {name}
                    tags: {tags}
                    lifetime phase: {lifetimePhase}
                    filename: {fileName}
                    position: {x}, {y}
                    rotation: {rotation}
                    scale: {scalex}, {scaley}
                    components ({componentCount}):
                    """);

                for (int j = 0; j < componentCount; j++)
                {
                    string componentName = reader.ReadNoitaString()!;

                    if (componentName is null)
                    {
                        throw new Exception();
                    }

                    byte otherByte = reader.ReadByte();

                    bool enabled = reader.ReadBoolean();

                    string? componentTags = reader.ReadNoitaString();

                    //Console.WriteLine(
                    //    $"""
                    //         --- {componentName} ---
                    //         other byte: {otherByte}
                    //         enabled: {enabled}
                    //         tags: {componentTags}
                    //     """);

                    foreach (ComponentVar var in schema.Vars[componentName])
                    {
                        string type = var.Type;

                        if (type.StartsWith("enum "))
                        {
                            type = "<>enum_type";
                        }

                        if (type.StartsWith("class ceng::math::CVector2"))
                        {
                            type = "<>ceng::math::CVector";
                        }

                        if (type.StartsWith("struct LensValue"))
                        {
                            type = "<>LensValue";
                        }

                        Console.WriteLine($"{reader.BaseStream.Position}: {var.Type} {var.Name}");

                        switch (type)
                        {
                            case "class PixelSprite *":
                                {
                                    int len = reader.ReadBEInt32();
                                    reader.BaseStream.Position += len;

                                    // 20 ??? bytes
                                    reader.BaseStream.Position += 20;

                                    // anchor_x
                                    reader.BaseStream.Position += 4;
                                    // anchor_y
                                    reader.BaseStream.Position += 4;
                                    // 5 ??? bytes
                                    reader.BaseStream.Position += 5;

                                    len = reader.ReadBEInt32();
                                    reader.BaseStream.Position += len;

                                    // 1 ??? byte
                                    reader.BaseStream.Position += 1;
                                }
                                break;
                            case "struct UintArrayInline":
                                {
                                    int len = reader.ReadBEInt32();
                                    reader.BaseStream.Position += len * sizeof(uint);
                                }
                                break;
                            case "class ConfigGun":
                                {
                                    // actions_per_round
                                    reader.BaseStream.Position += 4;
                                    // shuffle_deck_when_empty
                                    reader.BaseStream.Position += 1;
                                    // reload_time
                                    reader.BaseStream.Position += 4;
                                    // deck_capacity
                                    reader.BaseStream.Position += 4;
                                }
                                break;
                            case "class ConfigGunActionInfo":
                                {
                                    // action_id
                                    int len = reader.ReadBEInt32();
                                    reader.BaseStream.Position += len;
                                    // action_name
                                    len = reader.ReadBEInt32();
                                    reader.BaseStream.Position += len;
                                    // action_description
                                    len = reader.ReadBEInt32();
                                    reader.BaseStream.Position += len;
                                    // action_sprite_filename
                                    len = reader.ReadBEInt32();
                                    reader.BaseStream.Position += len;
                                    // action_unidentified_sprite_filename
                                    len = reader.ReadBEInt32();
                                    reader.BaseStream.Position += len;
                                    // action_type
                                    reader.BaseStream.Position += 4;
                                    // action_spawn_level
                                    len = reader.ReadBEInt32();
                                    reader.BaseStream.Position += len;
                                    // action_spawn_probability
                                    len = reader.ReadBEInt32();
                                    reader.BaseStream.Position += len;
                                    // action_spawn_requires_flag
                                    len = reader.ReadBEInt32();
                                    reader.BaseStream.Position += len;
                                    // action_spawn_manual_unlock
                                    reader.BaseStream.Position += 1;

                                    reader.BaseStream.Position += 4;
                                    len = reader.ReadBEInt32();
                                    reader.BaseStream.Position += len;
                                    reader.BaseStream.Position += 4;
                                    reader.BaseStream.Position += 1;
                                    reader.BaseStream.Position += 4;
                                    reader.BaseStream.Position += 1;
                                    reader.BaseStream.Position += 1;
                                    reader.BaseStream.Position += 1;
                                    reader.BaseStream.Position += 4;
                                    reader.BaseStream.Position += 1;
                                    reader.BaseStream.Position += 1;
                                    reader.BaseStream.Position += 4;
                                    reader.BaseStream.Position += 4;
                                    reader.BaseStream.Position += 4;
                                    reader.BaseStream.Position += 4;
                                    reader.BaseStream.Position += 4;
                                    reader.BaseStream.Position += 4;
                                    reader.BaseStream.Position += 4;
                                    reader.BaseStream.Position += 4;
                                    reader.BaseStream.Position += 4;
                                    reader.BaseStream.Position += 4;
                                    reader.BaseStream.Position += 4;
                                    reader.BaseStream.Position += 4;
                                    reader.BaseStream.Position += 4;
                                    reader.BaseStream.Position += 4;
                                    reader.BaseStream.Position += 4;
                                    reader.BaseStream.Position += 4;
                                    reader.BaseStream.Position += 4;
                                    reader.BaseStream.Position += 4;
                                    reader.BaseStream.Position += 4;
                                    reader.BaseStream.Position += 4;
                                    reader.BaseStream.Position += 4;
                                    reader.BaseStream.Position += 4;
                                    reader.BaseStream.Position += 4;
                                    reader.BaseStream.Position += 4;
                                    reader.BaseStream.Position += 4;
                                    reader.BaseStream.Position += 4;
                                    len = reader.ReadBEInt32();
                                    reader.BaseStream.Position += len;
                                    reader.BaseStream.Position += 4;
                                    len = reader.ReadBEInt32();
                                    reader.BaseStream.Position += len;
                                    reader.BaseStream.Position += 4;
                                    reader.BaseStream.Position += 4;
                                    reader.BaseStream.Position += 4;
                                    reader.BaseStream.Position += 4;
                                    reader.BaseStream.Position += 4;
                                    reader.BaseStream.Position += 4;
                                    reader.BaseStream.Position += 4;
                                    reader.BaseStream.Position += 1;
                                    reader.BaseStream.Position += 4;
                                    reader.BaseStream.Position += 4;
                                    len = reader.ReadBEInt32();
                                    reader.BaseStream.Position += len;
                                    len = reader.ReadBEInt32();
                                    reader.BaseStream.Position += len;
                                    len = reader.ReadBEInt32();
                                    reader.BaseStream.Position += len;
                                    len = reader.ReadBEInt32();
                                    reader.BaseStream.Position += len;
                                    len = reader.ReadBEInt32();
                                    reader.BaseStream.Position += len;
                                }
                                break;
                            case "class ConfigExplosion":
                                {
                                    reader.BaseStream.Position += 1;
                                    reader.BaseStream.Position += 4;
                                    int len = reader.ReadBEInt32();
                                    reader.BaseStream.Position += len;
                                    reader.BaseStream.Position += 1;
                                    reader.BaseStream.Position += 1;
                                    reader.BaseStream.Position += 1;
                                    reader.BaseStream.Position += 4;
                                    reader.BaseStream.Position += 4;
                                    // physics_explosion_power
                                    reader.BaseStream.Position += 8;
                                    reader.BaseStream.Position += 4;
                                    reader.BaseStream.Position += 1;
                                    len = reader.ReadBEInt32();
                                    reader.BaseStream.Position += len;
                                    reader.BaseStream.Position += 1;
                                    reader.BaseStream.Position += 4;
                                    reader.BaseStream.Position += 4;
                                    reader.BaseStream.Position += 4;
                                    reader.BaseStream.Position += 4;
                                    reader.BaseStream.Position += 4;
                                    reader.BaseStream.Position += 1;
                                    reader.BaseStream.Position += 1;
                                    reader.BaseStream.Position += 4;
                                    reader.BaseStream.Position += 4;
                                    reader.BaseStream.Position += 4;
                                    reader.BaseStream.Position += 4;
                                    reader.BaseStream.Position += 1;
                                    reader.BaseStream.Position += 1;
                                    len = reader.ReadBEInt32();
                                    reader.BaseStream.Position += len;
                                    reader.BaseStream.Position += 4;
                                    reader.BaseStream.Position += 4;
                                    reader.BaseStream.Position += 4;
                                    reader.BaseStream.Position += 4;
                                    reader.BaseStream.Position += 1;
                                    reader.BaseStream.Position += 1;
                                    reader.BaseStream.Position += 1;
                                    reader.BaseStream.Position += 1;
                                    reader.BaseStream.Position += 4;
                                    reader.BaseStream.Position += 4;
                                    reader.BaseStream.Position += 4;
                                    reader.BaseStream.Position += 1;
                                    reader.BaseStream.Position += 4;
                                    reader.BaseStream.Position += 4;
                                    reader.BaseStream.Position += 4;
                                    reader.BaseStream.Position += 4;
                                    reader.BaseStream.Position += 4;
                                    reader.BaseStream.Position += 1;
                                    reader.BaseStream.Position += 1;
                                    reader.BaseStream.Position += 1;
                                    reader.BaseStream.Position += 4;
                                    reader.BaseStream.Position += 4;
                                    reader.BaseStream.Position += 4;
                                    reader.BaseStream.Position += 4;
                                    reader.BaseStream.Position += 4;
                                    reader.BaseStream.Position += 4;
                                    reader.BaseStream.Position += 4;
                                    reader.BaseStream.Position += 4;
                                    reader.BaseStream.Position += 1;
                                    reader.BaseStream.Position += 1;
                                    reader.BaseStream.Position += 1;
                                    len = reader.ReadBEInt32();
                                    reader.BaseStream.Position += len;
                                    reader.BaseStream.Position += 4;
                                    reader.BaseStream.Position += 4;

                                    //ConfigDamageCritical
                                    //{
                                    //    reader.BaseStream.Position += 4;
                                    //    reader.BaseStream.Position += 4;
                                    //    reader.BaseStream.Position += 1;
                                    //}

                                    //// delay
                                    //reader.BaseStream.Position += 8;

                                    //reader.BaseStream.Position += 4;
                                    //reader.BaseStream.Position += 4;
                                    //reader.BaseStream.Position += 4;
                                    //reader.BaseStream.Position += 8;
                                    //reader.BaseStream.Position += 4;
                                }
                                break;
                            case "class ConfigDamagesByType":
                                {
                                    reader.BaseStream.Position += 60;
                                }
                                break;
                            case "unsigned int":
                            case "int":
                            case "unsigned long":
                            case "long long":
                            case "long":
                            case "float":
                            case "double":
                            case "bool":
                            case "struct ValueRange":
                            case "<>ceng::math::CVector":
                            case "<>enum_type":
                            case "<>LensValue":
                                reader.BaseStream.Position += var.Size;
                                break;
                            case "class std::basic_string<char,struct std::char_traits<char>,class std::allocator<char> >":
                                {
                                    int length = reader.ReadBEInt32();
                                    reader.BaseStream.Position += length;
                                }
                                break;
                            default:
                                throw new NotImplementedException($"??? type at {reader.BaseStream.Position} {componentName}.{var.Type} {var.Name}");
                        }
                    }
                }

                break;
            }
        }

        decompressedData = null;
        //Directory.CreateDirectory("entities");
        //File.WriteAllBytes(Path.Combine("entities", Path.GetFileName(path)), NoitaDecompressor.ReadAndDecompressChunk(path));
    }
}
