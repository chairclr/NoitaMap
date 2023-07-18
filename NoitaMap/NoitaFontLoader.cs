using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace NoitaMap;

internal class NoitaFontLoader
{
    public static SpriteFont Load()
    {
        XmlSerializer serializer = new XmlSerializer(typeof(FontData));
        using StringReader reader = new StringReader(File.ReadAllText("Assets/font_pixel.xml"));

        FontData fontData = (FontData)serializer.Deserialize(reader)!;

        Texture2D texture = Texture2D.FromFile(GraphicsDeviceProvider.GraphicsDevice, "Assets/font_pixel.png");

        List<Rectangle> rects = fontData.QuadChar.Select(x => new Rectangle(x.RectX, x.RectY, x.RectW, x.RectH)).ToList();

        return new SpriteFont(texture, rects, rects.Select(x => texture.Bounds).ToList(), fontData.QuadChar.Select(x => (char)x.Id).ToList(), fontData.LineHeight, 0, fontData.QuadChar.Select(x => new Vector3(0f, x.Width, 0f)).ToList(), null);
    }
}

[XmlRoot(ElementName = "QuadChar")]
public class QuadChar
{
    [XmlAttribute(AttributeName = "id")]
    public int Id { get; set; }

    [XmlAttribute(AttributeName = "offset_x")]
    public int OffsetX { get; set; }

    [XmlAttribute(AttributeName = "offset_y")]
    public int OffsetY { get; set; }

    [XmlAttribute(AttributeName = "rect_h")]
    public int RectH { get; set; }

    [XmlAttribute(AttributeName = "rect_w")]
    public int RectW { get; set; }

    [XmlAttribute(AttributeName = "rect_x")]
    public int RectX { get; set; }

    [XmlAttribute(AttributeName = "rect_y")]
    public int RectY { get; set; }

    [XmlAttribute(AttributeName = "width")]
    public int Width { get; set; }
}

[XmlRoot(ElementName = "FontData")]
public class FontData
{
    [NotNull]
    [XmlElement(ElementName = "Texture")]
    public string? Texture { get; set; }

    [XmlElement(ElementName = "LineHeight")]
    public int LineHeight { get; set; }

    [XmlElement(ElementName = "CharSpace")]
    public int CharSpace { get; set; }

    [XmlElement(ElementName = "WordSpace")]
    public int WordSpace { get; set; }

    [NotNull]
    [XmlElement(ElementName = "QuadChar")]
    public List<QuadChar>? QuadChar { get; set; }
}