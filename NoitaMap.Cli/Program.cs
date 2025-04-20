using System;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading;
using NoitaMap;
using NoitaMap.Logging;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

internal class Program
{
    private static void Main(string[] args)
    {
        if (args.Length == 0)
        {
            throw new NotImplementedException();
        }

        if (args.Length == 1)
        {
            throw new NotImplementedException();
        }

        string verb = args[0];

        if (verb == "dump")
        {
            string path = args[1];

            string filename = Path.GetFileName(path);

            if (filename == ".stream_info")
            {
                byte[] bytes = NoitaFile.LoadCompressedFile(path);

                using MemoryStream ms = new(bytes);
                using BinaryReader reader = new(ms);

                StreamInfo streamInfo = new();
                streamInfo.Deserialize(reader);

                Log.LogInfo("Deserialized stream info");

                Log.LogInfo($"Seed: {streamInfo.Seed}");
                Log.LogInfo($"FramesPlayed: {streamInfo.FramesPlayed}");
                Log.LogInfo($"SecondsPlayed: {streamInfo.SecondsPlayed}");
                Log.LogInfo($"UnknownCounter: {streamInfo.UnknownCounter}");
                Log.LogInfo($"SchemaHash: {streamInfo.SchemaHash}");
                Log.LogInfo($"GameModeIndex: {streamInfo.GameModeIndex}");
                Log.LogInfo($"GameModeName: {streamInfo.GameModeName}");
                Log.LogInfo($"GameModeSteamId: {streamInfo.GameModeSteamId}");
                Log.LogInfo($"NonNollaModUsed: {streamInfo.NonNollaModUsed}");
                Log.LogInfo($"SaveAndQuitTime: {streamInfo.SaveAndQuitTime}");
                Log.LogInfo($"NewGameUIName: {streamInfo.NewGameUIName}");
                Log.LogInfo($"UnknownCameras: {streamInfo.UnknownCamera1}, {streamInfo.UnknownCamera2}, {streamInfo.UnknownCamera3}, {streamInfo.UnknownCamera4}");

                Log.LogInfo("Backgrounds:");
                foreach (StreamInfo.Background bg in streamInfo.Backgrounds)
                {
                    Log.LogInfo($"at ({bg.Position}): {bg.Filename}");
                }

                Log.LogInfo("ChunkLoadInfo:");
                foreach (StreamInfo.ChunkLoadedInfo chunk in streamInfo.ChunkLoadInfo)
                {
                    Log.LogInfo($"Loaded at ({chunk.X}, {chunk.Y}): {chunk.Loaded}");
                }

                return;
            }

            if (filename == "world_pixel_scenes.bin")
            {
                byte[] bytes = NoitaFile.LoadCompressedFile(path);

                using MemoryStream ms = new(bytes);
                using BinaryReader reader = new(ms);

                WorldPixelScenes pixelScenes = new();
                pixelScenes.Deserialize(reader);

                Log.LogInfo("Deserialized world pixel scenes");

                Log.LogInfo("PendingPixelScenes:");
                foreach (PixelScene pixelScene in pixelScenes.PendingPixelScenes)
                {
                    Log.LogInfo($"at ({pixelScene.X}, {pixelScene.Y}): col {pixelScene.ColorsFilename}; mat {pixelScene.MaterialFilename}; bg {pixelScene.BackgroundFilename};");
                }

                Log.LogInfo("PlacedPixelScenes:");
                foreach (PixelScene pixelScene in pixelScenes.PendingPixelScenes)
                {
                    Log.LogInfo($"at ({pixelScene.X}, {pixelScene.Y}): col {pixelScene.ColorsFilename}; mat {pixelScene.MaterialFilename}; bg {pixelScene.BackgroundFilename};");
                }

                Log.LogInfo("BackgroundImages:");
                foreach (PixelScene.BackgroundImage backgroundImage in pixelScenes.BackgroundImages)
                {
                    Log.LogInfo($"at ({backgroundImage.X}, {backgroundImage.Y}): {backgroundImage.Filename}");
                }

                return;
            }

            Regex regex = new Regex("world_(?<x>-?\\d+)_(?<y>-?\\d+)\\.png_petri");
            if (filename.StartsWith("world_") && regex.IsMatch(filename))
            {
                Match match = regex.Match(filename);

                int cx = int.Parse(match.Groups["x"].Value);
                int cy = int.Parse(match.Groups["y"].Value);

                MaterialProvider mp = new MaterialProvider();

                byte[] bytes = NoitaFile.LoadCompressedFile(path);

                using MemoryStream ms = new(bytes);
                using BinaryReader reader = new(ms);

                Chunk chunk = new Chunk(cx, cy, mp);
                chunk.Deserialize(reader);

                Log.LogInfo("Deserialized chunk");

                Rgba32[,] pixelData = chunk.GetPixelData();

                using Image<Rgba32> chunkImage = new(512, 512, Color.Aqua);

                for (int x = 0; x < 512; x++)
                {
                    for (int y = 0; y < 512; y++)
                    {
                        chunkImage[y, x] = pixelData[x, y];
                    }
                }

                Log.LogInfo($"Saving chunk cells as {filename + ".png"}");

                chunkImage.SaveAsPng(filename + ".png");

                Log.LogInfo("PhysicsObjects:");
                foreach (PhysicsObject physicsObject in chunk.PhysicsObjects)
                {
                    Log.LogInfo($"at {physicsObject.Position}, theta = {physicsObject.Rotation}");
                }
            }
        }
    }
}