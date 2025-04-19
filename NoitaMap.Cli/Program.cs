using System;
using System.IO;
using System.Threading;
using System.Text.RegularExpressions;
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

            if (filename == ".streaminfo")
            {
                throw new NotImplementedException();
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

                Chunk chunk = new Chunk(cx, cy, mp);

                byte[] bytes = NoitaFile.LoadCompressedFile(path);

                using MemoryStream ms = new(bytes);
                using BinaryReader reader = new(ms);

                chunk.Deserialize(reader);

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