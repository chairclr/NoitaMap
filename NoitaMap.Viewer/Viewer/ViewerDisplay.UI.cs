using System.Numerics;
using ImGuiNET;
using NoitaMap.Graphics;
using NoitaMap.Map;
using NoitaMap.Map.Components;
using NoitaMap.Map.Entities;
using SixLabors.ImageSharp;

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

    private bool DebugDrawCurrentCell = false;

    private bool DebugPaint = false;

    private float BrushSize = 32f;

    private string MaterialName = "";

    private bool ShowSearch = false;

    private readonly HashSet<Chunk> ModifiedChunks = new HashSet<Chunk>();

    private bool DebugDrawAreaEntityBorders = false;

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
                    ImGui.Checkbox("Draw AreaEntity borders", ref DebugDrawAreaEntityBorders);
                    ImGui.Checkbox("Draw hovered cell", ref DebugDrawCurrentCell);
                    ImGui.Checkbox("Paint", ref DebugPaint);

                    if (DebugPaint)
                    {
                        ImGui.SliderFloat("Brush size", ref BrushSize, 1f, 512f * 10f);

                        if (BrushSize <= 0f)
                        {
                            BrushSize = 0f;
                        }


                        ImGui.InputText("Material", ref MaterialName, 256);

                        if (!ChunkContainer.MaterialProvider.Material.TryGetValue(MaterialName, out _) && MaterialName.Length > 0)
                        {
                            (string closestMat, _) = ChunkContainer.MaterialProvider.Material.OrderByDescending(x => x.Key.CompareTo(MaterialName)).FirstOrDefault();

                            ImGui.TextDisabled(closestMat);
                        }

                        if (ImGui.Button("Save chunks"))
                        {
                            using MemoryStream ms = new MemoryStream();
                            using BinaryWriter writer = new BinaryWriter(ms);

                            foreach (Chunk chunk in ModifiedChunks)
                            {
                                ms.Position = 0;
                                chunk.Serialize(writer);

                                NoitaFile.WriteCompressedFile($"{PathService.WorldPath}/world_{chunk.Position.X}_{chunk.Position.Y}.png_petri", ms.GetBuffer().AsSpan()[..(int)ms.Position]);
                            }

                            ModifiedChunks.Clear();
                        }
                    }

                    ImGui.Checkbox("Force PhysicsObject no framebuffer", ref ChunkContainer.ForceNoFrambuffer);

                    ImGui.EndTabItem();
                }

                if (ImGui.BeginTabItem("Test"))
                {
                    if (ImGui.Button("Test edit chunk"))
                    {
                        Chunk chunk = ChunkContainer.Chunks.Single(x => x.Position == Vector2.Zero);

                        Material mat = ChunkContainer.MaterialProvider.GetMaterial("steel_static");

                        for (int x = 0; x < 100; x++)
                            for (int y = 0; y < 100; y++)
                            {
                                chunk.SetPixel(x, y, mat);
                            }

                        ChunkContainer.InvalidateChunk(chunk);

                        using MemoryStream ms = new MemoryStream();
                        using BinaryWriter writer = new BinaryWriter(ms);
                        chunk.Serialize(writer);

                        NoitaFile.WriteCompressedFile($"{PathService.WorldPath}/world_0_0.png_petri", ms.GetBuffer()[..(int)ms.Position]);
                    }



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

        if (DebugDrawAreaEntityBorders)
        {
            foreach (AreaEntitySprite sprite in AreaContainer.AreaEntitySprites)
            {
                Matrix4x4 mat = sprite.WorldMatrix * Renderer.View;

                Vector2 p0 = Vector2.Transform(Vector2.Zero, mat);
                Vector2 p1 = Vector2.Transform(Vector2.UnitY, mat);
                Vector2 p2 = Vector2.Transform(Vector2.One, mat);
                Vector2 p3 = Vector2.Transform(Vector2.UnitX, mat);

                Color color = Color.GreenYellow;

                drawList.AddQuad(p0, p1, p2, p3, color.ToPixel<Rgba32>().PackedValue, 4f);
            }
        }

        if (DebugDrawCurrentCell)
        {
            Matrix4x4.Invert(Renderer.View, out Matrix4x4 inverse);
            Vector2 v = Vector2.Transform(InputSystem.MousePosition, inverse);

            if (ChunkContainer.TryGetChunk(v, out Chunk? chunk))
            {
                int x = ((int)v.X) % 512;
                int y = ((int)v.Y) % 512;

                if (x < 0) x = 512 + x;
                if (y < 0) y = 512 + y;

                Material mat = chunk.GetPixel(y, x);

                drawList.AddText(InputSystem.MousePosition, Color.White.ToPixel<Rgba32>().PackedValue, $"({float.Floor(v.X)}, {float.Floor(v.Y)}) {mat.Name}");
            }
        }

        if (DebugPaint)
        {

            drawList.AddCircleFilled(InputSystem.MousePosition, BrushSize * Renderer.ViewScale.X, Color.Red.WithAlpha(0.3f).ToPixel<Rgba32>().PackedValue);

            if (ChunkContainer.MaterialProvider.Material.TryGetValue(MaterialName, out Material? mat))
            {
                Matrix4x4.Invert(Renderer.View, out Matrix4x4 inverse);
                Vector2 v = Vector2.Transform(InputSystem.MousePosition, inverse);

                List<Chunk> chunksToEdit = new List<Chunk>();

                for (float x = -BrushSize - 512f; x < BrushSize + 512f; x += 512f)
                {
                    for (float y = -BrushSize - 512f; y < BrushSize + 512f; y += 512f)
                    {
                        if (ChunkContainer.TryGetChunk(v + new Vector2(x, y), out Chunk? chunk))
                        {
                            chunksToEdit.Add(chunk);
                        }
                    }
                }

                if (InputSystem.RightMouseDown)
                {
                    foreach (Chunk chunk in chunksToEdit)
                    {
                        Vector2 p = v - chunk.Position;

                        chunk.SetBulkCircle((int)p.X, (int)p.Y, BrushSize, mat);

                        ChunkContainer.InvalidateChunk(chunk);

                        ModifiedChunks.Add(chunk);
                    }
                }
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