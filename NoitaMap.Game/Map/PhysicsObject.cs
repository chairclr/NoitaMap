using System;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using osu.Framework.Graphics.Textures;
using osuTK;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace NoitaMap.Game.Map;

public class PhysicsObject
{
    public Vector2 Position;

    public float Rotation;

    public int Width;

    public int Height;

    public Rgba32[,]? TextureData;

    public Texture InternalTexture;

    public bool ReadyToBeAddedToAtlas = true;

    public int TextureHash;

    public void Deserialize(BinaryReader reader)
    {
        reader.ReadBEUInt64();
        reader.ReadBEUInt32();
        Position.X = reader.ReadBESingle();
        Position.Y = reader.ReadBESingle();
        Rotation = reader.ReadBESingle();
        reader.ReadBEInt64();
        reader.ReadBEInt64();
        reader.ReadBEInt64();
        reader.ReadBEInt64();
        reader.ReadBEInt64();
        reader.ReadBoolean();
        reader.ReadBoolean();
        reader.ReadBoolean();
        reader.ReadBoolean();
        reader.ReadBoolean();
        reader.ReadBESingle();
        Width = reader.ReadBEInt32();
        Height = reader.ReadBEInt32();

        TextureData = new Rgba32[Width, Height];

        for (int x = 0; x < Width; x++)
        {
            for (int y = 0; y < Height; y++)
            {
                uint value = reader.ReadBEUInt32();
                TextureData[x, y].PackedValue = value;
                TextureHash = HashCode.Combine(TextureHash, value);
            }
        }
    }

    public unsafe void SetTexture(Texture textureRegion)
    {
        if (!ReadyToBeAddedToAtlas)
        {
            throw new InvalidOperationException("Not ready to be added to atlas");
        }

        ReadyToBeAddedToAtlas = false;

        Span<Rgba32> span = MemoryMarshal.CreateSpan(ref TextureData![0, 0], Width * Height);

        Image<Rgba32> image = SixLabors.ImageSharp.Image.WrapMemory<Rgba32>(Unsafe.AsPointer(ref span[0]), Width, Height);

        textureRegion.BypassTextureUploadQueueing = true;

        textureRegion.SetData(new TextureUpload(image));

        InternalTexture = textureRegion;
    }
}
