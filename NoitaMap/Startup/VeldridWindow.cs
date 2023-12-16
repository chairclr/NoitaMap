using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Veldrid;
using Veldrid.Sdl2;

namespace NoitaMap.Startup;

public class VeldridWindow
{
    public static void CreateWindowAndGraphicsDevice(WindowOptions windowOptions, GraphicsDeviceOptions deviceOptions, out Sdl2Window window, out GraphicsDevice graphicsDevice)
    {
        Sdl2Native.SDL_Init(SDLInitFlags.Video);

        window = CreateWindow(windowOptions);
        graphicsDevice = CreateGraphicsDevice(window, deviceOptions, GetPlatformDefaultBackend()); 
    }

    public static void CreateWindowAndGraphicsDevice(WindowOptions windowOptions, GraphicsDeviceOptions deviceOptions, GraphicsBackend preferredBackend, out Sdl2Window window, out GraphicsDevice graphicsDevice)
    {
        Sdl2Native.SDL_Init(SDLInitFlags.Video);

        window = CreateWindow(windowOptions);
        graphicsDevice = CreateGraphicsDevice(window, deviceOptions, preferredBackend);
    }

    private static Sdl2Window CreateWindow(in WindowOptions windowOptions)
    {
        SDL_WindowFlags flags = SDL_WindowFlags.OpenGL | SDL_WindowFlags.Resizable;

        if (windowOptions.Hide)
        {
            flags |= SDL_WindowFlags.Hidden;
        }
        else
        {
            flags |= SDL_WindowFlags.Shown;
        }

        return new Sdl2Window(windowOptions.Title, windowOptions.X, windowOptions.Y, windowOptions.Width, windowOptions.Height, flags, false);
    }

    private static GraphicsDevice CreateGraphicsDevice(Sdl2Window window, GraphicsDeviceOptions options, GraphicsBackend backend)
    {
        switch (backend)
        {
            case GraphicsBackend.Direct3D11:
                return CreateDefaultD3D11GraphicsDevice(options, window);
            case GraphicsBackend.Vulkan:
                return CreateVulkanGraphicsDevice(options, window);
            case GraphicsBackend.OpenGL:
                break;
            case GraphicsBackend.Metal:
                return CreateMetalGraphicsDevice(options, window);
            case GraphicsBackend.OpenGLES:
                break;
        }

        throw new Exception($"Couldn't create graphics device for {backend}");
    }

    private static SwapchainDescription GetSwapchainDesc(GraphicsDeviceOptions options, Sdl2Window window)
    {
        return new SwapchainDescription(
            GetSwapchainSource(window),
            (uint)window.Width, (uint)window.Height,
            options.SwapchainDepthFormat,
            options.SyncToVerticalBlank,
            options.SwapchainSrgbFormat);
    }

    public static GraphicsDevice CreateDefaultD3D11GraphicsDevice(GraphicsDeviceOptions options, Sdl2Window window)
    {
        return GraphicsDevice.CreateD3D11(options, GetSwapchainDesc(options, window));
    }

    public static GraphicsDevice CreateVulkanGraphicsDevice(GraphicsDeviceOptions options, Sdl2Window window)
    {
        return GraphicsDevice.CreateVulkan(options, GetSwapchainDesc(options, window));
    }

    private static GraphicsDevice CreateMetalGraphicsDevice(GraphicsDeviceOptions options, Sdl2Window window)
    {
        return GraphicsDevice.CreateMetal(options, GetSwapchainDesc(options, window));
    }

    public static unsafe SwapchainSource GetSwapchainSource(Sdl2Window window)
    {
        nint sdlHandle = window.SdlWindowHandle;

        SDL_SysWMinfo sysWmInfo;
        Sdl2Native.SDL_GetVersion(&sysWmInfo.version);
        Sdl2Native.SDL_GetWMWindowInfo(sdlHandle, &sysWmInfo);

        switch (sysWmInfo.subsystem)
        {
            case SysWMType.Windows:
                {
                    Win32WindowInfo w32Info = Unsafe.Read<Win32WindowInfo>(&sysWmInfo.info);
                    return SwapchainSource.CreateWin32(w32Info.Sdl2Window, w32Info.hinstance);
                }
            case SysWMType.X11:
                {
                    X11WindowInfo x11Info = Unsafe.Read<X11WindowInfo>(&sysWmInfo.info);
                    return SwapchainSource.CreateXlib(
                    x11Info.display,
                    x11Info.Sdl2Window);
                }
            case SysWMType.Wayland:
                {
                    WaylandWindowInfo wlInfo = Unsafe.Read<WaylandWindowInfo>(&sysWmInfo.info);
                    return SwapchainSource.CreateWayland(wlInfo.display, wlInfo.surface);
                }
            case SysWMType.Cocoa:
                {
                    CocoaWindowInfo cocoaInfo = Unsafe.Read<CocoaWindowInfo>(&sysWmInfo.info);
                    nint nsWindow = cocoaInfo.Window;
                    return SwapchainSource.CreateNSWindow(nsWindow);
                }
            default:
                throw new PlatformNotSupportedException("Cannot create a SwapchainSource for " + sysWmInfo.subsystem + ".");
        }
    }

    public static GraphicsBackend GetPlatformDefaultBackend()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            return GraphicsDevice.IsBackendSupported(GraphicsBackend.Direct3D11) ? GraphicsBackend.Direct3D11 : GraphicsBackend.OpenGL;
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            return GraphicsDevice.IsBackendSupported(GraphicsBackend.Metal) ? GraphicsBackend.Metal : GraphicsBackend.OpenGL;
        }
        else
        {
            return GraphicsDevice.IsBackendSupported(GraphicsBackend.Vulkan) ? GraphicsBackend.Vulkan : GraphicsBackend.OpenGL;
        }
    }
}

public record struct WindowOptions(string Title, int X, int Y, int Width, int Height, bool Hide);
