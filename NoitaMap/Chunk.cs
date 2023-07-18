using System.Buffers;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace NoitaMap;

internal class Chunk
{
    public const int Width = 512;

    public const int Height = 512;

    public Vector2 Position;

    public Texture2D Texture;

    public PhysicsObject[] PhysicsObjects;

    internal Chunk(int chunkX, int chunkY, BinaryReader reader)
    {
        Position = new Vector2(chunkX, chunkY);

        byte[] packedMaterialInfo = new byte[Width * Height];

        reader.Read(packedMaterialInfo);

        string[] materialNames = ReadMaterialNames(reader);

        Material[] materials = MaterialProvider.CreateMaterialMap(materialNames);

        Color[] customColors = ReadCustomColors(reader);

        Texture = new Texture2D(GraphicsDeviceProvider.GraphicsDevice, Width, Height);

        Color[] colors = ArrayPool<Color>.Shared.Rent(Width * Height);
        int customColorIndex = 0;

        for (int x = 0; x < Width; x++)
        {
            for (int y = 0; y < Height; y++)
            {
                // for indexing colors[] and packedMaterialInfo[]
                int i = y + x * Height;

                int material = packedMaterialInfo[i] & (~0x80);
                bool customColor = (packedMaterialInfo[i] & 0x80) != 0;

                if (customColor)
                {
                    colors[i] = customColors[customColorIndex];
                    // explicit > implicit
                    customColorIndex++;
                }
                else
                {
                    Material mat = materials[material];

                    if (mat.Name == "err")
                    {
                        colors[i] = mat.Colors[(x + y) % 4];
                    }
                    else
                    {
                        int wx = (x + chunkX * Width) * 6;
                        int wy = (y + chunkY * Height) * 6;

                        int colorX = ((wx % Material.MaterialWidth) + Material.MaterialWidth) % Material.MaterialWidth;
                        int colorY = ((wy % Material.MaterialHeight) + Material.MaterialHeight) % Material.MaterialHeight;

                        colors[i] = mat.Colors[colorX + (colorY * Material.MaterialWidth)];
                    }
                }
            }
        }

        Texture.SetData(colors, 0, Width * Height);

        ArrayPool<Color>.Shared.Return(colors);

        PhysicsObjects = ReadPhysicsObjects(reader);
    }

    private static string[] ReadMaterialNames(BinaryReader reader)
    {
        int materialNameCount = reader.BEReadInt32();

        string[] materialNames = new string[materialNameCount];

        for (int i = 0; i < materialNameCount; i++)
        {
            int size = reader.BEReadInt32();

            // rent a buffer here for fast :thumbs_up:
            byte[] stringBuffer = ArrayPool<byte>.Shared.Rent(size);

            reader.Read(stringBuffer.AsSpan()[..size]);

            materialNames[i] = Encoding.UTF8.GetString(stringBuffer.AsSpan()[..size]);

            ArrayPool<byte>.Shared.Return(stringBuffer);
        }

        return materialNames;
    }

    private static Color[] ReadCustomColors(BinaryReader reader)
    {
        int materialWorldColorCount = reader.BEReadInt32();

        Color[] materialWorldColors = new Color[materialWorldColorCount];

        for (int i = 0; i < materialWorldColorCount; i++)
        {
            materialWorldColors[i].PackedValue = reader.BEReadUInt32();
        }

        return materialWorldColors;
    }

    private static PhysicsObject[] ReadPhysicsObjects(BinaryReader reader)
    {
        int physicsObjectCount = reader.BEReadInt32();

        PhysicsObject[] physicsObjects = new PhysicsObject[physicsObjectCount];

        for (int i = 0; i < physicsObjectCount; i++)
        {
            physicsObjects[i] = new PhysicsObject(reader);
        }

        return physicsObjects;
    }

    public override int GetHashCode()
    {
        return Position.GetHashCode();
    }
}
