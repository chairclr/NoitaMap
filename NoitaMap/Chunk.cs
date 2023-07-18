using Microsoft.Xna.Framework.Graphics;

namespace NoitaMap;

internal class Chunk
{
    public int X;

    public int Y;

    public Texture2D Texture;

    internal Chunk(int x, int y, Texture2D texture)
    {
        X = x;
        Y = y;
        Texture = texture;
    }
}
