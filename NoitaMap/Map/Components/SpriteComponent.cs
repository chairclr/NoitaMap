using System.Numerics;
using System.Xml.Serialization;
using CommunityToolkit.HighPerformance;
using NoitaMap.Graphics;
using NoitaMap.Map.Entities;
using Vortice.DXGI;

namespace NoitaMap.Map.Components;

public class SpriteComponent(Entity entity, string name) : Component(entity, name), IAtlasObject
{
    public string? ImageFile;

    public bool UIIsParent;

    public bool IsTextSprite;

    public float OffsetX;

    public float OffsetY;

    public Vector2 TransformOffset;

    public Vector2 OffsetAnimatorOffset;

    public float Alpha;

    public bool Visible;

    public bool Emissive;

    public bool Additive;

    public bool FogOfWarHole;

    public bool SmoothFiltering;

    public string? RectAnimation;

    public string? NextRectAnimation;

    public string? Text;

    public float ZIndex;

    public bool UpdateTransform;

    public bool UpdateTransformRotation;

    public bool KillEntityAfterFinished;

    public bool HasSpecialScale;

    public float SpecialScaleX;

    public float SpecialScaleY;

    public bool NeverRagdollifyOnDeath;

    public Matrix4x4 WorldMatrix { get; set; }

    public Rgba32[,]? WorkingTextureData { get; set; }

    public int TextureWidth { get; set; }

    public int TextureHeight { get; set; }

    public int TextureHash { get; set; }

    public override void Deserialize(BinaryReader reader)
    {
        base.Deserialize(reader);

        ImageFile = reader.ReadNoitaString();

        UIIsParent = reader.ReadBoolean();

        IsTextSprite = reader.ReadBoolean();

        OffsetX = reader.ReadBESingle();

        OffsetY = reader.ReadBESingle();

        TransformOffset = new Vector2(reader.ReadBESingle(), reader.ReadBESingle());

        OffsetAnimatorOffset = new Vector2(reader.ReadBESingle(), reader.ReadBESingle());

        Alpha = reader.ReadBESingle();

        Visible = reader.ReadBoolean();

        Emissive = reader.ReadBoolean();

        Additive = reader.ReadBoolean();

        FogOfWarHole = reader.ReadBoolean();

        SmoothFiltering = reader.ReadBoolean();

        RectAnimation = reader.ReadNoitaString();

        NextRectAnimation = reader.ReadNoitaString();

        Text = reader.ReadNoitaString();

        ZIndex = reader.ReadBESingle();

        UpdateTransform = reader.ReadBoolean();

        UpdateTransformRotation = reader.ReadBoolean();

        KillEntityAfterFinished = reader.ReadBoolean();

        HasSpecialScale = reader.ReadBoolean();

        SpecialScaleX = reader.ReadBESingle();

        SpecialScaleY = reader.ReadBESingle();

        NeverRagdollifyOnDeath = reader.ReadBoolean();

        if (ImageFile is not null && PathService.DataPath is not null)
        {
            ImageFile = ImageFile.ToLower();

            string? path = null;
            if (ImageFile.StartsWith("data/"))
            {
                path = Path.Combine(PathService.DataPath!, ImageFile.Remove(0, 5));
            }

            if (path is not null && File.Exists(path))
            {
                if (path.EndsWith(".xml"))
                {
                    if (RectAnimation is not null)
                    {
                        string text = File.ReadAllText(path);

                        XmlSerializer serializer = new XmlSerializer(typeof(SpriteData));
                        using StringReader xmlText = new StringReader(text);

                        SpriteData spriteData = (SpriteData)serializer.Deserialize(xmlText)!;

                        string? imagePath = spriteData.Filename;

                        if (imagePath is not null)
                        {
                            if (imagePath.StartsWith("data/"))
                            {
                                imagePath = Path.Combine(PathService.DataPath!, imagePath.Remove(0, 5));
                            }

                            using Image<Rgba32> image = ImageUtility.LoadImage(imagePath);

                            foreach (SpriteRectAnimation sra in spriteData.RectAnimation!)
                            {
                                if (RectAnimation == sra.Name)
                                {
                                    WorkingTextureData = new Rgba32[sra.FrameWidth, sra.FrameHeight];

                                    TextureWidth = sra.FrameWidth;
                                    TextureHeight = sra.FrameHeight;

                                    for (int x = 0; x < sra.FrameWidth; x++)
                                    {
                                        for (int y = 0; y < sra.FrameHeight; y++)
                                        {
                                            Rgba32 col = image[x + sra.PosX, y + sra.PosY];
                                            WorkingTextureData[x, y].PackedValue = col.PackedValue;
                                            TextureHash = HashCode.Combine(TextureHash, col.PackedValue);
                                        }
                                    }

                                    OffsetX += spriteData.OffsetX;
                                    OffsetY += spriteData.OffsetY;

                                    break;
                                }
                            }
                        }
                    }
                }
                else
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

        Vector2 scale = Vector2.One;

        if (HasSpecialScale)
        {
            scale = new Vector2(SpecialScaleX, SpecialScaleY);
        }

        WorldMatrix = Matrix4x4.CreateScale(TextureWidth * scale.X, TextureHeight * scale.Y, 1f) * Matrix4x4.CreateRotationZ(Entity.Rotation) * Matrix4x4.CreateTranslation(Entity.Position.X - TransformOffset.X - OffsetX, Entity.Position.Y - TransformOffset.Y - OffsetY, 0f);
    }
}

[XmlRoot(ElementName = "RectAnimation")]
public class SpriteRectAnimation
{
    [XmlAttribute(AttributeName = "frame_count")]
    public int FrameCount { get; set; }

    [XmlAttribute(AttributeName = "frame_height")]
    public int FrameHeight { get; set; }

    [XmlAttribute(AttributeName = "frame_wait")]
    public double FrameWait { get; set; }

    [XmlAttribute(AttributeName = "frame_width")]
    public int FrameWidth { get; set; }

    [XmlAttribute(AttributeName = "frames_per_row")]
    public int FramesPerRow { get; set; }

    [XmlAttribute(AttributeName = "name")]
    public string? Name { get; set; }

    [XmlAttribute(AttributeName = "pos_x")]
    public int PosX { get; set; }

    [XmlAttribute(AttributeName = "pos_y")]
    public int PosY { get; set; }

    [XmlElement(ElementName = "Event")]
    public List<SpriteRectEvent>? Event { get; set; }

    [XmlAttribute(AttributeName = "shrink_by_one_pixel")]
    public int ShrinkByOnePixel { get; set; }

    [XmlAttribute(AttributeName = "loop")]
    public int Loop { get; set; }
}

[XmlRoot(ElementName = "Event")]
public class SpriteRectEvent
{
    [XmlAttribute(AttributeName = "check_physics_material")]
    public int CheckPhysicsMaterial { get; set; }

    [XmlAttribute(AttributeName = "frame")]
    public int Frame { get; set; }

    [XmlAttribute(AttributeName = "max_distance")]
    public int MaxDistance { get; set; }

    [XmlAttribute(AttributeName = "name")]
    public string? Name { get; set; }

    [XmlAttribute(AttributeName = "on_finished")]
    public int OnFinished { get; set; }

    [XmlAttribute(AttributeName = "probability")]
    public int Probability { get; set; }
}

[XmlRoot(ElementName = "Sprite")]
public class SpriteData
{
    [XmlElement(ElementName = "RectAnimation")]
    public List<SpriteRectAnimation>? RectAnimation { get; set; }

    [XmlAttribute(AttributeName = "filename")]
    public string? Filename { get; set; }

    [XmlAttribute(AttributeName = "offset_x")]
    public int OffsetX { get; set; }

    [XmlAttribute(AttributeName = "offset_y")]
    public int OffsetY { get; set; }

    [XmlAttribute(AttributeName = "default_animation")]
    public string? DefaultAnimation { get; set; }
}