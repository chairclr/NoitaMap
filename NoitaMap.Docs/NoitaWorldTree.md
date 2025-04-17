# Noita World Tree
The world_tree.bin file contains.. I have no idea. It seems to be related to world gen and is loaded by like world_broad.cpp


## Noita World Tree File Format

```
BE int      version, should always be 0

temp[]      not sure what this represents, but goes to end of file

struct temp
{
    BE int  some index or world chunk index, seems to just count up
    byte[64] fixed array of 64 bytes, seems to mostly be 0xFF but randomly changes
}
