using NoitaMap.Logging;

namespace NoitaMap;

public class WorldPixelScenes : INoitaSerializable
{
    public int Version = 3;

    public uint MagicNumber = 0x02F0AA9F;

    public List<PixelScene> PendingPixelScenes = [];

    public List<PixelScene> PlacedPixelScenes = [];

    public List<PixelScene.BackgroundImage> BackgroundImages = [];

    public WorldPixelScenes()
    {

    }

    public void Deserialize(BinaryReader reader)
    {
        Version = reader.ReadBEInt32();

        if (Version != 3)
        {
            Log.LogCrit($"world_pixel_scenes.bin Version was {Version}");
            throw new InvalidDataException();
        }

        MagicNumber = reader.ReadBEUInt32();

        if (MagicNumber != 0x02F0AA9F)
        {
            Log.LogWarn($"world_pixel_scenes.bin MagicNumber was {MagicNumber}");
        }

        int pendingCount = reader.ReadBEInt32();
        for (int i = 0; i < pendingCount; i++)
        {
            PixelScene scene = new();
            scene.Deserialize(reader);

            PendingPixelScenes.Add(scene);
        }

        int placedCount = reader.ReadBEInt32();
        for (int i = 0; i < placedCount; i++)
        {
            PixelScene scene = new();
            scene.Deserialize(reader);

            PlacedPixelScenes.Add(scene);
        }

        int backgroundCount = reader.ReadBEInt32();
        for (int i = 0; i < backgroundCount; i++)
        {
            PixelScene.BackgroundImage backgroundImage = new();
            backgroundImage.Deserialize(reader);

            BackgroundImages.Add(backgroundImage);
        }
    }

    public void Serialize(BinaryWriter writer)
    {
        writer.WriteBE(Version);
        writer.WriteBE(MagicNumber);

        writer.WriteBE(PendingPixelScenes.Count);
        foreach (PixelScene scene in PendingPixelScenes)
        {
            scene.Serialize(writer);
        }

        writer.WriteBE(PlacedPixelScenes.Count);
        foreach (PixelScene scene in PlacedPixelScenes)
        {
            scene.Serialize(writer);
        }

        writer.WriteBE(BackgroundImages.Count);
        foreach (PixelScene.BackgroundImage backgroundImage in BackgroundImages)
        {
            backgroundImage.Serialize(writer);
        }
    }
}