using System.Numerics;
using System.Runtime.CompilerServices;
using Veldrid;

namespace NoitaMap.Graphics;

public abstract class QuadVertexBuffer : IDisposable
{
    protected readonly GraphicsDevice GraphicsDevice;

    public List<InstanceBuffer> InstanceBuffers = new List<InstanceBuffer>();

    private bool Disposed;

    public bool Ready { get; protected set; } = false;

    public QuadVertexBuffer(GraphicsDevice graphicsDevice)
    {
        GraphicsDevice = graphicsDevice;
    }

    public abstract void Draw(CommandList commandList, int length, int offset);

    protected virtual void Dispose(bool disposing)
    {
        if (!Disposed)
        {
            foreach (InstanceBuffer instanceBuffer in InstanceBuffers)
            {
                instanceBuffer.Dispose();
            }

            Disposed = true;
        }
    }

    public void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }
}

public unsafe class QuadVertexBuffer<TVert> : QuadVertexBuffer, IDisposable
    where TVert : unmanaged
{
    private readonly DeviceBuffer VertexBuffer;

    private bool Disposed;

    public QuadVertexBuffer(GraphicsDevice graphicsDevice, Func<Vector2, Vector2, TVert> constructVert, params InstanceBuffer[] instanceBuffers)
        : base(graphicsDevice)
    {
        VertexBuffer = GraphicsDevice.ResourceFactory.CreateBuffer(new BufferDescription()
        {
            SizeInBytes = (uint)(Unsafe.SizeOf<TVert>() * 6),
            Usage = BufferUsage.VertexBuffer
        });

        Span<TVert> verts = stackalloc TVert[6];

        verts[0] = constructVert(new Vector2(0f, 0f), Vector2.Zero);
        verts[2] = constructVert(new Vector2(1f, 0f), Vector2.UnitX);
        verts[1] = constructVert(new Vector2(0f, 1f), Vector2.UnitY);

        verts[3] = constructVert(new Vector2(1f, 0f), Vector2.UnitX);
        verts[5] = constructVert(new Vector2(1f, 1f), Vector2.One);
        verts[4] = constructVert(new Vector2(0f, 1f), Vector2.UnitY);

        using CommandList copyCommandList = GraphicsDevice.ResourceFactory.CreateCommandList();

        copyCommandList.Begin();

        copyCommandList.UpdateBuffer(VertexBuffer, 0, verts);

        copyCommandList.End();

        GraphicsDevice.SubmitCommands(copyCommandList);

        GraphicsDevice.WaitForIdle();

        InstanceBuffers.AddRange(instanceBuffers);
    }

    public override void Draw(CommandList commandList, int length, int offset)
    {
        commandList.SetVertexBuffer(0, VertexBuffer);

        for (int i = 0; i < InstanceBuffers.Count; i++)
        {
            commandList.SetVertexBuffer((uint)(i + 1), InstanceBuffers[i].Buffer!);
        }

        commandList.Draw(6, (uint)length, 0, (uint)offset);
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        if (!Disposed)
        {
            VertexBuffer.Dispose();

            Disposed = true;
        }
    }
}
