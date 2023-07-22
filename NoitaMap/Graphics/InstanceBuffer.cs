using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

using Veldrid;

namespace NoitaMap.Graphics;

public abstract class InstanceBuffer
{
    protected readonly GraphicsDevice GraphicsDevice;

    protected readonly CommandList CopyCommandList;

    public DeviceBuffer? Buffer { get; protected set; }

    public abstract void UpdateInstanceBuffer();

    public InstanceBuffer(GraphicsDevice graphicsDevice)
    {
        GraphicsDevice = graphicsDevice;

        CopyCommandList = GraphicsDevice.ResourceFactory.CreateCommandList();
    }
}

public class InstanceBuffer<T> : InstanceBuffer
    where T : unmanaged
{
    private int Capacity = 1024;

    public readonly List<T> Instances = new List<T>(1024);

    public InstanceBuffer(GraphicsDevice graphicsDevice)
        : base(graphicsDevice)
    {
        Buffer = GraphicsDevice.ResourceFactory.CreateBuffer(new BufferDescription()
        {
            SizeInBytes = (uint)(Unsafe.SizeOf<T>() * Capacity),
            Usage = BufferUsage.VertexBuffer | BufferUsage.Dynamic
        });
    }

    public void AddInstance(T instanceData)
    {
        Instances.Add(instanceData);

        if (Instances.Count >= Capacity)
        {
            Capacity *= 2;

            DeviceBuffer newBuffer = GraphicsDevice.ResourceFactory.CreateBuffer(new BufferDescription()
            {
                SizeInBytes = (uint)(Unsafe.SizeOf<T>() * Capacity),
                Usage = BufferUsage.VertexBuffer | BufferUsage.Dynamic
            });

            CopyCommandList.Begin();

            // -1 because we just added an element
            CopyCommandList.CopyBuffer(Buffer, 0, newBuffer, 0, (uint)((Instances.Count - 1) * Unsafe.SizeOf<T>()));

            CopyCommandList.End();

            GraphicsDevice.SubmitCommands(CopyCommandList);

            Buffer?.Dispose();

            Buffer = newBuffer;
        }
    }

    public unsafe override void UpdateInstanceBuffer()
    {
        MappedResource resource = GraphicsDevice.Map(Buffer, MapMode.Write);

        Span<T> span = CollectionsMarshal.AsSpan(Instances);

        fixed (void* instancePointer = &span[0])
        {
            Unsafe.CopyBlock((void*)resource.Data, instancePointer, (uint)(Instances.Count * Unsafe.SizeOf<T>()));
        }

        GraphicsDevice.Unmap(Buffer);
    }
}
