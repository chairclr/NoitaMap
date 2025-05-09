# Noita File
Most of Noita's game save data is compressed (using [fastlz](https://github.com/ariya/FastLZ)) and stored in a simple custom file format

Files that are compressed like this include:

world_x_y.png_petri
entities_x.bin
area_x.bin
world_pixel_scenes.bin
world_tree.bin
world_sim.bin
.streaminfo

## Format

```
LE int      compressed data size
LE int      uncompressed data size
byte[]      compressed data
```

## Common type formats/patterns
`string`s are length encoded ascii/utf8? strings:
```
int         string length
byte[len]   chars
```

most `array`s are length encoded contiguous blocks of memory:
```
int         array length
T[len]      elements
```


Most other data types are big endian but otherwise as expected

## Documented file formats

[world_x_y.png_petri/chunk files](NoitaChunk.md) contain the pixel data and physics objects of each world chunk

[world_pixel_scenes.bin files](NoitaPixelScenes.md) contain the background images and information the game uses to load structure materials from images

the [.streaminfo file](NoitaStreamInfo.md) contains information such as the world seed, number of frames in the world, the schema used, game modes, and more.

[world_tree.bin file](NoitaWorldTree.md) contain broad/estimate informating describing how many cells are in each part of the world.

## Undocumented file formats

world_sim.bin contains information used in the verlet simulation for vines and other objects

