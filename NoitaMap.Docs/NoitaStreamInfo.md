# Noita Stream Info
The .streaminfo file contains more global information about a save, with things like seed, frames played, schema used, game mode, and more


## Noita Stream Info file Format

```
int         version, should always be 24

uint        seed
int         frames played
float       seconds played
ulong       unknown counter

Background[] backgrounds

int         chunk count

string      schema hash, used for entity files I guess

int         game mode index
string      game mode name
long        game mode steam id

bool        non-Nolla mod used

save and quit date time, in local time (from my testing):
{
    short   year
    short   month
    short   day
    short   hour
    short   minute
    short   second
}

these are likely camera related:
int         unknown 1
int         unknown 2
int         unknown 3
int         unknown 4

ChunkInfo[chunk count] chunks, not length encoded


struct ChunkInfo
{
    int     x
    int     y
    bool    loaded, maybe generated?
}

struct Background
{
    float   x
    float   y
    string  filename

    float   z index

    float   x offset
    float   y offset
}
```
