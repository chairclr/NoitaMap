using NoitaMap.Game.Map;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Primitives;
using osu.Framework.Graphics.Rendering;
using osuTK;
using osuTK.Graphics;

namespace NoitaMap.Game.Graphics;

public class ChunkContainerDrawNode : TexturedShaderDrawNode
{
    protected readonly ChunkContainer ChunkContainer;

    public ChunkContainerDrawNode(ChunkContainer chunkContainer)
        : base(chunkContainer)
    {
        ChunkContainer = chunkContainer;
    }

    public override void Draw(IRenderer renderer)
    {
        base.Draw(renderer);

        while (ChunkContainer.FinishedChunks.TryDequeue(out Chunk? chunk))
        {
            ChunkContainer.Chunks.Add(chunk.Position, chunk);
        }

        BindTextureShader(renderer);

        foreach (Chunk chunk in ChunkContainer.Chunks.Values)
        {
            Vector2 position = chunk.Position;

            if (chunk.ReadyForTextureCreation)
            {
                chunk.CreateTexture(renderer);
            }

            if (chunk.InternalTexture?.Available != true)
            {
                continue;
            }

            Quad quad = new Quad(position.X, position.Y, Chunk.ChunkWidth, Chunk.ChunkHeight);

            renderer.DrawQuad(chunk.InternalTexture, quad, Color4.White);
        }

        UnbindTextureShader(renderer);
    }
}
