using System.Numerics;
using CommunityToolkit.HighPerformance;
using NoitaMap.Graphics;
using NoitaMap.Map.Entities;

namespace NoitaMap.Map.Components;

public class PixelSpriteComponent(Entity entity, string name) : Component(entity, name), IAtlasObject
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

    public Matrix4x4 WorldMatrix { get; set; }

    public Rgba32[,]? WorkingTextureData { get; set; }

    public int TextureWidth { get; set; }

    public int TextureHeight { get; set; }

    public int TextureHash { get; set; }

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

        Vector2 extraTextureOffset = Vector2.Zero;

        if (hasCustomTexture)
        {
            TextureWidth = reader.ReadBEInt32();

            TextureHeight = reader.ReadBEInt32();

            WorkingTextureData = new Rgba32[TextureWidth, TextureHeight];

            for (int tx = 0; tx < TextureWidth; tx++)
            {
                for (int ty = 0; ty < TextureHeight; ty++)
                {
                    uint value = reader.ReadBEUInt32();
                    WorkingTextureData[tx, ty].PackedValue = value;
                    TextureHash = HashCode.Combine(TextureHash, value);
                }
            }

            extraTextureOffset = new Vector2(-(TextureWidth * Scale.X) / 2f, -TextureHeight * Scale.Y);
        }
        else
        {
            if (ImageFile is not null && PathService.DataPath is not null)
            {
                string? path = null;

                if (ImageFile.StartsWith("data/"))
                {
                    path = Path.Combine(PathService.DataPath!, ImageFile.Remove(0, 5));
                }

                if (path is not null)
                {
                    using Image<Rgba32> image = ImageUtility.LoadImage(path);

                    WorkingTextureData = new Rgba32[image.Width, image.Height];

                    TextureWidth = image.Width;

                    TextureHeight = image.Height;

                    TextureHash = path.GetHashCode();

                    image.CopyPixelDataTo(WorkingTextureData.AsSpan());
                }
            }
        }

        WorldMatrix = Matrix4x4.CreateScale(TextureWidth * Scale.X, TextureHeight * Scale.Y, 1f) * Matrix4x4.CreateTranslation(Position.X - AnchorX + extraTextureOffset.X, Position.Y - AnchorY + extraTextureOffset.Y, 0f);
    }
}
