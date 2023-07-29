using System.Diagnostics;

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

    public int BackgroundZIndexProbably;

    public string? JustLoadAnEntity;

    public bool Unknown2;
    
    public bool Unknown3;

    public bool Unknown4;

    public bool Unknown5;

    public byte Unknown6;

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

        BackgroundZIndexProbably = reader.ReadBEInt32();

        JustLoadAnEntity = reader.ReadNoitaString();

        Unknown2 = reader.ReadBoolean();

        Unknown3 = reader.ReadBoolean();

        Unknown4 = reader.ReadBoolean();

        Unknown5 = reader.ReadBoolean();

        Unknown6 = reader.ReadByte();

        IsThereExtraUnknown = reader.ReadBoolean();

        if (IsThereExtraUnknown)
        {
            ExtraUnknown = reader.ReadBEUInt64();
        }
    }
}
