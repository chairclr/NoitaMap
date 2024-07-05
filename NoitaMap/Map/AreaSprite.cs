

using System.Numerics;
using System.Xml;
using System.Xml.Serialization;
using NoitaMap.Graphics;
using NoitaMap.Logging;
using NoitaMap.Map.Components;
using SixLabors.ImageSharp;

namespace NoitaMap.Map;

public class AreaEntitySprite : IAtlasObject
{
    public Matrix4x4 WorldMatrix { get; set; }

    public Rgba32[,]? WorkingTextureData { get; set; }

    public int TextureWidth { get; set; }

    public int TextureHeight { get; set; }

    public int TextureHash { get; set; }

    public float ScaleX = 1.0f;

    public float ScaleY = 1.0f;

    public float OffsetX;

    public float OffsetY;

    public float TransformOffsetX;

    public float TransformOffsetY;

    public AreaEntitySprite(string xmlFilePath, Vector2 position)
    {
        LoadImage(xmlFilePath);

        WorldMatrix =
            Matrix4x4.CreateScale(TextureWidth * ScaleX, TextureHeight * ScaleY, 1f)
            * Matrix4x4.CreateTranslation(TransformOffsetX - OffsetX, TransformOffsetY - OffsetY, 0f)
            // * Matrix4x4.CreateRotationZ(Entity.Rotation)
            * Matrix4x4.CreateTranslation(position.X, position.Y, 0f);
    }

    // TODO: Refactor this lmao
    private void LoadImage(string xmlFilePath)
    {
        if (PathService.DataPath is null)
        {
            return;
        }

        string caselessBaseXmlFilePath = xmlFilePath.ToLower();

        string? fullXmlPath = null;
        if (caselessBaseXmlFilePath.StartsWith("data/"))
        {
            fullXmlPath = Path.Combine(PathService.DataPath, caselessBaseXmlFilePath.Remove(0, 5));
        }

        if (fullXmlPath is null || !File.Exists(fullXmlPath))
        {
            return;
        }

        string baseXmlContent = File.ReadAllText(fullXmlPath);

        XmlDocument xmlDoc = new XmlDocument();
        xmlDoc.LoadXml(baseXmlContent);
        
        XmlNode? spriteNode = xmlDoc.SelectSingleNode("//SpriteComponent");

        if (spriteNode is null)
        {
            return;
        }

        string spriteXml = spriteNode.OuterXml;

        
        XmlSerializer serializer = new XmlSerializer(typeof(SpriteComponentData));
        using StringReader xmlText = new StringReader(spriteXml);

        SpriteComponentData spriteComponentData = (SpriteComponentData)serializer.Deserialize(xmlText)!;

        if (spriteComponentData.HasSpecialScale != 0)
        {
            ScaleX = spriteComponentData.SpecialScaleX;
            ScaleY = spriteComponentData.SpecialScaleY;
        }

        OffsetX = spriteComponentData.OffsetX / 2;
        OffsetY = spriteComponentData.OffsetY / 2; 

        TransformOffsetX = spriteComponentData.TransformOffsetX;
        TransformOffsetY = spriteComponentData.TransformOffsetY;

        string gfxFilePath = spriteComponentData.ImageFile ?? "";

        string caselessGfxFilePath = gfxFilePath.ToLower();

        string? fullGfxPath = null;
        if (caselessGfxFilePath.StartsWith("data/"))
        {
            fullGfxPath = Path.Combine(PathService.DataPath, caselessGfxFilePath.Remove(0, 5));
        }

        if (fullGfxPath is null || !File.Exists(fullGfxPath))
        {
            return;
        }

        string gfxXmlContent = File.ReadAllText(fullGfxPath);

        XmlSerializer gfxSerializer = new XmlSerializer(typeof(SpriteData));
        using StringReader gfxXmlText = new StringReader(gfxXmlContent);

        SpriteData spriteData = (SpriteData)gfxSerializer.Deserialize(gfxXmlText)!;


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

        SpriteRectAnimation? rectAnimation = spriteData.RectAnimation!.FirstOrDefault(x => spriteData.DefaultAnimation == x.Name);

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
}

[XmlRoot(ElementName="SpriteComponent")]
public class SpriteComponentData 
{ 
	[XmlAttribute(AttributeName="_tags")] 
	public string? Tags { get; set; } 

	[XmlAttribute(AttributeName="_enabled")] 
	public int Enabled { get; set; } 

	[XmlAttribute(AttributeName="transform_offset.x")] 
	public float TransformOffsetX { get; set; } 

	[XmlAttribute(AttributeName="transform_offset.y")] 
	public float TransformOffsetY { get; set; } 

	[XmlAttribute(AttributeName="alpha")] 
	public float Alpha { get; set; } 

	[XmlAttribute(AttributeName="has_special_scale")] 
	public int HasSpecialScale { get; set; } 

	[XmlAttribute(AttributeName="image_file")] 
	public string? ImageFile { get; set; } 

	[XmlAttribute(AttributeName="is_text_sprite")] 
	public int IsTextSprite { get; set; } 

	[XmlAttribute(AttributeName="offset_x")] 
	public float OffsetX { get; set; } 

	[XmlAttribute(AttributeName="offset_y")] 
	public float OffsetY { get; set; }

	[XmlAttribute(AttributeName="special_scale_x")] 
	public float SpecialScaleX { get; set; } 

	[XmlAttribute(AttributeName="special_scale_y")] 
	public float SpecialScaleY { get; set; } 

	[XmlAttribute(AttributeName="ui_is_parent")] 
	public int UiIsParent { get; set; } 

	[XmlAttribute(AttributeName="update_transform")] 
	public int UpdateTransform { get; set; } 

	[XmlAttribute(AttributeName="visible")] 
	public int Visible { get; set; } 

	[XmlAttribute(AttributeName="emissive")] 
	public int Emissive { get; set; } 

	[XmlAttribute(AttributeName="never_ragdollify_on_death")] 
	public int NeverRagdollifyOnDeath { get; set; } 

	[XmlAttribute(AttributeName="z_index")] 
	public int ZIndex { get; set; } 
}
