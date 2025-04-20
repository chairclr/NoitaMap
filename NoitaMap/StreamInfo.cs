using System.Numerics;

namespace NoitaMap;

public class StreamInfo : INoitaSerializable
{
    public uint Seed;

    public uint FramesPlayed;
    public float SecondsPlayed;

    public ulong UnknownCounter;

    public List<Background> Backgrounds = [];

    public string SchemaHash = "TODO";

    public int GameModeIndex;
    public string? GameModeName;
    public long GameModeSteamId;

    public bool NonNollaModUsed;

    public DateTime SaveAndQuitTime;

    public string? NewGameUIName;

    public int UnknownCamera1;
    public int UnknownCamera2;
    public int UnknownCamera3;
    public int UnknownCamera4;

    public List<ChunkLoadedInfo> ChunkLoadInfo = [];

    public void Deserialize(BinaryReader reader)
    {
        Seed = reader.ReadBEUInt32();

        FramesPlayed = reader.ReadBEUInt32();
        SecondsPlayed = reader.ReadBESingle();

        UnknownCounter = reader.ReadBEUInt64();

        int backgroundCount = reader.ReadBEInt32();
        for (int i = 0; i < backgroundCount; i++)
        {
            Background bg = new();
            bg.Deserialize(reader);

            Backgrounds.Add(bg);
        }

        // For some reason, chunk count is here, not at the bottom...?
        int chunkCount = reader.ReadBEInt32();

        SchemaHash = reader.ReadNoitaString() ?? "WHAT";

        GameModeIndex = reader.ReadBEInt32();
        GameModeName = reader.ReadNoitaString();
        GameModeSteamId = reader.ReadBEInt64();

        NonNollaModUsed = reader.ReadBoolean();

        SaveAndQuitTime = new DateTime(reader.ReadBEUInt16(), reader.ReadBEUInt16(), reader.ReadBEUInt16(), reader.ReadBEUInt16(), reader.ReadBEUInt16(), reader.ReadBEUInt16());

        NewGameUIName = reader.ReadNoitaString();

        UnknownCamera1 = reader.ReadBEInt32();
        UnknownCamera2 = reader.ReadBEInt32();
        UnknownCamera3 = reader.ReadBEInt32();
        UnknownCamera4 = reader.ReadBEInt32();

        for (int i = 0; i < chunkCount; i++)
        {
            ChunkLoadedInfo info = new();
            info.Deserialize(reader);

            ChunkLoadInfo.Add(info);
        }
    }

    public void Serialize(BinaryWriter writer)
    {
        writer.WriteBE(Seed);

        writer.WriteBE(FramesPlayed);
        writer.WriteBE(SecondsPlayed);

        writer.WriteBE(UnknownCounter);

        writer.WriteBE(Backgrounds.Count);
        foreach (Background bg in Backgrounds)
        {
            bg.Serialize(writer);
        }

        // For some reason, chunk count is here, not at the bottom...?
        writer.WriteBE(ChunkLoadInfo.Count);

        writer.WriteNoitaString(SchemaHash);

        writer.WriteBE(GameModeIndex);
        writer.WriteNoitaString(GameModeName);
        writer.WriteBE(GameModeSteamId);

        writer.Write(NonNollaModUsed);

        writer.WriteBE((ushort)SaveAndQuitTime.Year);
        writer.WriteBE((ushort)SaveAndQuitTime.Month);
        writer.WriteBE((ushort)SaveAndQuitTime.Day);
        writer.WriteBE((ushort)SaveAndQuitTime.Hour);
        writer.WriteBE((ushort)SaveAndQuitTime.Minute);
        writer.WriteBE((ushort)SaveAndQuitTime.Second);

        writer.WriteNoitaString(NewGameUIName);

        writer.WriteBE(UnknownCamera1);
        writer.WriteBE(UnknownCamera2);
        writer.WriteBE(UnknownCamera3);
        writer.WriteBE(UnknownCamera4);

        foreach (ChunkLoadedInfo info in ChunkLoadInfo)
        {
            info.Serialize(writer);
        }
    }

    public class Background : INoitaSerializable
    {
        public Vector2 Position;

        public string? Filename;

        public float ZIndex;

        public Vector2 Offset;

        public void Deserialize(BinaryReader reader)
        {
            Position = new Vector2(reader.ReadBESingle(), reader.ReadBESingle());

            Filename = reader.ReadNoitaString();

            ZIndex = reader.ReadBESingle();

            Offset = new Vector2(reader.ReadBESingle(), reader.ReadBESingle());
        }

        public void Serialize(BinaryWriter writer)
        {
            writer.WriteBE(Position.X);
            writer.WriteBE(Position.Y);

            writer.WriteNoitaString(Filename);

            writer.WriteBE(ZIndex);

            writer.WriteBE(Offset.X);
            writer.WriteBE(Offset.Y);
        }
    }

    public class ChunkLoadedInfo : INoitaSerializable
    {
        public int X;
        public int Y;

        public bool Loaded;

        public void Deserialize(BinaryReader reader)
        {
            X = reader.ReadBEInt32();
            Y = reader.ReadBEInt32();

            Loaded = reader.ReadBoolean();
        }

        public void Serialize(BinaryWriter writer)
        {
            writer.WriteBE(X);
            writer.WriteBE(Y);

            writer.Write(Loaded);
        }
    }
}