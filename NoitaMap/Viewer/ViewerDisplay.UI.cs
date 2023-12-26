using System.Numerics;
using ImGuiNET;
using NoitaMap.Map;
using NoitaMap.Map.Components;

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

    private void DrawUI()
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

        if (ImGui.IsKeyPressed(ImGuiKey.F12, false))
        {
            ShowDebugger = !ShowDebugger;
        }

        if (ShowDebugger)
        {
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

                ImGui.EndTabBar();
            }
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
