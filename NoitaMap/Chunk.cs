using System.Numerics;
using Microsoft.Xna.Framework.Graphics;

namespace NoitaMap;

internal class Chunk
{
    public Vector2 Position;

    public Texture2D Texture;

    internal Chunk(int x, int y, Texture2D texture)
    {
        Position = new Vector2(x, y);

        Texture = texture;
    }
}
