# Noita File
Most of Noita's game save data is compressed (using [fastlz](https://github.com/ariya/FastLZ)) and stored in a simple custom file format

Files that are compressed like this include:

world_x_y.png_petri
world_pixel_scenes.bin
entities_x.bin
area_x.bin

They also probably include world_tree.bin and world_sim.bin, but I haven't checked

## Format

```
LE Int32    compressed data size
LE Int32    uncompressed data size
byte[]      compressed data
```
