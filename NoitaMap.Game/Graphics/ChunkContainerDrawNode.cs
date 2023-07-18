using System;
using System.Runtime.InteropServices;
using NoitaMap.Game.Map;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Primitives;
using osu.Framework.Graphics.Rendering;
using osu.Framework.Graphics.Rendering.Vertices;
using osu.Framework.Graphics.Shaders;
using osu.Framework.Graphics.Shaders.Types;
using osuTK;
using osuTK.Graphics;

namespace NoitaMap.Game.Graphics;

public class ChunkContainerDrawNode : TexturedShaderDrawNode
{
    protected readonly ChunkContainer ChunkContainer;

    protected IUniformBuffer<TransformUniform>? TransformBuffer;

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

        TransformBuffer ??= renderer.CreateUniformBuffer<TransformUniform>();

        TransformBuffer.Data = new TransformUniform()
        {
            ViewMatrix = ChunkContainer.ViewMatrix
        };

        ChunkContainer.TextureShader.BindUniformBlock("g_Transform", TransformBuffer);

        foreach (Chunk chunk in ChunkContainer.Chunks.Values)
        {
            if (chunk.ReadyForTextureCreation)
            {
                chunk.CreateTexture(renderer);
            }

            if (chunk.InternalTexture?.Available != true)
            {
                continue;
            }

            Vector2 position = chunk.Position;

            Quad quad = new Quad(position.X, position.Y, Chunk.ChunkWidth, Chunk.ChunkHeight);

            renderer.DrawQuad(chunk.InternalTexture, quad, Color4.White);
        }

        UnbindTextureShader(renderer);
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    protected struct TransformUniform : IEquatable<TransformUniform>
    {
        public UniformMatrix4 ViewMatrix;

        public bool Equals(TransformUniform other)
        {
            return ViewMatrix == other.ViewMatrix;
        }
    }
}
