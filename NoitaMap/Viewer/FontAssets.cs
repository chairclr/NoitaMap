using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Xml.Serialization;
using CommunityToolkit.HighPerformance;
using ImGuiNET;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats;

namespace NoitaMap.Viewer;

public unsafe class FontAssets
{
    public static void AddImGuiFont()
    {
        ImGuiIOPtr io = ImGui.GetIO();

        // Clear the default font that Veldrid.ImGui adds
        // FontData fontData = LoadFontData();

        // using Image<Rgba32> image = LoadFontImage();

        // OverwriteImGuiFont(io, fontData, image);

        // SetFontParameters(io, image);

        io.Fonts.Clear();

        ImFontConfigPtr font = ImGuiNative.ImFontConfig_ImFontConfig();
        font.SizePixels = 13 * 2f;
        font.PixelSnapH = true;
        font.OversampleH = 1;
        font.OversampleV = 1;
        io.Fonts.AddFontDefault(font);

        ImGui.GetStyle().ScaleAllSizes(2f);
    }

    private static FontData LoadFontData()
    {
        XmlSerializer serializer = new XmlSerializer(typeof(FontData));
        using StringReader reader = new StringReader(File.ReadAllText("Assets/Fonts/font_pixel.xml"));
        return (FontData)serializer.Deserialize(reader)!;
    }

    private static Image<Rgba32> LoadFontImage()
    {
        Configuration config = Configuration.Default;
        config.PreferContiguousImageBuffers = true;

        return Image.Load<Rgba32>(new DecoderOptions() { Configuration = config }, $"Assets/Fonts/font_pixel.png");
    }

    private static void OverwriteImGuiFont(ImGuiIOPtr io, FontData fontData, Image<Rgba32> image)
    {
        io.Fonts.Clear();
        io.Fonts.AddFontDefault();
        io.Fonts.Build();

        io.Fonts.GetTexDataAsRGBA32(out byte* byteOriginalAtlasPixels, out int originalAtlasWidth, out int originalAtlasHeight);
        io.Fonts.TexReady = false;
        Rgba32* originalAtlasPixels = (Rgba32*)ImGuiNative.igMemAlloc((uint)(originalAtlasWidth * originalAtlasHeight * Unsafe.SizeOf<Rgba32>()));
        Unsafe.CopyBlock(originalAtlasPixels, byteOriginalAtlasPixels, (uint)(originalAtlasWidth * originalAtlasHeight * Unsafe.SizeOf<Rgba32>()));

        ImFontConfigPtr fontConfig = ImGuiNative.ImFontConfig_ImFontConfig();
        fontConfig.FontDataOwnedByAtlas = true;
        fontConfig.FontData = 0;
        fontConfig.FontDataSize = 0;
        fontConfig.SizePixels = fontData.LineHeight;

        ImFontPtr font = io.Fonts.AddFont(fontConfig);

        int newAtlasWidth = Math.Max(image.Width, originalAtlasWidth);
        int newAtlasHeight = originalAtlasHeight + image.Height;

        io.Fonts.TexWidth = newAtlasWidth;
        io.Fonts.TexHeight = newAtlasHeight;

        ImFontPtr defaultFont = io.Fonts.Fonts[0];

        float RescaleU(float u)
        {
            return u * ((float)originalAtlasWidth / (float)newAtlasWidth);
        }

        float RescaleV(float v)
        {
            return v * ((float)originalAtlasHeight / (float)newAtlasHeight);
        }

        Vector2 RescaleUV(Vector2 uv)
        {
            return new Vector2(RescaleU(uv.X), RescaleV(uv.Y));
        }

        for (int i = 0; i < io.Fonts.TexUvLines.Count; i++)
        {
            ref Vector4 v = ref io.Fonts.TexUvLines[i];

            v.X = RescaleU(v.X);
            v.Z = RescaleU(v.Z);
            v.Y = RescaleV(v.Y);
            v.W = RescaleV(v.W);
        }

        io.Fonts.TexUvWhitePixel = RescaleUV(io.Fonts.TexUvWhitePixel);

        io.Fonts.TexUvScale = new Vector2(1f / (float)newAtlasWidth, 1f / (float)newAtlasHeight);

        font.NativePtr->ConfigData = fontConfig;
        font.NativePtr->ConfigDataCount = 1;
        font.NativePtr->ContainerAtlas = io.Fonts.NativePtr;
        font.FontSize = fontData.LineHeight;
        font.FallbackAdvanceX = fontData.QuadChar[0].Width;

        QuadChar ellipsis = fontData.QuadChar[^2];
        font.EllipsisChar = (ushort)ellipsis.Id;
        font.EllipsisCharCount = 1;
        font.EllipsisCharStep = (ushort)ellipsis.Width;
        font.EllipsisWidth = (ushort)ellipsis.Width;

        int offsetX = 0;
        int offsetY = originalAtlasHeight;

        foreach (QuadChar quad in fontData.QuadChar)
        {
            font.AddGlyph(fontConfig,
                (ushort)quad.Id, quad.OffsetX - 1f, quad.OffsetY - 1f, quad.RectW - 1f, quad.RectH - 1f,
                (float)(quad.RectX + offsetX) / (float)newAtlasWidth, (float)(quad.RectY + offsetY) / (float)newAtlasHeight, (float)(quad.RectX + offsetX + quad.RectW) / (float)newAtlasWidth, (float)(quad.RectY + offsetY + quad.RectH) / (float)newAtlasHeight,
                quad.Width);
        }

        io.NativePtr->FontDefault = font.NativePtr;

        Rgba32* newAtlasPixels = (Rgba32*)ImGuiNative.igMemAlloc((uint)(newAtlasWidth * newAtlasHeight * Unsafe.SizeOf<Rgba32>()));
        io.Fonts.NativePtr->TexPixelsRGBA32 = (uint*)newAtlasPixels;

        // Copy the data from the original atlas to the new atlas
        for (int x = 0; x < originalAtlasWidth; x++)
        {
            for (int y = 0; y < originalAtlasHeight; y++)
            {
                newAtlasPixels[x + y * newAtlasWidth] = originalAtlasPixels[x + y * originalAtlasWidth];
            }
        }

        image.DangerousTryGetSinglePixelMemory(out Memory<Rgba32> imageMemory);
        Unsafe.CopyBlock(newAtlasPixels + originalAtlasHeight * newAtlasWidth, Unsafe.AsPointer(ref imageMemory.Span.DangerousGetReference()), (uint)(image.Width * image.Height * Unsafe.SizeOf<Rgba32>()));
    }

    private static void SetFontParameters(ImGuiIOPtr io, Image<Rgba32> image)
    {
        //io.NativePtr->FontDefault = io.Fonts.Fonts[0].NativePtr;

        for (int i = 0; i < io.Fonts.Fonts.Size; i++)
        {
            if (io.Fonts.Fonts[i].DirtyLookupTables)
            {
                io.Fonts.Fonts[i].BuildLookupTable();
            }
        }

        io.Fonts.TexReady = true;

        // 2x the font size for more readability
        // io.FontGlobalScale = 2f;

        // Scale everything else up
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
