using System.Numerics;
using NoitaMap.Entities;
using SixLabors.ImageSharp.PixelFormats;

namespace NoitaMap.Components;

public class PixelSpriteComponent(Entity entity, string name) : Component(entity, name)
{
    public string? ImageFile;

    public int AnchorX;

    public int AnchorY;

    public string? Material;

    public bool Diggable;

    public bool CleanOverlappingPixels;

    public bool KillWhenSpriteDies;

    public bool CreateBox2dBodies;

    public Vector2 Position;

    public Vector2 Scale;

    public Matrix4x4 WorldMatrix { get; set; }

    public Rgba32[,]? CustomTexture { get; set; }

    public int CustomTextureWidth { get; set; }

    public int CustomTextureHeight { get; set; }

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

        CreateBox2dBodies = reader.ReadBoolean();

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

            CustomTexture = new Rgba32[CustomTextureWidth, CustomTextureHeight];

            for (int tx = 0; tx < CustomTextureWidth; tx++)
            {
                for (int ty = 0; ty < CustomTextureHeight; ty++)
                {
                    uint value = reader.ReadBEUInt32();
                    CustomTexture[tx, ty].PackedValue = value;
                }
            }
        }
    }
}