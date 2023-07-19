using System;
using System.Runtime.InteropServices;
using NoitaMap.Game.Map;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Rendering;
using osu.Framework.Graphics.Shaders.Types;

namespace NoitaMap.Game.Graphics;

//public class ChunkContainerDrawNode : TexturedShaderDrawNode
//{
//    protected readonly ChunkContainer ChunkContainer;

//    protected IUniformBuffer<TransformUniform>? TransformBuffer;

//    protected IndexedTextureAtlas? PhysicsObjectAtlas;

//    public ChunkContainerDrawNode(ChunkContainer chunkContainer)
//        : base(chunkContainer)
//    {
//        ChunkContainer = chunkContainer;
//    }

//    float i = 0;

//    public override void Draw(IRenderer renderer)
//    {
//        base.Draw(renderer);

//        while (ChunkContainer.FinishedChunks.TryDequeue(out Chunk? chunk))
//        {
//            ChunkContainer.AddChunk(chunk);
//            //ChunkContainer.Chunks.Add(chunk.ChunkPosition, chunk);

//            //if (chunk.PhysicsObjects is not null)
//            //{
//            //    ChunkContainer.PhysicsObjects.AddRange(chunk.PhysicsObjects);
//            //}
//        }

//        BindTextureShader(renderer);

//        //TransformBuffer ??= renderer.CreateUniformBuffer<TransformUniform>();

//        //PhysicsObjectAtlas ??= new IndexedTextureAtlas(renderer, 4096, 4096, filteringMode: TextureFilteringMode.Nearest);

//        //TransformBuffer.Data = new TransformUniform()
//        //{
//        //    ViewMatrix = ChunkContainer.ViewMatrix,
//        //    ModelMatrix = Matrix4.Identity
//        //};

//        //ChunkContainer.TextureShader.BindUniformBlock("g_Transform", TransformBuffer);

//        //ApplyState();

//        //foreach (Chunk chunk in ChunkContainer.Chunks.Values)
//        //{
//        //    if (chunk.ReadyForTextureCreation)
//        //    {
//        //        chunk.CreateTexture(renderer);
//        //    }

//        //    if (chunk.InternalTexture?.Available != true)
//        //    {
//        //        continue;
//        //    }

//        //    Vector2 position = Vector2Extensions.Transform(chunk.Position, Source.DrawInfo.Matrix);
//        //    Vector2 size = new Vector2(Chunk.ChunkWidth, Chunk.ChunkHeight) * Source.DrawInfo.Matrix.ExtractScale().Xy;

//        //    Quad quad = new Quad(position, position + size * Vector2.UnitX, position + size * Vector2.UnitY, position + size);

//        //    renderer.DrawQuad(chunk.InternalTexture, quad, Color4.White);
//        //}

//        //float i = 0;
//        //foreach (PhysicsObject physicsObject in ChunkContainer.PhysicsObjects)
//        //{
//        //    if (physicsObject.ReadyToBeAddedToAtlas)
//        //    {
//        //        if (PhysicsObjectAtlas.TryFind(physicsObject.TextureHash, out Texture? texture))
//        //        {
//        //            physicsObject.InternalTexture = texture;

//        //            physicsObject.ReadyToBeAddedToAtlas = false;
//        //        }
//        //        else
//        //        {
//        //            Texture? textureRegion = PhysicsObjectAtlas.Add(physicsObject.TextureHash, physicsObject.Width, physicsObject.Height);

//        //            if (textureRegion is not null)
//        //            {
//        //                physicsObject.SetTexture(textureRegion);
//        //            }
//        //        }
//        //    }

//        //    osuTK.Input.MouseState state = Mouse.GetCursorState();

//        //    TransformBuffer.Data = new TransformUniform()
//        //    {
//        //        ViewMatrix = ChunkContainer.ViewMatrix,
//        //        ModelMatrix = Matrix4.CreateRotationZ(physicsObject.Rotation),
//        //        CoolPosition = physicsObject.Position
//        //    };

//        //    Quad quad = new Quad(physicsObject.Position.X, physicsObject.Position.Y, physicsObject.Width, physicsObject.Height);
//        //    renderer.DrawQuad(physicsObject.InternalTexture, quad, Color4.White);
//        //    //TransformBuffer.Data = new TransformUniform()
//        //    //{
//        //    //    ViewMatrix = ChunkContainer.ViewMatrix,
//        //    //    ModelMatrix = Matrix4.Identity
//        //    //};

//        //    //Quad quad2 = new Quad(physicsObject.Position.X, physicsObject.Position.Y - 10f, physicsObject.Width, physicsObject.Height);
//        //    //renderer.DrawQuad(physicsObject.InternalTexture, quad, Color4.Red);
//        //}

//        UnbindTextureShader(renderer);
//    }

//    [StructLayout(LayoutKind.Sequential, Pack = 1)]
//    protected struct TransformUniform : IEquatable<TransformUniform>
//    {
//        public UniformMatrix4 ViewMatrix;
//        public UniformMatrix4 ModelMatrix;
//        public UniformVector2 CoolPosition;
//        private UniformPadding8 __padding1;

//        public readonly bool Equals(TransformUniform other)
//        {
//            return ViewMatrix == other.ViewMatrix && ModelMatrix == other.ModelMatrix;
//        }
//    }
//}
