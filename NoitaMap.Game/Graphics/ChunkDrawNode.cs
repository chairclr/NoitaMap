using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NoitaMap.Game.Map;
using osu.Framework.Graphics.Rendering;
using osu.Framework.Graphics.Sprites;

namespace NoitaMap.Game.Graphics;

public class ChunkDrawNode : SpriteDrawNode
{
    private Chunk Chunk;

    public ChunkDrawNode(Chunk source)
        : base(source)
    {
        Chunk = source;
    }

    public override void Draw(IRenderer renderer)
    {
        if (Chunk.ReadyForTextureCreation)
        {
            Chunk.CreateTexture(renderer);
        }

        base.Draw(renderer);
    }
}
