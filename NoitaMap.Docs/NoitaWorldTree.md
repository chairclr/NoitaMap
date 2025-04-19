# Noita World Tree
The world_tree.bin file contains data that's (probably) to mutlithreading or something

It is a sparse image, with a somewhat tree like structure

The larger, overall image is 256x256. Each pixel in the larger image corresponds to a normal 512x512 pixel chunk

Each pixel in the larger contains a 8x8 grid of bytes, corresponding to how many cells are in that area of the chunk

However, if you split a 512x512 chunk into 8x8 sections, each section could contain 4096 cells

So the value stored is a byte from 0 tom 255, which is a remapped value that is proportional to the number of cells in that section

tl;dr it's a memory efficient way of generally knowing how many cells are in a given area of a chunk/the world


## Noita World Tree File Format

```
BE int      version, should always be 0

ChunkEst[]  large image data

struct ChunkEst
{
    BE int  index into the 256x256 larger image. x = index % 256, y = index / 256
    byte[64] fixed array of bytes representing the data in the chunk. x = i * 8, y = i / 8
}
```

## World Tree Data -> Image
Here is some C#/psuedocode to visualize what is contained in this file

```
int version = reader.ReadBEInt32();
int count = reader.ReadBEInt32();

using Image<Rgba32> image = new(256 * 8, 256 * 8, Color.Black);

for (int i = 0; i < count; i++)
{
    int index = reader.ReadBEInt32();

    int bx = (index % 256) * 8;
    int by = (index / 256) * 8;

    byte[] alphaData = reader.ReadBytes(64);

    for (int j = 0; j < 64; j++)
    {
        byte alpha = alphaData[j];

        int ax = j % 8;
        int ay = j / 8;

        image[bx + ax, by + ay] = new Rgba32(alpha, alpha, alpha, 255);
    }
}

image.SaveAsPng("world_tree.png");
```
