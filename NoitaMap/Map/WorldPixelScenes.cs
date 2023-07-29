using NoitaMap.Graphics;
using NoitaMap.Viewer;
using Veldrid;

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
// int unknown
// int length
// PixelScene[length] scenes

public class WorldPixelScenes
{
    private ViewerDisplay ViewerDisplay;

    private PixelSceneAtlasBuffer PixelScenes;

    public WorldPixelScenes(ViewerDisplay viewerDisplay)
    {
        ViewerDisplay = viewerDisplay;

        PixelScenes = new PixelSceneAtlasBuffer(ViewerDisplay);
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

            for (int i = 0; i < length; i++)
            {
                PixelScene pixelScene = new PixelScene();

                pixelScene.Deserialize(reader);

                PixelScenes.AddPixelScene(pixelScene);
            }

            int length2 = reader.ReadBEInt32();

            for (int i = 0; i < length2; i++)
            {
                PixelScene pixelScene = new PixelScene();

                pixelScene.Deserialize(reader);

                PixelScenes.AddPixelScene(pixelScene);
            }
        }

        decompressedData = null;
    }

    public void Update()
    {
        PixelScenes.Update();
    }

    public void Draw(CommandList commandList)
    {
        PixelScenes.Draw(commandList);
    }
}
