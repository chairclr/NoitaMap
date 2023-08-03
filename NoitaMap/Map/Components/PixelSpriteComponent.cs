using System.Numerics;

namespace NoitaMap.Map.Components;

public class PixelSpriteComponent : Component
{
    public string? ImageFile;

    public int AnchorX;

    public int AnchorY;

    public string? Material;

    public bool Diggable;

    public bool CleanOverlappingPixels;

    public bool KillWhenSpriteDies;

    public bool CreateBox2dDodies;

    public Vector2 Position;

    public Vector2 Scale;

    public int CustomTextureWidth;

    public int CustomTextureHeight;

    public Rgba32[,]? CustomTextureData;

    public int CustomTextureHash;

    public PixelSpriteComponent(string name)
        : base(name)
    {

    }

    public override void Deserialize(BinaryReader reader)
    {
        base.Deserialize(reader);

        ImageFile = reader.ReadNoitaString();

        AnchorX = reader.ReadBEInt32();

        AnchorY = reader.ReadBEInt32();

        Material = reader.ReadNoitaString();

        Diggable = reader.ReadBoolean();

        CleanOverlappingPixels = reader.ReadBoolean();

        KillWhenSpriteDies = reader.ReadBoolean();

        CreateBox2dDodies = reader.ReadBoolean();

        // pixel sprite pointer

        // Image file again
        int len = reader.ReadBEInt32();
        reader.BaseStream.Position += len;

        float x = reader.ReadBESingle();
        float y = reader.ReadBESingle();

        Position = new Vector2(x, y);

        float scalex = reader.ReadBESingle();
        float scaley = reader.ReadBESingle();

        Scale = new Vector2(scalex, scaley);

        // Material again
        len = reader.ReadBEInt32();
        reader.BaseStream.Position += len;

        // anchor_x again
        reader.BaseStream.Position += 4;
        // anchor_y again
        reader.BaseStream.Position += 4;

        // 5 ??? bytes
        reader.BaseStream.Position += 5;

        len = reader.ReadBEInt32();
        reader.BaseStream.Position += len;

        bool hasCustomTexture = reader.ReadBoolean();

        if (hasCustomTexture)
        {
            CustomTextureWidth = reader.ReadBEInt32();

            CustomTextureHeight = reader.ReadBEInt32();

            CustomTextureData = new Rgba32[CustomTextureWidth, CustomTextureHeight];

            for (int tx = 0; tx < CustomTextureWidth; tx++)
            {
                for (int ty = 0; ty < CustomTextureHeight; ty++)
                {
                    uint value = reader.ReadBEUInt32();
                    CustomTextureData[tx, ty].PackedValue = value;
                    CustomTextureHash = HashCode.Combine(CustomTextureHash, value);
                }
            }
        }
    }
}
