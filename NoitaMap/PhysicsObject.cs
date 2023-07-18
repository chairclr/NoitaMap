using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace NoitaMap;

internal class PhysicsObject
{
    public Vector2 Position;

    public Vector2 Size;

    public float Rotation;

    public Texture2D Texture;

    public PhysicsObject(Vector2 position, Vector2 size, float rotation, Texture2D image)
    {
        Position = position;

        Size = size;

        Rotation = rotation;

        Texture = image;
    }
}
