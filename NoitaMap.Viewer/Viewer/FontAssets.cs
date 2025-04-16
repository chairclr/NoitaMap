using ImGuiNET;

namespace NoitaMap.Viewer;

public unsafe class FontAssets
{
    public static void AddImGuiFont()
    {
        ImGuiIOPtr io = ImGui.GetIO();

        ImFontConfigPtr fontConfig = ImGuiNative.ImFontConfig_ImFontConfig();
        fontConfig.SizePixels = 11 * 3f;
        fontConfig.PixelSnapH = true;
        fontConfig.OversampleH = 1;
        fontConfig.OversampleV = 1;

        ImFontPtr noitaFont = io.Fonts.AddFontFromFileTTF(Path.Combine(PathService.ApplicationPath, "Assets", "Fonts", "NoitaPixel.ttf"), fontConfig.SizePixels, fontConfig);

        io.NativePtr->FontDefault = noitaFont.NativePtr;
    }
}

