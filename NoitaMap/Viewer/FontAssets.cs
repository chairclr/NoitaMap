using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Xml.Serialization;
using CommunityToolkit.HighPerformance;
using ImGuiNET;
using SixLabors.ImageSharp.Formats;

namespace NoitaMap.Viewer;

public unsafe class FontAssets
{
    public static void LoadAndAddfont()
    {
        ImGuiIOPtr io = ImGui.GetIO();

        io.Fonts.Clear();

        ImFontConfigPtr fontConfig = ImGuiNative.ImFontConfig_ImFontConfig();

        Configuration config = Configuration.Default;

        config.PreferContiguousImageBuffers = true;

        XmlSerializer serializer = new XmlSerializer(typeof(FontData));
        using StringReader reader = new StringReader(File.ReadAllText("Assets/Fonts/font_pixel.xml"));

        FontData fontData = (FontData)serializer.Deserialize(reader)!;

        using Image<Rgba32> image = Image.Load<Rgba32>(new DecoderOptions() { Configuration = config }, $"Assets/Fonts/font_pixel.png");

        image.DangerousTryGetSinglePixelMemory(out Memory<Rgba32> fontMemory);

        fontConfig.FontData = 0;

        fontConfig.FontDataSize = 0;

        fontConfig.SizePixels = fontData.LineHeight;

        ImFontPtr font = io.Fonts.AddFont(fontConfig);

        void* ptr = ImGuiNative.igMemAlloc((uint)(fontMemory.Length * Unsafe.SizeOf<Rgba32>()));

        Unsafe.CopyBlock(ptr, Unsafe.AsPointer(ref fontMemory.Span.DangerousGetReference()), (uint)(fontMemory.Length * Unsafe.SizeOf<Rgba32>()));

        io.Fonts.NativePtr->TexPixelsRGBA32 = (uint*)ptr;

        io.Fonts.TexWidth = image.Width;

        io.Fonts.TexHeight = image.Height;

        font.NativePtr->ContainerAtlas = io.Fonts.NativePtr;

        font.NativePtr->ConfigData = fontConfig.NativePtr;

        font.FontSize = fontData.LineHeight;

        font.FallbackAdvanceX = fontData.QuadChar.First().Width;

        QuadChar ellipsis = fontData.QuadChar[^2];

        font.EllipsisChar = (ushort)ellipsis.Id;

        font.EllipsisCharCount = 1;

        font.EllipsisCharStep = (ushort)ellipsis.Width;

        font.EllipsisWidth = (ushort)ellipsis.Width;

        float iw = image.Width;
        float ih = image.Height;

        foreach (QuadChar quad in fontData.QuadChar)
        {
            font.AddGlyph(fontConfig, (ushort)quad.Id, quad.OffsetX - 1f, quad.OffsetY - 1f, quad.RectW - 1f, quad.RectH - 1f, (float)quad.RectX / iw, (float)quad.RectY / ih, (float)(quad.RectX + quad.RectW) / iw, (float)(quad.RectY + quad.RectH) / ih, quad.Width);
        }

        io.NativePtr->FontDefault = font.NativePtr;

        for (int i = 0; i < io.Fonts.Fonts.Size; i++)
            if (io.Fonts.Fonts[i].DirtyLookupTables)
                io.Fonts.Fonts[i].BuildLookupTable();

        io.Fonts.TexReady = true;

        io.Fonts.TexUvWhitePixel = new Vector2(0.5017f, 0f);

        io.FontGlobalScale = 2f;

        ImGui.GetStyle().ScaleAllSizes(2f);
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
