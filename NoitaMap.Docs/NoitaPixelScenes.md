# Noita Pixel Scenes
Pixel Scenes make up custom backgrounds in the world, such as those behind holy mountains, the tree, avarice diamond, initial cave enterance, and more

They also are used by the game to generate complex material structures from images


## Pixel Scene File Format

```
BE int      version, should always be 3
BE int      magic num, should always be 0x2F0AA9F

PixelScene[] pending list
PixelScene[] placed list
PixelSceneBackgroundImage[] background images

struct PixelSceneBackgroundImage
{
    BE int x
    BE int y
    string filename
}
```

## Pixel Scene Format

```
BE int      x
BE int      y
string      material filename
string      color filename
string      background filename
bool        skip biome checks
bool        skip edge textures
BE int      background z index
string      just load an entity, empty when there's nothing to load I guess
bool        clean area before

ColorMaterial[] color materials, see below

struct ColorMaterial 
{
    Rgba32 color
    BE int cell type
}
```

