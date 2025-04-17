# Noita Stream Info
The .streaminfo file contains more global information about a save, with things like seed, frames played, schema used, game mode, and more


## Noita Stream Info file Format

```
BE int      version, should always be 24

BE uint     seed
BE int      frames played
BE float    seconds played
BE ulong    unknown counter

Background[] backgrounds

BE int      chunk count

string      schema hash, used for entity files I guess

BE int      game mode index
BE int      game mode name
BE long     game mode steam id

bool        non-Nolla mod used

save and quit date time, in local time (from my testing):
{
    BE short year
    BE short month
    BE short day
    BE short hour
    BE short minute
    BE short second
}

these are likely camera related:
BE int      unknown 1
BE int      unknown 2
BE int      unknown 3
BE int      unknown 4

ChunkInfo[chunk count] chunks, not length encoded


struct ChunkInfo
{
    BE int  x
    BE int  y
    bool    loaded, maybe generated?
}

struct Background
{
    BE float x
    BE float y
    string filename

    BE float z index

    BE float x offset
    BE float y offset
}
```
