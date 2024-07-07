using System.Numerics;
using System.Xml.Serialization;
using CommunityToolkit.HighPerformance;
using NoitaMap.Graphics;
using NoitaMap.Map.Entities;
using SixLabors.ImageSharp;

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

        LoadImage();

        Vector2 scale = Vector2.One;

        if (HasSpecialScale)
        {
            scale = new Vector2(SpecialScaleX, SpecialScaleY);
        }

        WorldMatrix =
            Matrix4x4.CreateScale(TextureWidth * scale.X, TextureHeight * scale.Y, 1f)
            * Matrix4x4.CreateTranslation(-TransformOffset.X - OffsetX, -TransformOffset.Y - OffsetY, 0f)
            * Matrix4x4.CreateRotationZ(Entity.Rotation)
            * Matrix4x4.CreateTranslation(Entity.Position.X, Entity.Position.Y, 0f);
    }

    private void LoadImage()
    {
        if (ImageFile is null || PathService.DataPath is null)
        {
            return;
        }

        string caselessImageFile = ImageFile.ToLower();

        string? path = null;
        if (caselessImageFile.StartsWith("data/"))
        {
            path = Path.Combine(PathService.DataPath, caselessImageFile.Remove(0, 5));
        }

        if (path is null || !File.Exists(path))
        {
            return;
        }

        if (path.EndsWith(".xml"))
        {
            if (RectAnimation is null)
            {
                return;
            }

            SpriteData spriteData = XmlUtility.LoadXml<SpriteData>(File.ReadAllText(path))!;

            string? imagePath = spriteData.Filename?.ToLower();

            if (imagePath is null)
            {
                return;
            }

            if (imagePath.StartsWith("data/"))
            {
                imagePath = Path.Combine(PathService.DataPath!, imagePath.Remove(0, 5));
            }

            using Image<Rgba32> image = ImageUtility.LoadImage(imagePath);

            SpriteRectAnimation? rectAnimation =
                spriteData.RectAnimation!.FirstOrDefault(x => RectAnimation == x.Name)
                ?? spriteData.RectAnimation!.Single(x => spriteData.DefaultAnimation == x.Name);

            if (rectAnimation is null)
            {
                return;
            }

            int width = rectAnimation.FrameWidth;
            int height = rectAnimation.FrameHeight;

            WorkingTextureData = new Rgba32[height, width];

            TextureWidth = width;
            TextureHeight = height;

            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    Rgba32 col = image[x + rectAnimation.PosX, y + rectAnimation.PosY];
                    WorkingTextureData[y, x] = col;
                    TextureHash = HashCode.Combine(TextureHash, col.PackedValue);
                }
            }

            OffsetX += spriteData.OffsetX;
            OffsetY += spriteData.OffsetY;
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
    public float OffsetX { get; set; }

    [XmlAttribute(AttributeName = "offset_y")]
    public float OffsetY { get; set; }

    [XmlAttribute(AttributeName = "default_animation")]
    public string? DefaultAnimation { get; set; }
}
