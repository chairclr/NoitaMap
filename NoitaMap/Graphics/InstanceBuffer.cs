using System.Collections;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Veldrid;

namespace NoitaMap.Graphics;

public abstract class InstanceBuffer : IDisposable
{
    protected readonly GraphicsDevice GraphicsDevice;

    protected readonly CommandList CopyCommandList;

    public DeviceBuffer? Buffer { get; protected set; }

    public abstract IList Instances { get; }

    private bool Disposed;

    public InstanceBuffer(GraphicsDevice graphicsDevice)
    {
        GraphicsDevice = graphicsDevice;

        CopyCommandList = GraphicsDevice.ResourceFactory.CreateCommandList();
    }

    public abstract void UpdateInstanceBuffer();

    protected virtual void Dispose(bool disposing)
    {
        if (!Disposed)
        {
            CopyCommandList.Dispose();

            Buffer?.Dispose();

            Disposed = true;
        }
    }

    public void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }
}

public class InstanceBuffer<T> : InstanceBuffer
    where T : unmanaged
{
    private int DeviceBufferCapacity;

    public override List<T> Instances { get; } = new List<T>(1024);

    public InstanceBuffer(GraphicsDevice graphicsDevice)
        : base(graphicsDevice)
    {
        DeviceBufferCapacity = Instances.Capacity;

        Buffer = GraphicsDevice.ResourceFactory.CreateBuffer(new BufferDescription()
        {
            SizeInBytes = (uint)(Unsafe.SizeOf<T>() * DeviceBufferCapacity),
            Usage = BufferUsage.VertexBuffer | BufferUsage.Dynamic
        });
    }

    public void AddInstance(T instanceData)
    {
        lock (Instances)
        {
            Instances.Add(instanceData);

            CheckCapacity();
        }
    }

    public void InsertInstance(int index, T instanceData)
    {
        lock (Instances)
        {
            Instances.Insert(index, instanceData);

            CheckCapacity();
        }
    }

    private void CheckCapacity()
    {
        if (Instances.Capacity > DeviceBufferCapacity)
        {
            DeviceBufferCapacity = Instances.Capacity;

            DeviceBuffer newBuffer = GraphicsDevice.ResourceFactory.CreateBuffer(new BufferDescription()
            {
                SizeInBytes = (uint)(Unsafe.SizeOf<T>() * DeviceBufferCapacity),
                Usage = BufferUsage.VertexBuffer | BufferUsage.Dynamic
            });

            CopyCommandList.Begin();

            // -1 because we just added an element
            CopyCommandList.CopyBuffer(Buffer, 0, newBuffer, 0, Buffer!.SizeInBytes);

            CopyCommandList.End();

            GraphicsDevice.SubmitCommands(CopyCommandList);

            Buffer?.Dispose();

            Buffer = newBuffer;
        }
    }

    public unsafe override void UpdateInstanceBuffer()
    {
        lock (Instances)
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
}
