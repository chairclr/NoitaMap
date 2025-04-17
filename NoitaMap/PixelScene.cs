using SixLabors.ImageSharp.PixelFormats;

namespace NoitaMap;

public class PixelScene : INoitaSerializable
{
    public int X;
    public int Y;

    public string? ColorsFilename;
    public string? MaterialFilename;
    public string? BackgroundFilename;

    public bool SkipBiomeChecks;
    public bool SkipEdgeTextures;

    public int BackgroundZIndex;

    public string? JustLoadAnEntity;

    public bool CleanAreaBefore;
    public bool DebugReloadMe;

    public Dictionary<Rgba32, int> ColorMaterials = [];

    public PixelScene()
    {

    }

    public void Deserialize(BinaryReader reader)
    {
        X = reader.ReadBEInt32();
        Y = reader.ReadBEInt32();

        MaterialFilename = reader.ReadNoitaString();
        ColorsFilename = reader.ReadNoitaString();
        BackgroundFilename = reader.ReadNoitaString();

        SkipBiomeChecks = reader.ReadBoolean();
        SkipEdgeTextures = reader.ReadBoolean();

        BackgroundZIndex = reader.ReadBEInt32();

        JustLoadAnEntity = reader.ReadNoitaString();

        CleanAreaBefore = reader.ReadBoolean();
        DebugReloadMe = reader.ReadBoolean();

        uint colorMaterialCount = reader.ReadBEUInt32();
        for (int i = 0; i < colorMaterialCount; i++)
        {
            Rgba32 color = new(reader.ReadUInt32());

            int cellType = reader.ReadBEInt32();

            ColorMaterials[color] = cellType;
        }
    }

    public void Serialize(BinaryWriter writer)
    {
        writer.WriteBE(X);
        writer.WriteBE(Y);

        writer.WriteNoitaString(BackgroundFilename);
        writer.WriteNoitaString(ColorsFilename);
        writer.WriteNoitaString(MaterialFilename);

        writer.Write(SkipBiomeChecks);
        writer.Write(SkipEdgeTextures);

        writer.WriteBE(BackgroundZIndex);

        writer.WriteNoitaString(JustLoadAnEntity);

        writer.Write(CleanAreaBefore);
        writer.Write(DebugReloadMe);

        writer.WriteBE(ColorMaterials.Count);
        foreach ((Rgba32 color, int cellType) in ColorMaterials)
        {
            writer.WriteBE(color.PackedValue);
            writer.WriteBE(cellType);
        }
    }

    public class BackgroundImage : INoitaSerializable
    {
        public int X;
        public int Y;

        public string? Filename;

        public BackgroundImage()
        {

        }

        public void Deserialize(BinaryReader reader)
        {
            X = reader.ReadBEInt32();
            Y = reader.ReadBEInt32();

            Filename = reader.ReadNoitaString();
        }

        public void Serialize(BinaryWriter writer)
        {
            writer.WriteBE(X);
            writer.WriteBE(Y);

            writer.WriteNoitaString(Filename);
        }
    }
}