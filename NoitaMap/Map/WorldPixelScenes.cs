using NoitaMap.Viewer;

namespace NoitaMap.Map;

// String format:
// int length
// byte[length] text

// PixelScene format:
// int x, int y
// string background_filename
// string colors_filename
// string material_filename
// bool skip_biome_checks
// bool skip_edge_textures
// int unknown (should be 50?)
// string just_load_an_entity
// int unknown (should be 0?)
// byte unknown
// bool isThereExtraUnknown
// if (isThereExtraUnknown)
//     ulong extraUnknown (probably for puzzles/entity for that pixel scene?)

// Pixel scenes file structure (mBufferedPixelScenes[]?):
// int version (should be 3)
// short unknown
// short unknown
// int length
// PixelScene[length] scenes

public class WorldPixelScenes
{
    private ViewerDisplay ViewerDisplay;

    public WorldPixelScenes(ViewerDisplay viewerDisplay)
    {
        ViewerDisplay = viewerDisplay;
    }

    public void Load(string path)
    {
        byte[]? decompressedData = NoitaDecompressor.ReadAndDecompressChunk(path);

        using (MemoryStream ms = new MemoryStream(decompressedData))
        {
            using BinaryReader reader = new BinaryReader(ms);

            int version = reader.ReadBEInt32();

            if (version != 3)
            {
                throw new Exception($"Burger wasn't 3 (it was {version})");
            }

            short unknown1 = reader.ReadBEInt16();
            short unknown2 = reader.ReadBEInt16();
            int length = reader.ReadBEInt32();

            PixelScene[] pixelScenes = new PixelScene[length];

            for (int i = 0; i < pixelScenes.Length; i++)
            {
                pixelScenes[i] = new PixelScene();

                Console.WriteLine($"Stream Position: {reader.BaseStream.Position:X}");

                pixelScenes[i].Deserialize(reader);

                Console.WriteLine($"{i}: {pixelScenes[i].X}, {pixelScenes[i].Y}, {pixelScenes[i].BackgroundFilename}, {pixelScenes[i].ColorsFilename}, {pixelScenes[i].MaterialFilename}, {pixelScenes[i].SkipBiomeChecks}, {pixelScenes[i].SkipEdgeTextures}, {pixelScenes[i].JustLoadAnEntity}");
                Console.WriteLine();
            }

            int length2 = reader.ReadBEInt32();
            PixelScene[] pixelScenes2 = new PixelScene[length2];

            for (int i = 0; i < pixelScenes2.Length; i++)
            {
                pixelScenes2[i] = new PixelScene();

                Console.WriteLine($"Stream Position: {reader.BaseStream.Position:X}");

                pixelScenes2[i].Deserialize(reader);

                Console.WriteLine($"{i}: {pixelScenes2[i].X}, {pixelScenes2[i].Y}, {pixelScenes2[i].BackgroundFilename}, {pixelScenes2[i].ColorsFilename}, {pixelScenes2[i].MaterialFilename}, {pixelScenes2[i].SkipBiomeChecks}, {pixelScenes2[i].SkipEdgeTextures}, {pixelScenes2[i].JustLoadAnEntity}");
                Console.WriteLine();
            }
        }

        decompressedData = null;
    }
}
