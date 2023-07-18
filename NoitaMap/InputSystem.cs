using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

namespace NoitaMap;

internal class InputSystem
{
    private static KeyboardState LastKeyboardState;

    private static KeyboardState KeyboardState;

    private static MouseState LastMouseState;

    private static MouseState MouseState;

    public static void Update()
    {
        LastKeyboardState = KeyboardState;
        KeyboardState = Keyboard.GetState();

        LastMouseState = MouseState;
        MouseState = Mouse.GetState();
    }

    public static bool IsKeyDown(Keys key)
    {
        return KeyboardState.IsKeyDown(key);
    }

    public static bool IsKeyUp(Keys key)
    {
        return KeyboardState.IsKeyDown(key);
    }

    public static bool IsKeyPressed(Keys key)
    {
        return KeyboardState.IsKeyDown(key) && !LastKeyboardState.IsKeyDown(key);
    }

    public static bool IsKeyReleased(Keys key)
    {
        return !KeyboardState.IsKeyDown(key) && LastKeyboardState.IsKeyDown(key);
    }

    public static bool LeftMouseDown => MouseState.LeftButton == ButtonState.Pressed;

    public static bool RightMouseDown => MouseState.RightButton == ButtonState.Pressed;

    public static bool MiddleMouseDown => MouseState.MiddleButton == ButtonState.Pressed;

    public static bool LeftMouseUp => MouseState.LeftButton == ButtonState.Released;

    public static bool RightMouseUp => MouseState.RightButton == ButtonState.Released;

    public static bool MiddleMouseUp => MouseState.MiddleButton == ButtonState.Released;

    public static bool LeftMousePressed => MouseState.LeftButton == ButtonState.Pressed && LastMouseState.LeftButton == ButtonState.Released;

    public static bool RightMousePressed => MouseState.RightButton == ButtonState.Pressed && LastMouseState.RightButton == ButtonState.Released;

    public static bool MiddleMousePressed => MouseState.MiddleButton == ButtonState.Pressed && LastMouseState.MiddleButton == ButtonState.Released;

    public static bool LeftMouseReleased => MouseState.LeftButton == ButtonState.Released && LastMouseState.LeftButton == ButtonState.Pressed;

    public static bool RightMouseReleased => MouseState.RightButton == ButtonState.Released && LastMouseState.RightButton == ButtonState.Pressed;

    public static bool MiddleMouseReleased => MouseState.MiddleButton == ButtonState.Released && LastMouseState.MiddleButton == ButtonState.Pressed;

    public static int ScrollDelta => MouseState.ScrollWheelValue - LastMouseState.ScrollWheelValue;

    public static Vector2 MousePosition => MouseState.Position.ToVector2();
}
