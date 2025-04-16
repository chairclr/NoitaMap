﻿using System.Numerics;
using CommunityToolkit.HighPerformance;
using NoitaMap.Graphics;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace NoitaMap;

public class PixelScene : IAtlasObject
{
    public int X;

    public int Y;

    public string? BackgroundFilename;

    public string? ColorsFilename;

    public string? MaterialFilename;

    public bool SkipBiomeChecks;

    public bool SkipEdgeTextures;

    public int BackgroundZIndexProbably;

    public string? JustLoadAnEntity;

    public bool Unknown2;

    public bool Unknown3;

    public bool Unknown4;

    public bool Unknown5;

    public byte Unknown6;

    public byte ExtraUnknownsCount;

    public List<ulong> ExtraUnknowns = new List<ulong>();

    public Matrix4x4 WorldMatrix { get; set; }

    public Rgba32[,]? WorkingTextureData { get; set; }

    public int TextureWidth { get; set; }

    public int TextureHeight { get; set; }

    public int TextureHash { get; set; }

    public PixelScene()
    {

    }

    public void Deserialize(BinaryReader reader)
    {
        X = reader.ReadBEInt32();

        Y = reader.ReadBEInt32();

        BackgroundFilename = reader.ReadNoitaString();

        ColorsFilename = reader.ReadNoitaString();

        MaterialFilename = reader.ReadNoitaString();

        SkipBiomeChecks = reader.ReadBoolean();

        SkipEdgeTextures = reader.ReadBoolean();

        BackgroundZIndexProbably = reader.ReadBEInt32();

        JustLoadAnEntity = reader.ReadNoitaString();

        Unknown2 = reader.ReadBoolean();

        Unknown3 = reader.ReadBoolean();

        Unknown4 = reader.ReadBoolean();

        Unknown5 = reader.ReadBoolean();

        Unknown6 = reader.ReadByte();

        ExtraUnknownsCount = reader.ReadByte();

        for (int i = 0; i <  ExtraUnknownsCount; i++)
        {
            ExtraUnknowns.Add(reader.ReadBEUInt64());
        }

        string? path = null;
        if (MaterialFilename is not null)
        {
            if (MaterialFilename.StartsWith("data/"))
            {
                path = Path.Combine(PathService.DataPath!, MaterialFilename.Remove(0, 5));
            }

            if (File.Exists(path))
            {
                TextureHash = path.GetHashCode();

                using Image<Rgba32> image = ImageUtility.LoadImage(path);

                WorkingTextureData = new Rgba32[image.Width, image.Height];

                TextureWidth = image.Width;

                TextureHeight = image.Height;

                image.CopyPixelDataTo(WorkingTextureData.AsSpan());
            }
        }

        WorldMatrix = Matrix4x4.CreateScale(TextureWidth, TextureHeight, 1f) * Matrix4x4.CreateTranslation(X, Y, 0f);
    }
}
