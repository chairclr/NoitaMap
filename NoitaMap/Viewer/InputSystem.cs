using System.Numerics;
using ImGuiNET;
using Veldrid;
using Veldrid.Sdl2;

namespace NoitaMap.Viewer;

public unsafe static class InputSystem
{
    private static MouseState LastMouseState = new MouseState();

    private static MouseState CurrentMouseState = new MouseState();

    private delegate uint GetMouseStateDelegate(out int x, out int y);

    private static GetMouseStateDelegate? GetMouseState;

    public static void Update(InputSnapshot inputSnapshot)
    {
        GetMouseState ??= Sdl2Native.LoadFunction<GetMouseStateDelegate>("SDL_GetMouseState");

        LastMouseState = CurrentMouseState;

        // We use GetMouseState to be more responsive
        GetMouseState(out int x, out int y);
        CurrentMouseState.Position = new Vector2(x, y);

        CurrentMouseState.LeftDown = inputSnapshot.IsMouseDown(MouseButton.Left);
        CurrentMouseState.RightDown = inputSnapshot.IsMouseDown(MouseButton.Right);
        CurrentMouseState.MiddleDown = inputSnapshot.IsMouseDown(MouseButton.Middle);

        CurrentMouseState.Scroll += inputSnapshot.WheelDelta;
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

    public static float ScrollDelta => ImGui.GetIO().WantCaptureMouse ? 0f : (CurrentMouseState.Scroll - LastMouseState.Scroll);

    private struct MouseState
    {
        public Vector2 Position;

        public bool LeftDown;

        public bool RightDown;

        public bool MiddleDown;

        public float Scroll;
    }

}
