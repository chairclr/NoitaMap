namespace NoitaMap.Map;

public class PixelScene
{
    public int X;

    public int Y;

    public string? BackgroundFilename;

    public string? ColorsFilename;

    public string? MaterialFilename;

    public bool SkipBiomeChecks;

    public bool SkipEdgeTextures;

    public int Unknown1;

    public string? JustLoadAnEntity;

    public int Unknown2;

    public byte Unknown3;

    public bool IsThereExtraUnknown;

    public ulong ExtraUnknown;

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

        Unknown1 = reader.ReadBEInt32();

        JustLoadAnEntity = reader.ReadNoitaString();

        Unknown2 = reader.ReadBEInt32();

        Unknown3 = reader.ReadByte();

        IsThereExtraUnknown = reader.ReadBoolean();

        if (IsThereExtraUnknown)
        {
            ExtraUnknown = reader.ReadBEUInt64();
        }
    }
}
