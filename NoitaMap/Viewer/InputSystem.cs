using System.Numerics;
using Silk.NET.Input;

namespace NoitaMap.Viewer;

public static class InputSystem
{
    private static IInputContext? InputContext;

    private static IMouse? Mouse;

    private static IKeyboard? Keyboard;

    private static MouseState LastMouseState = new MouseState();

    private static MouseState CurrentMouseState = new MouseState();

    private static float Scroll = 0f;

    public static void SetInputContext(IInputContext inputContext)
    {
        InputContext = inputContext;

        Mouse = InputContext.Mice.FirstOrDefault(x => x.IsConnected);

        if (Mouse is not null)
        {
            Mouse.Scroll += (_, x) =>
            {
                Scroll += x.Y;
            };
        }

        Keyboard = InputContext.Keyboards.FirstOrDefault(x => x.IsConnected);

        InputContext.ConnectionChanged += (x, y) =>
        {
            Mouse = InputContext.Mice.FirstOrDefault(x => x.IsConnected);

            Keyboard = InputContext.Keyboards.FirstOrDefault(x => x.IsConnected);
        };
    }

    public static void Update()
    {
        if (Mouse is not null && Mouse.IsConnected)
        {
            LastMouseState = CurrentMouseState;

            CurrentMouseState.Position = Mouse.Position;

            CurrentMouseState.LeftDown = Mouse.IsButtonPressed(MouseButton.Left);
            CurrentMouseState.RightDown = Mouse.IsButtonPressed(MouseButton.Right);
            CurrentMouseState.MiddleDown = Mouse.IsButtonPressed(MouseButton.Middle);

            CurrentMouseState.Scroll = Scroll;
        }
    }

    public static bool LeftMouseDown => CurrentMouseState.LeftDown;

    public static bool RightMouseDown => CurrentMouseState.RightDown;

    public static bool MiddleMouseDown => CurrentMouseState.MiddleDown;

    public static bool LeftMousePressed => CurrentMouseState.LeftDown && !LastMouseState.LeftDown;

    public static bool RightMousePressed => CurrentMouseState.RightDown && !LastMouseState.RightDown;

    public static bool MiddleMousePressed => CurrentMouseState.MiddleDown && !LastMouseState.MiddleDown;

    public static bool LeftMouseReleased => !CurrentMouseState.LeftDown && LastMouseState.LeftDown;

    public static bool RightMouseReleased => !CurrentMouseState.RightDown && LastMouseState.RightDown;

    public static bool MiddleMouseReleased => !CurrentMouseState.MiddleDown && LastMouseState.MiddleDown;

    public static Vector2 MousePosition => CurrentMouseState.Position;

    public static float ScrollDelta => CurrentMouseState.Scroll - LastMouseState.Scroll;

    private struct MouseState
    {
        public Vector2 Position;

        public bool LeftDown;

        public bool RightDown;

        public bool MiddleDown;

        public float Scroll;
    }
}
