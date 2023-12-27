using System.Numerics;
using ImGuiNET;
using NoitaMap.Map;
using NoitaMap.Map.Components;
using NoitaMap.Map.Entities;

namespace NoitaMap.Viewer;

public partial class ViewerDisplay
{
    private bool ShowMetrics = true;

    private bool ShowDebugger = false;

    private bool DebugDrawChunkBorders = false;

    private bool DebugDrawPhysicsObjectBorders = false;

    private bool DebugDrawPixelSceneBorders = false;

    private bool DebugDrawPixelSpriteComponentBorders = false;

    private bool DebugDrawSpriteComponentBorders = false;

    private bool ShowSearch = false;

    private string SearchText = "";

    private void DrawUI()
    {
        DrawMetricsUI();

        DrawDebugUI();

        DrawSearchUI();
    }

    private void DrawMetricsUI()
    {
        ImGui.SetNextWindowPos(Vector2.Zero);
        ImGui.Begin("##StatusWindow", ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoBackground | ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.NoMouseInputs | ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoSavedSettings | ImGuiWindowFlags.NoScrollWithMouse | ImGuiWindowFlags.AlwaysAutoResize);

        ImGui.TextUnformatted($"Framerate:     {ImGui.GetIO().Framerate:F1}");

        ImGui.TextUnformatted($"Chunks Loaded: {LoadedChunks} / {TotalChunkCount}");

        if (ImGui.IsKeyPressed(ImGuiKey.F11, false))
        {
            ShowMetrics = !ShowMetrics;
        }

        if (ShowMetrics)
        {
            ImGui.TextUnformatted($"---- Metrics ----");
            foreach ((string name, Func<string> format) in Statistics.Metrics)
            {
                ImGui.TextUnformatted($"{name + ":",-20} {format()}");
            }

#if TIME_STATS
            ImGui.TextUnformatted($"---- Per Frame Times ----");
            foreach ((string name, TimeSpan time) in Statistics.OncePerFrameTimeStats)
            {
                ImGui.TextUnformatted($"{name + ":",-20} {time.TotalSeconds:F5}s");
            }

            ImGui.TextUnformatted($"---- Summed Times ----");
            foreach ((string name, TimeSpan time) in Statistics.SummedTimeStats)
            {
                ImGui.TextUnformatted($"{name + ":",-20} {time.TotalSeconds:F5}s");
            }

            ImGui.TextUnformatted($"---- Single Times ----");
            foreach ((string name, TimeSpan time) in Statistics.SingleTimeStats)
            {
                ImGui.TextUnformatted($"{name + ":",-20} {time.TotalSeconds:F5}s");
            }
#endif
        }

        ImGui.End();
    }

    private void DrawDebugUI()
    {
        if (ImGui.IsKeyPressed(ImGuiKey.F12, false))
        {
            ShowDebugger = !ShowDebugger;
        }

        if (ShowDebugger)
        {
            ImGui.Begin("Debug", ref ShowDebugger);

            if (ImGui.BeginTabBar("MainBar"))
            {
                if (ImGui.BeginTabItem("Draw Settings"))
                {
                    ImGui.Checkbox("Draw Chunk borders", ref DebugDrawChunkBorders);
                    ImGui.Checkbox("Draw PhysicsObject borders", ref DebugDrawPhysicsObjectBorders);
                    ImGui.Checkbox("Draw PixelScene borders", ref DebugDrawPixelSceneBorders);
                    ImGui.Checkbox("Draw PixelSpriteComponent borders", ref DebugDrawPixelSpriteComponentBorders);
                    ImGui.Checkbox("Draw SpriteComponent borders", ref DebugDrawSpriteComponentBorders);

                    ImGui.Checkbox("Force PhysicsObject no framebuffer", ref ChunkContainer.ForceNoFrambuffer);

                    ImGui.EndTabItem();
                }

                if (ImGui.BeginTabItem("Test"))
                {
                    ImGui.EndTabItem();
                }

                ImGui.EndTabBar();
            }

            ImGui.End();
        }

        ImDrawListPtr drawList = ImGui.GetBackgroundDrawList();

        if (DebugDrawChunkBorders)
        {
            foreach (Chunk chunk in ChunkContainer.Chunks)
            {
                Matrix4x4 mat = chunk.WorldMatrix * Renderer.View;

                Vector2 p0 = Vector2.Transform(Vector2.Zero, mat);
                Vector2 p1 = Vector2.Transform(Vector2.UnitY, mat);
                Vector2 p2 = Vector2.Transform(Vector2.One, mat);
                Vector2 p3 = Vector2.Transform(Vector2.UnitX, mat);

                drawList.AddQuad(p0, p1, p2, p3, Color.Aqua.ToPixel<Rgba32>().PackedValue, 4f);
            }
        }

        if (DebugDrawPhysicsObjectBorders)
        {
            foreach (PhysicsObject physicsObject in ChunkContainer.PhysicsObjects)
            {
                Matrix4x4 mat = physicsObject.WorldMatrix * Renderer.View;

                Vector2 p0 = Vector2.Transform(Vector2.Zero, mat);
                Vector2 p1 = Vector2.Transform(Vector2.UnitY, mat);
                Vector2 p2 = Vector2.Transform(Vector2.One, mat);
                Vector2 p3 = Vector2.Transform(Vector2.UnitX, mat);

                drawList.AddQuad(p0, p1, p2, p3, Color.Pink.ToPixel<Rgba32>().PackedValue, 4f);
            }
        }

        if (DebugDrawPixelSceneBorders)
        {
            foreach (PixelScene pixelScene in WorldPixelScenes.PixelScenes)
            {
                Matrix4x4 mat = pixelScene.WorldMatrix * Renderer.View;

                Vector2 p0 = Vector2.Transform(Vector2.Zero, mat);
                Vector2 p1 = Vector2.Transform(Vector2.UnitY, mat);
                Vector2 p2 = Vector2.Transform(Vector2.One, mat);
                Vector2 p3 = Vector2.Transform(Vector2.UnitX, mat);

                drawList.AddQuad(p0, p1, p2, p3, Color.Pink.ToPixel<Rgba32>().PackedValue, 4f);
            }
        }

        if (DebugDrawPixelSpriteComponentBorders)
        {
            foreach (PixelSpriteComponent pixelSprite in EntityContainer.PixelSprites)
            {
                Matrix4x4 mat = pixelSprite.WorldMatrix * Renderer.View;

                Vector2 p0 = Vector2.Transform(Vector2.Zero, mat);
                Vector2 p1 = Vector2.Transform(Vector2.UnitY, mat);
                Vector2 p2 = Vector2.Transform(Vector2.One, mat);
                Vector2 p3 = Vector2.Transform(Vector2.UnitX, mat);

                if (pixelSprite.ImageFile is null)
                {
                    drawList.AddQuad(p0, p1, p2, p3, Color.Red.ToPixel<Rgba32>().PackedValue, 4f);
                }
                else
                {
                    drawList.AddQuad(p0, p1, p2, p3, Color.Lime.ToPixel<Rgba32>().PackedValue, 4f);
                }
            }
        }

        if (DebugDrawSpriteComponentBorders)
        {
            foreach (SpriteComponent sprite in EntityContainer.RegularSprites)
            {
                Matrix4x4 mat = sprite.WorldMatrix * Renderer.View;

                Vector2 p0 = Vector2.Transform(Vector2.Zero, mat);
                Vector2 p1 = Vector2.Transform(Vector2.UnitY, mat);
                Vector2 p2 = Vector2.Transform(Vector2.One, mat);
                Vector2 p3 = Vector2.Transform(Vector2.UnitX, mat);

                Color color = Color.Orange;

                drawList.AddQuad(p0, p1, p2, p3, color.ToPixel<Rgba32>().PackedValue, 4f);
            }
        }
    }

    private void DrawSearchUI()
    {
        if ((ImGui.IsKeyDown(ImGuiKey.LeftCtrl) || ImGui.IsKeyDown(ImGuiKey.RightCtrl)) && ImGui.IsKeyPressed(ImGuiKey.F, false))
        {
            ShowSearch = !ShowSearch;
        }

        if (!ShowSearch)
        {
            return;
        }

        ImGui.Begin("Search", ref ShowSearch);

        ImGui.Text("Search:");

        ImGui.InputText("##search", ref SearchText, 2048);

        if (ImGui.BeginChild("SearchScrollableChild"))
        {
            ImGui.Text("Results for PixelScene:");
            ImGui.Indent(6f);
            for (int i = 0; i < WorldPixelScenes.PixelScenes.Count; i++)
            {
                PixelScene pixelScene = WorldPixelScenes.PixelScenes[i];

                if (SearchText.Length == 0)
                {
                    break;
                }

                bool found = 
                (pixelScene.BackgroundFilename?.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ?? false)
                || (pixelScene.ColorsFilename?.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ?? false)
                || (pixelScene.MaterialFilename?.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ?? false);

                if (found)
                {
                    if (ImGui.Selectable(
                        $"""
                        PixelScene:
                            BackgroundFilename: {pixelScene.BackgroundFilename}
                            ColorsFilename: {pixelScene.ColorsFilename}
                            MaterialFilename: {pixelScene.MaterialFilename}
                        ###pixelscene{i}
                        """))
                    {
                        Renderer.ViewOffset = 
                            new Vector2(pixelScene.X, pixelScene.Y)
                            - (ImGui.GetIO().DisplaySize / 2f) / Renderer.ViewScale
                            + (new Vector2(pixelScene.TextureWidth, pixelScene.TextureHeight) / 2f);
                    }
                }
            }
            ImGui.Unindent(6f);

            ImGui.Text("Results for Entities:");
            ImGui.Indent(6f);
            for (int i = 0; i < EntityContainer.Entities.Count; i++)
            {
                Entity entity = EntityContainer.Entities[i];

                if (SearchText.Length == 0)
                {
                    break;
                }

                bool found =
                (entity.Name?.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ?? false)
                || (entity.Tags?.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ?? false)
                || (entity.FileName?.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ?? false);

                if (found)
                {
                    if (ImGui.Selectable(
                        $"""
                        Entity:
                            Name: {entity.Name}
                            Tags: {entity.Tags}
                            FileName: {entity.FileName}
                        ###entity{i}
                        """))
                    {
                        Renderer.ViewOffset =
                            entity.Position
                            - (ImGui.GetIO().DisplaySize / 2f) / Renderer.ViewScale;
                    }
                }
            }
            ImGui.Unindent(6f);
            ImGui.EndChild();
        }

        ImGui.End();
    }

    private static bool IsPointInQuad(Vector2 point, Vector2 p0, Vector2 p1, Vector2 p2, Vector2 p3)
    {
        Vector2 e0 = p1 - p0;
        Vector2 e1 = p2 - p1;
        Vector2 e2 = p3 - p2;
        Vector2 e3 = p0 - p3;

        Vector2 v0 = point - p0;
        Vector2 v1 = point - p1;
        Vector2 v2 = point - p2;
        Vector2 v3 = point - p3;

        float dot0 = Vector2.Dot(e0, v0);
        float dot1 = Vector2.Dot(e1, v1);
        float dot2 = Vector2.Dot(e2, v2);
        float dot3 = Vector2.Dot(e3, v3);

        return (dot0 >= 0 && dot1 >= 0 && dot2 >= 0 && dot3 >= 0) || (dot0 <= 0 && dot1 <= 0 && dot2 <= 0 && dot3 <= 0);
    }
}
