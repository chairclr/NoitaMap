using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;
using NoitaMap.Viewer;
using Veldrid;

namespace NoitaMap.Graphics;

public unsafe class QuadVertexBuffer<TVert>
    where TVert : unmanaged
{
    private readonly GraphicsDevice GraphicsDevice;

    private readonly DeviceBuffer VertexBuffer;

    public List<InstanceBuffer> InstanceBuffers = new List<InstanceBuffer>();

    public bool Ready { get; private set; } = false;

    public QuadVertexBuffer(GraphicsDevice graphicsDevice, Func<Vector2, Vector2, TVert> constructVert, params InstanceBuffer[] instanceBuffers)
    {
        GraphicsDevice = graphicsDevice;

        VertexBuffer = GraphicsDevice.ResourceFactory.CreateBuffer(new BufferDescription()
        {
            SizeInBytes = (uint)(Unsafe.SizeOf<TVert>() * 6),
            Usage = BufferUsage.VertexBuffer
        });

        Span<TVert> verts = stackalloc TVert[6];

        verts[0] = constructVert(new Vector2(0f, 0f), Vector2.Zero);
        verts[1] = constructVert(new Vector2(1f, 0f), Vector2.UnitX);
        verts[2] = constructVert(new Vector2(0f, 1f), Vector2.UnitY);

        verts[3] = constructVert(new Vector2(1f, 0f), Vector2.UnitX);
        verts[4] = constructVert(new Vector2(1f, 1f), Vector2.One);
        verts[5] = constructVert(new Vector2(0f, 1f), Vector2.UnitY);

        GraphicsDevice.UpdateBuffer(VertexBuffer, 0, verts);

        InstanceBuffers.AddRange(instanceBuffers);
    }

    public void Draw(CommandList commandList, int length, int offset)
    {
        commandList.SetVertexBuffer(0, VertexBuffer);

        for (int i = 0; i < InstanceBuffers.Count; i++)
        {
            commandList.SetVertexBuffer((uint)(i + 1), InstanceBuffers[i].Buffer!);
        }

        commandList.Draw(6, (uint)length, 0, (uint)offset);
    }
}
