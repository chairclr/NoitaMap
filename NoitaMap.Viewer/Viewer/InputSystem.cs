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

        bool setMouse = Mouse is null;
        Mouse ??= InputContext.Mice[0];

        bool setKeys = Keyboard is null;
        Keyboard ??= InputContext.Keyboards[0];

        if (Mouse is not null && setMouse)
        {
            ImGuiIOPtr io = ImGui.GetIO();

            Mouse.MouseDown += (_, button) =>
            {
                io.AddMouseButtonEvent(((int)button), true);
            };

            Mouse.MouseUp += (_, button) =>
            {
                io.AddMouseButtonEvent(((int)button), false);
            };

            Mouse.Scroll += (_, wheel) =>
            {
                io.AddMouseWheelEvent(wheel.X, wheel.Y);
            };
        }

        if (Keyboard is not null && setKeys)
        {
            ImGuiIOPtr io = ImGui.GetIO();

            Keyboard.KeyChar += (_, character) =>
            {
                io.AddInputCharacter(character);
            };

            Keyboard.KeyDown += (_, key, y) =>
            {
                io.AddKeyEvent(KeyTranslator.GetKey(key), true);
            };

            Keyboard.KeyUp += (_, key, y) =>
            {
                io.AddKeyEvent(KeyTranslator.GetKey(key), false);
            };
        }

        LastMouseState = CurrentMouseState;

        if (Mouse is not null)
        {
            CurrentMouseState.Position = Mouse.Position;

            CurrentMouseState.LeftDown = Mouse.IsButtonPressed(MouseButton.Left);
            CurrentMouseState.RightDown = Mouse.IsButtonPressed(MouseButton.Right);
            CurrentMouseState.MiddleDown = Mouse.IsButtonPressed(MouseButton.Middle);

            CurrentMouseState.Scroll += Mouse.ScrollWheels[0].Y;
        }
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

    public static float ScrollDelta => ImGui.GetIO().WantCaptureMouse ? 0f : CurrentMouseState.Scroll - LastMouseState.Scroll;

    private struct MouseState
    {
        public Vector2 Position;

        public bool LeftDown;

        public bool RightDown;

        public bool MiddleDown;

        public float Scroll;
    }
}
