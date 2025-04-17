# Noita Chunk
Chunks are the world cell/pixel data that make up the terrain

Chunks also include all of the physics bodies contained in them

Chunks support up to 128 materials and allow pixels to have custom colors

## Chunk Format

```
BE int      version, should always be 24
BE int      chunk width, should always be 512
BE int      chunk height, should alwayus be 512

byte[]      raw/unindexed cell data
string[]    list of the names of all materials in the cell, indexed into by the cell data
Rgba32[]    list of the custom colors of the cells, indexed into when the first bit of the raw cell data is set, and then increment the index

PhysicsObject[] all of the physics objects in the chunk. See PhysicsObject.md for more info

int         should always end in an extra zero int, not sure why
```

## Physics Object Format

```
byte[]      12 unknown bytes
float       pos.x
float       pos.y
float       rotation
byte[]      49 unknown bytes
int         pixels width
int         pixels height
Rgba32[]    raw pixel data for this physics object
```


## Cell data -> Cell color
Here is some pseudocode to decode cell data into raw pixel data:

```
for (int x = 0; x < ChunkSize; x++)
{
    for (int y = 0; y < ChunkSize; y++)
    {
        Cell = cells[x, y];

        int material = cell.MaterialIndex;

        if (cell.HasCustomColor)
        {
            pixelData[x, y] = cell.CustomColor;
        }
        else
        {
            // Air
            if (material == 0)
            {
                continue;
            }

            Material mat = materials[material];

            int wx = (x + (X * ChunkSize)) * 6;
            int wy = (y + (Y * ChunkSize)) * 6;

            int colorX = ((wx % Material.MaterialWidth) + Material.MaterialWidthM1) % Material.MaterialWidthM1;
            int colorY = ((wy % Material.MaterialHeight) + Material.MaterialHeightM1) % Material.MaterialHeightM1;

            pixelData[x, y] = mat.MaterialTexture[colorY, colorX];
        }
    }
}
```
