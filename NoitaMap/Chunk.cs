using System.Numerics;
using Microsoft.Xna.Framework.Graphics;

namespace NoitaMap;

internal class Chunk
{
    public Vector2 Position;

    public Texture2D Texture;

    public PhysicsObject[] PhysicsObjects;

    internal Chunk(int x, int y, Texture2D texture, PhysicsObject[] physicsObjects)
    {
        Position = new Vector2(x, y);

        Texture = texture;

        PhysicsObjects = physicsObjects;
    }
}
