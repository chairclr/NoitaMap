using System.Numerics;
using ImGuiNET;
using Silk.NET.Input;
using Silk.NET.Windowing;

namespace NoitaMap.Viewer;

public unsafe static class InputSystem
{
    private static MouseState LastMouseState = new MouseState();

    private static MouseState CurrentMouseState = new MouseState();

    private static IInputContext? InputContext;

    private static IMouse? Mouse;

    private static IKeyboard? Keyboard;

    public static void Update(IWindow window)
    {
        InputContext ??= window.CreateInput();
        Mouse ??= InputContext.Mice[0];
        Keyboard ??= InputContext.Keyboards[0];

        LastMouseState = CurrentMouseState;

        // We use GetMouseState to be more responsive
        CurrentMouseState.Position = Mouse.Position;

        CurrentMouseState.LeftDown = Mouse.IsButtonPressed(MouseButton.Left); 
        CurrentMouseState.RightDown = Mouse.IsButtonPressed(MouseButton.Right);
        CurrentMouseState.MiddleDown = Mouse.IsButtonPressed(MouseButton.Middle);

        CurrentMouseState.Scroll += Mouse.ScrollWheels[0].X;
    }

    public static bool LeftMouseDown => !ImGui.GetIO().WantCaptureMouse && CurrentMouseState.LeftDown;

    public static bool RightMouseDown => !ImGui.GetIO().WantCaptureMouse && CurrentMouseState.RightDown;

    public static bool MiddleMouseDown => !ImGui.GetIO().WantCaptureMouse && CurrentMouseState.MiddleDown;

    public static bool LeftMousePressed => !ImGui.GetIO().WantCaptureMouse && (CurrentMouseState.LeftDown && !LastMouseState.LeftDown);

    public static bool RightMousePressed => !ImGui.GetIO().WantCaptureMouse && (CurrentMouseState.RightDown && !LastMouseState.RightDown);

    public static bool MiddleMousePressed => !ImGui.GetIO().WantCaptureMouse && (CurrentMouseState.MiddleDown && !LastMouseState.MiddleDown);

    public static bool LeftMouseReleased => !ImGui.GetIO().WantCaptureMouse && (!CurrentMouseState.LeftDown && LastMouseState.LeftDown);

    public static bool RightMouseReleased => !ImGui.GetIO().WantCaptureMouse && (!CurrentMouseState.RightDown && LastMouseState.RightDown);

    public static bool MiddleMouseReleased => !ImGui.GetIO().WantCaptureMouse && (!CurrentMouseState.MiddleDown && LastMouseState.MiddleDown);

    public static Vector2 MousePosition => CurrentMouseState.Position;

    public static float ScrollDelta => ImGui.GetIO().WantCaptureMouse ? 0f : CurrentMouseState.Scroll;

    private struct MouseState
    {
        public Vector2 Position;

        public bool LeftDown;

        public bool RightDown;

        public bool MiddleDown;

        public float Scroll;
    }

}
