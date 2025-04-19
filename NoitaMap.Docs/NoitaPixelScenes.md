# Noita Pixel Scenes
Pixel Scenes make up custom backgrounds in the world, such as those behind holy mountains, the tree, avarice diamond, initial cave enterance, and more

They also are used by the game to generate complex material structures from images


## Pixel Scene File Format

```
int         version, should always be 3
int         magic num, should always be 0x2F0AA9F

PixelScene[] pending list
PixelScene[] placed list
PixelSceneBackgroundImage[] background images

struct PixelScene
{
    int     x
    int     y
    string  material filename
    string  color filename
    string  background filename
    bool    skip biome checks
    bool    skip edge textures
    int     background z index
    string  just load an entity, empty when there's nothing to load I guess
    bool    clean area before

    ColorMaterial[] color materials, see below

    struct ColorMaterial 
    {
        Rgba32 color
        int cell type
    }
}

struct PixelSceneBackgroundImage
{
    int     x
    int     y
    string  filename
}
```
