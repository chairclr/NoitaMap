using System.Runtime.CompilerServices;
using Veldrid;

namespace NoitaMap.Graphics;

public class ConstantBuffer<T> : IDisposable
    where T : unmanaged
{
    private bool Disposed;

    protected readonly GraphicsDevice GraphicsDevice;

    public readonly DeviceBuffer DeviceBuffer;

    public T Data;

    public ConstantBuffer(GraphicsDevice graphicsDevice)
    {
        GraphicsDevice = graphicsDevice;

        DeviceBuffer = graphicsDevice.ResourceFactory.CreateBuffer(new BufferDescription()
        {
            SizeInBytes = (uint)Unsafe.SizeOf<T>(),
            Usage = BufferUsage.UniformBuffer | BufferUsage.Dynamic
        });

        Update();
    }

    public void Update()
    {
        unsafe
        {
            MappedResource mapped = GraphicsDevice.Map(DeviceBuffer, MapMode.Write);

            fixed (void* constantBufferPointer = &Data)
            {
                Unsafe.CopyBlock((void*)mapped.Data, constantBufferPointer, (uint)(Unsafe.SizeOf<T>()));
            }

            GraphicsDevice.Unmap(DeviceBuffer);
        }
    }

    public void Update(CommandList commandList)
    {
        unsafe
        {
            fixed (void* constantBufferPointer = &Data)
            {
                commandList.UpdateBuffer(DeviceBuffer, 0, (nint)constantBufferPointer, (uint)Unsafe.SizeOf<T>());
            }
        }
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!Disposed)
        {
            DeviceBuffer.Dispose();

            Disposed = true;
        }
    }

    public void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }
}
