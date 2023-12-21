using System.Runtime.InteropServices;
using NoitaMap.Logging;
using Silk.NET.Windowing;
using Veldrid;

namespace NoitaMap.Startup;

public class VeldridWindow
{
    public static void CreateGraphicsDevice(IWindow window, GraphicsDeviceOptions deviceOptions, out GraphicsDevice graphicsDevice)
    {
        graphicsDevice = CreateGraphicsDevice(window, deviceOptions, GetPlatformDefaultBackend());
    }

    public static void CreateGraphicsDevice(IWindow window, GraphicsDeviceOptions deviceOptions, GraphicsBackend preferredBackend, out GraphicsDevice graphicsDevice)
    {
        graphicsDevice = CreateGraphicsDevice(window, deviceOptions, preferredBackend);
    }

    private static GraphicsDevice CreateGraphicsDevice(IWindow window, GraphicsDeviceOptions options, GraphicsBackend backend)
    {
        switch (backend)
        {
            case GraphicsBackend.Direct3D11:
                return CreateDefaultD3D11GraphicsDevice(options, window);
            case GraphicsBackend.Vulkan:
                return CreateVulkanGraphicsDevice(options, window);
            case GraphicsBackend.Metal:
                return CreateMetalGraphicsDevice(options, window);
            case GraphicsBackend.OpenGL:
            case GraphicsBackend.OpenGLES:
                break;
        }

        throw new Exception($"Couldn't create graphics device for {backend}");
    }

    private static SwapchainDescription GetSwapchainDesc(GraphicsDeviceOptions options, IWindow window)
    {
        return new SwapchainDescription(
            GetSwapchainSource(window),
            (uint)window.Size.X, (uint)window.Size.Y,
            options.SwapchainDepthFormat,
            options.SyncToVerticalBlank,
            options.SwapchainSrgbFormat);
    }

    public static GraphicsDevice CreateDefaultD3D11GraphicsDevice(GraphicsDeviceOptions options, IWindow window)
    {
        return GraphicsDevice.CreateD3D11(options, GetSwapchainDesc(options, window));
    }

    public static GraphicsDevice CreateVulkanGraphicsDevice(GraphicsDeviceOptions options, IWindow window)
    {
        return GraphicsDevice.CreateVulkan(options, GetSwapchainDesc(options, window));
    }

    private static GraphicsDevice CreateMetalGraphicsDevice(GraphicsDeviceOptions options, IWindow window)
    {
        return GraphicsDevice.CreateMetal(options, GetSwapchainDesc(options, window));
    }

    public static SwapchainSource GetSwapchainSource(IWindow window)
    {
        if (window.Native?.Win32 is not null)
        {
            return SwapchainSource.CreateWin32(window.Native.Win32.Value.Hwnd, window.Native.Win32.Value.HInstance);
        }
        else if (window.Native?.X11 is not null)
        {
            return SwapchainSource.CreateXlib(window.Native.X11.Value.Display, (nint)window.Native.X11.Value.Window);
        }
        else if (window.Native?.Wayland is not null)
        {
            return SwapchainSource.CreateWayland(window.Native.Wayland.Value.Display, window.Native.Wayland.Value.Surface);
        }
        else if (window.Native?.Cocoa is not null)
        {
            return SwapchainSource.CreateNSWindow(window.Native.Cocoa.Value);
        }

        Logger.LogCritical($"Failed to create swapchain for {window.Native}");

        throw new Exception();
    }

    public static GraphicsBackend GetPlatformDefaultBackend()
    {
        if (GraphicsDevice.IsBackendSupported(GraphicsBackend.Vulkan))
        {
            return GraphicsBackend.Vulkan;
        }

        if (OperatingSystem.IsWindows())
        {
            return GraphicsDevice.IsBackendSupported(GraphicsBackend.Direct3D11) ? GraphicsBackend.Direct3D11 : GraphicsBackend.OpenGL;
        }
        else if (OperatingSystem.IsMacOS())
        {
            return GraphicsDevice.IsBackendSupported(GraphicsBackend.Metal) ? GraphicsBackend.Metal : GraphicsBackend.OpenGL;
        }
        else
        {
            return GraphicsDevice.IsBackendSupported(GraphicsBackend.Vulkan) ? GraphicsBackend.Vulkan : GraphicsBackend.OpenGL;
        }
    }
}
