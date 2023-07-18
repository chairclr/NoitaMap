using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace NoitaMap;

internal class PhysicsObject
{
    public Vector2 Position;

    public Vector2 Size;

    public float Rotation;

    public Texture2D Texture;

    public ulong A;

    public uint B;

    public double C;

    public double D;

    public double E;

    public double F;

    public double G;

    public bool H;

    public bool I;

    public bool J;

    public bool K;

    public bool L;

    public float M;

    public PhysicsObject(Vector2 position, Vector2 size, float rotation, Texture2D image)
    {
        Position = position;

        Size = size;

        Rotation = rotation;

        Texture = image;
    }
}
