﻿using NoitaMap.Entities;
using NoitaMap.Logging;
using NoitaMap.Schemas;

namespace NoitaMap.Components;

public class DummyComponent : Component
{
    private readonly ComponentSchema Schema;

    public DummyComponent(Entity entity, string name, ComponentSchema schema)
        : base(entity, name)
    {
        Schema = schema;
    }

    public override void Deserialize(BinaryReader reader)
    {
        base.Deserialize(reader);

        void ProcessField(string typeName, string name, int size)
        {
            string type = typeName;

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

            switch (type)
            {
                case "class PixelSprite *":
                    {
                        int len = reader.ReadBEInt32();
                        reader.BaseStream.Position += len;

                        //x?
                        reader.BaseStream.Position += 4;
                        //y?
                        reader.BaseStream.Position += 4;
                        //scalex?
                        reader.BaseStream.Position += 4;
                        //scaley?
                        reader.BaseStream.Position += 4;

                        len = reader.ReadBEInt32();
                        reader.BaseStream.Position += len;

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
                case "class ConfigGunActionInfo":
                case "class ConfigExplosion":
                    {
                        if (Schema.Name != "c8ecfb341d22516067569b04563bff9c")
                        {
                            throw new NotImplementedException($"No ObjectSchema file for {Schema.Name}");
                        }

                        ObjectSchema objectSchema = ObjectSchema.GetSchema(Schema.Name, type);

                        foreach (ObjectSchema.ObjectSchemaField field in objectSchema.SchemaFields)
                        {
                            if (field.Kind == "Privates")
                                continue;

                            ProcessField(field.RawType, field.Name, (int)field.Size!);
                        }
                    }
                    break;
                case "struct SpriteStains *":
                    {
                        reader.BaseStream.Position += 6;

                        foreach (ComponentVar var in Schema.Vars[reader.ReadNoitaString()!])
                        {
                            ProcessField(var.Type, var.Name, var.Size);
                        }
                    }
                    break;
                case "class ConfigDamagesByType":
                    // -4 for fun
                    reader.BaseStream.Position += size - 4;
                    break;
                case "ConfigDamageCritical":
                case "class ConfigDamageCritical":
                    reader.BaseStream.Position += 8;
                    break;
                case "class ceng::CColorFloat":
                    reader.BaseStream.Position += 16;
                    break;
                case "struct ceng::math::CXForm<float>":
                    reader.BaseStream.Position += 20;
                    break;
                case "class ConfigLaser":
                    reader.BaseStream.Position += 41;
                    break;
                case "unsigned short":
                case "short":
                case "unsigned int":
                case "int":
                case "unsigned long long":
                case "unsigned long":
                case "long long":
                case "unsigned __int64":
                case "__int64":
                case "long":
                case "float":
                case "double":
                case "bool":
                case "struct ValueRange":
                case "ValueRange":
                case "ValueRangeInt":
                case "<>ceng::math::CVector":
                case "<>enum_type":
                case "<>LensValue":
                case "struct types::aabb":
                case "struct types::iaabb":
                    reader.BaseStream.Position += size;
                    break;
                case "std_string":
                case "std::string":
                case "class std::basic_string<char,struct std::char_traits<char>,class std::allocator<char> >":
                    {
                        int length = reader.ReadBEInt32();
                        reader.BaseStream.Position += length;
                    }
                    break;
                case "class std::vector<int,class std::allocator<int> >":
                    {
                        int length = reader.ReadBEInt32();
                        reader.BaseStream.Position += length * sizeof(int);
                    }
                    break;
                case "class std::vector<float,class std::allocator<float> >":
                    {
                        int length = reader.ReadBEInt32();
                        reader.BaseStream.Position += length * sizeof(float);
                    }
                    break;
                case "class std::vector<double,class std::allocator<double> >":
                    {
                        int length = reader.ReadBEInt32();
                        reader.BaseStream.Position += length * sizeof(double);
                    }
                    break;
                case "class std::vector<class std::basic_string<char,struct std::char_traits<char>,class std::allocator<char> >,class std::allocator<class std::basic_string<char,struct std::char_traits<char>,class std::allocator<char> > > >":
                    int vectorLength = reader.ReadBEInt32();
                    for (int i = 0; i < vectorLength; i++)
                    {
                        int stringLength = reader.ReadBEInt32();
                        reader.BaseStream.Position += stringLength;
                    }
                    break;
                // sizeof(PathFindingJumpParams) = 12
                case "class std::vector<struct PathFindingJumpParams,class std::allocator<struct PathFindingJumpParams> >":
                    {
                        int length = reader.ReadBEInt32();
                        reader.BaseStream.Position += length * 12;
                    }
                    break;
                default:
                    throw new NotImplementedException($"??? type at {reader.BaseStream.Position} {type} {ComponentName}.{name}");
            }
        }

        try
        {
            foreach (ComponentVar componentVar in Schema.Vars[ComponentName])
            {
                ProcessField(componentVar.Type, componentVar.Name, componentVar.Size);
            }
        }
        catch (NotImplementedException) { throw; }
        catch
        {
            Logger.LogWarning($"Error reading component {this.ComponentName}. Exception at {reader.BaseStream.Position} for \"{ComponentName}\":");

            throw;
        }
    }
}