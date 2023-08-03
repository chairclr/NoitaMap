using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

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

        // 1 ??? byte
        reader.BaseStream.Position += 1;
    }
}
