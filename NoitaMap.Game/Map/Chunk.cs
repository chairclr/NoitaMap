using System.Buffers;
using System;
using System.IO;
using System.Text;
using osu.Framework.Graphics.Textures;
using osuTK;
using NoitaMap.Game.Materials;
using SixLabors.ImageSharp.PixelFormats;
using osu.Framework.Graphics.Rendering;
using System.Runtime.InteropServices;
using NUnit.Framework.Constraints;
using System.Runtime.CompilerServices;
using SixLabors.ImageSharp;

namespace NoitaMap.Game.Map;

public partial class Chunk
{
    public const int ChunkWidth = 512;

    public const int ChunkHeight = 512;

    public readonly Vector2 Position;

    public Texture? InternalTexture = null;

    public Rgba32[,]? TextureData;

    public bool ReadyForTextureCreation { get; private set; }

    private MaterialProvider MaterialProvider;

    public Chunk(Vector2 position, MaterialProvider materialProvider)
    {
        Position = position;

        MaterialProvider = materialProvider;
    }

    public void Deserialize(BinaryReader reader)
    {
        byte[,] cellTable = new byte[ChunkWidth, ChunkHeight];

        reader.Read(MemoryMarshal.CreateSpan(ref cellTable[0, 0], ChunkWidth * ChunkHeight));

        string[] materialNames = ReadMaterialNames(reader);

        Material[] materials = MaterialProvider.CreateMaterialMap(materialNames);

        Rgba32[] customColors = ReadCustomColors(reader);

        int chunkX = (int)Position.X;
        int chunkY = (int)Position.Y;

        TextureData = new Rgba32[ChunkWidth, ChunkHeight];

        int customColorIndex = 0;
        for (int x = 0; x < ChunkWidth; x++)
        {
            for (int y = 0; y < ChunkHeight; y++)
            {
                int material = cellTable[x, y] & (~0x80);
                bool customColor = (cellTable[x, y] & 0x80) != 0;

                if (customColor)
                {
                    TextureData[x, y] = customColors[customColorIndex];
                    // explicit > implicit
                    customColorIndex++;
                }
                else
                {
                    Material mat = materials[material];

                    if (mat.Name == "err")
                    {
                        TextureData[x, y] = mat.Colors[x % mat.Colors.GetLength(0), y % mat.Colors.GetLength(1)];
                    }
                    else
                    {
                        int wx = (x + chunkX * ChunkWidth) * 6;
                        int wy = (y + chunkY * ChunkHeight) * 6;

                        int colorX = ((wx & Material.MaterialWidthM1) + Material.MaterialWidthM1) & Material.MaterialWidthM1;
                        int colorY = ((wy & Material.MaterialHeightM1) + Material.MaterialHeightM1) & Material.MaterialHeightM1;

                        TextureData[x, y] = mat.Colors[colorX, colorY];
                    }
                }
            }
        }

        ReadyForTextureCreation = true;
    }

    public void CreateTexture(IRenderer renderer)
    {
        if (!ReadyForTextureCreation)
        {
            throw new InvalidOperationException("Not ready for Texture creation");
        }

        ReadyForTextureCreation = false;

        InternalTexture = renderer.CreateTexture(ChunkWidth, ChunkHeight, filteringMode: TextureFilteringMode.Nearest);

        Image<Rgba32> image = SixLabors.ImageSharp.Image.LoadPixelData<Rgba32>(MemoryMarshal.CreateSpan(ref TextureData![0, 0], ChunkWidth * ChunkHeight), ChunkWidth, ChunkHeight);

        InternalTexture.BypassTextureUploadQueueing = true;

        InternalTexture.SetData(new TextureUpload(image));
    }

    private string[] ReadMaterialNames(BinaryReader reader)
    {
        int materialNameCount = reader.ReadBEInt32();

        string[] materialNames = new string[materialNameCount];

        for (int i = 0; i < materialNameCount; i++)
        {
            int size = reader.ReadBEInt32();

            // rent a buffer here for fast :thumbs_up:
            byte[] stringBuffer = ArrayPool<byte>.Shared.Rent(size);

            reader.Read(stringBuffer.AsSpan()[..size]);

            materialNames[i] = Encoding.UTF8.GetString(stringBuffer.AsSpan()[..size]);

            ArrayPool<byte>.Shared.Return(stringBuffer);
        }

        return materialNames;
    }

    private Rgba32[] ReadCustomColors(BinaryReader reader)
    {
        int materialWorldColorCount = reader.ReadBEInt32();

        Rgba32[] materialWorldColors = new Rgba32[materialWorldColorCount];

        for (int i = 0; i < materialWorldColorCount; i++)
        {
            materialWorldColors[i].PackedValue = reader.ReadBEUInt32();
        }

        return materialWorldColors;
    }
}
