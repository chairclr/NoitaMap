using System.Numerics;
using ImGuiNET;
using Silk.NET.Input;
using Veldrid;

namespace NoitaMap.Viewer;

public static class InputSystem
{
    private static IInputContext? InputContext;

    private static IMouse? Mouse;

    private static IKeyboard? Keyboard;

    private static MouseState LastMouseState = new MouseState();

    private static MouseState CurrentMouseState = new MouseState();

    private static float Scroll = 0f;

    private static SystemSnapshot InputSnapshot = new SystemSnapshot();

    public static void SetInputContext(IInputContext inputContext)
    {
        InputContext = inputContext;

        void Setup()
        {
            if (Mouse is not null)
            {
                Mouse.Scroll += (_, x) =>
                {
                    Scroll += x.Y;
                };
            }

            if (Keyboard is not null)
            {
                Keyboard.KeyChar += (_, x) =>
                {
                    InputSnapshot.InternalKeyCharPresses.Add(x);
                };
            }
        }

        Mouse = InputContext.Mice.FirstOrDefault(x => x.IsConnected);

        Keyboard = InputContext.Keyboards.FirstOrDefault(x => x.IsConnected);

        Setup();

        InputContext.ConnectionChanged += (x, y) =>
        {
            Mouse = InputContext.Mice.FirstOrDefault(x => x.IsConnected);

            Keyboard = InputContext.Keyboards.FirstOrDefault(x => x.IsConnected);

            Setup();
        };
    }

    public static void Update()
    {
        InputSnapshot.Update();

        if (Mouse is not null && Mouse.IsConnected)
        {
            LastMouseState = CurrentMouseState;

            CurrentMouseState.Position = Mouse.Position;

            CurrentMouseState.LeftDown = Mouse.IsButtonPressed(Silk.NET.Input.MouseButton.Left);
            CurrentMouseState.RightDown = Mouse.IsButtonPressed(Silk.NET.Input.MouseButton.Right);
            CurrentMouseState.MiddleDown = Mouse.IsButtonPressed(Silk.NET.Input.MouseButton.Middle);

            CurrentMouseState.Scroll = Scroll;
        }
    }

    public static InputSnapshot GetInputSnapshot()
    {
        return InputSnapshot;
    }

    public static bool LeftMouseDown => !ImGui.GetIO().WantCaptureMouse && CurrentMouseState.LeftDown;

    public static bool RightMouseDown => !ImGui.GetIO().WantCaptureMouse && CurrentMouseState.RightDown;

    public static bool MiddleMouseDown => !ImGui.GetIO().WantCaptureMouse && CurrentMouseState.MiddleDown;

    public static bool LeftMousePressed => !ImGui.GetIO().WantCaptureMouse && (CurrentMouseState.LeftDown && !LastMouseState.LeftDown);

    public static bool RightMousePressed => !ImGui.GetIO().WantCaptureMouse && (CurrentMouseState.RightDown && !LastMouseState.RightDown);

    public static bool MiddleMousePressed => !ImGui.GetIO().WantCaptureMouse &&(CurrentMouseState.MiddleDown && !LastMouseState.MiddleDown);

    public static bool LeftMouseReleased => !ImGui.GetIO().WantCaptureMouse && (!CurrentMouseState.LeftDown && LastMouseState.LeftDown);

    public static bool RightMouseReleased => !ImGui.GetIO().WantCaptureMouse &&(!CurrentMouseState.RightDown && LastMouseState.RightDown);

    public static bool MiddleMouseReleased => !ImGui.GetIO().WantCaptureMouse &&(!CurrentMouseState.MiddleDown && LastMouseState.MiddleDown);

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

    private class SystemSnapshot : InputSnapshot
    {
        public readonly List<KeyEvent> InternalKeyEvents = new List<KeyEvent>();

        public readonly List<MouseEvent> InternalMouseEvents = new List<MouseEvent>();

        public readonly List<char> InternalKeyCharPresses = new List<char>();

        public IReadOnlyList<KeyEvent> KeyEvents => InternalKeyEvents;

        public IReadOnlyList<MouseEvent> MouseEvents => InternalMouseEvents;

        public IReadOnlyList<char> KeyCharPresses => InternalKeyCharPresses;

        public Vector2 MousePosition { get; set; }

        public float WheelDelta { get; set; }

        public bool IsMouseDown(Veldrid.MouseButton button)
        {
            return button switch
            {
                Veldrid.MouseButton.Left => InputSystem.CurrentMouseState.LeftDown,
                Veldrid.MouseButton.Right => InputSystem.CurrentMouseState.RightDown,
                Veldrid.MouseButton.Middle => InputSystem.CurrentMouseState.MiddleDown,
                _ => false,
            };
        }

        public SystemSnapshot()
        {

        }

        public void Update()
        {
            MousePosition = InputSystem.MousePosition;

            WheelDelta = InputSystem.ScrollDelta;

            InternalKeyEvents.Clear();

            InternalMouseEvents.Clear();

            InternalKeyCharPresses.Clear();
        }
    }
}
