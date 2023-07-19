using System.Runtime.InteropServices;
using CommunityToolkit.HighPerformance.Helpers;
using NoitaMap.Graphics;
using NoitaMap.Map;
using Silk.NET.GLFW;
using Silk.NET.Maths;
using Silk.NET.Windowing;
using Silk.NET.Windowing.Extensions.Veldrid;
using Veldrid;
using Vulkan;

namespace NoitaMap.Viewer;

public class ViewerDisplay : IDisposable
{
    private readonly IWindow Window;

    private readonly GraphicsDevice GraphicsDevice;

    private readonly CommandList MainCommandList;

    private Framebuffer MainFrameBuffer;

    private readonly StagingResourcePool StagingResourcePool;

    private readonly Texture TestTexture;

    private readonly MaterialProvider MaterialProvider;

    private bool Disposed;

    public ViewerDisplay()
    {
        WindowOptions windowOptions = new WindowOptions()
        {
            API = GraphicsAPI.None,
            Title = "Noita Map Viewer",
            Size = new Vector2D<int>(1280, 720)
        };

        GraphicsDeviceOptions graphicsOptions = new GraphicsDeviceOptions()
        {
#if DEBUG
            Debug = true,
#endif
            SyncToVerticalBlank = true,
            HasMainSwapchain = true
        };

        VeldridWindow.CreateWindowAndGraphicsDevice(windowOptions, graphicsOptions, out Window, out GraphicsDevice);

        MainCommandList = GraphicsDevice.ResourceFactory.CreateCommandList();

        MainFrameBuffer = GraphicsDevice.MainSwapchain.Framebuffer;

        StagingResourcePool = new StagingResourcePool(GraphicsDevice);

        MaterialProvider = new MaterialProvider();

        Material brick = MaterialProvider.GetMaterial("brick");

        TextureDescription desc = new TextureDescription()
        {
            Type = TextureType.Texture2D,
            Format = PixelFormat.R8_G8_B8_A8_UNorm,
            Width = (uint)brick.MaterialTexture.Width,
            Height = (uint)brick.MaterialTexture.Height,
            Usage = TextureUsage.Staging,
            MipLevels = 1,

            // Nececessary
            Depth = 1,
            ArrayLayers = 1,
            SampleCount = TextureSampleCount.Count1,
        };

        TestTexture = GraphicsDevice.ResourceFactory.CreateTexture(desc);

        GraphicsDevice.UpdateTexture(TestTexture, MemoryMarshal.CreateSpan(ref brick.MaterialTexture.Span.DangerousGetReference(), (int)brick.MaterialTexture.Length), 0, 0, 0, (uint)brick.MaterialTexture.Width, (uint)brick.MaterialTexture.Height, 1, 0, 0);

        Window.Center();

        Window.Render += x => Render();

        Window.Resize += HandleResize;
    }

    public void Start()
    {
        Window.Run();
    }

    private void Render()
    {
        MainCommandList.Begin();

        MainCommandList.SetFramebuffer(MainFrameBuffer);

        MainCommandList.ClearColorTarget(0, RgbaFloat.CornflowerBlue);

        MainCommandList.End();

        GraphicsDevice.SubmitCommands(MainCommandList);

        GraphicsDevice.SwapBuffers();
    }

    private void HandleResize(Vector2D<int> size)
    {
        GraphicsDevice.ResizeMainWindow((uint)size.X, (uint)size.Y);

        MainFrameBuffer = GraphicsDevice.MainSwapchain.Framebuffer;

        // We call render to be more responsive when resizing.. or something like that
        Render();
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!Disposed)
        {
            MainCommandList.Dispose();

            GraphicsDevice.Dispose();

            Window.Dispose();

            Disposed = true;
        }
    }

    public void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }
}
