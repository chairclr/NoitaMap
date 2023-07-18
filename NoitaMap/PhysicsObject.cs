using System.Buffers;
using System.Reflection.PortableExecutable;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace NoitaMap;

internal class PhysicsObject
{
    public Vector2 Position;

    public float Rotation;

    public int Width;

    public int Height;

    public Color[]? Colors;

    public Texture2D? Texture;

    public PhysicsObject(BinaryReader reader)
    {
        reader.ReadUInt64();
        reader.ReadUInt32();
        Position.X = reader.BEReadSingle();
        Position.Y = reader.BEReadSingle();
        Rotation = reader.BEReadSingle();
        reader.BEReadInt64();
        reader.BEReadInt64();
        reader.BEReadInt64();
        reader.BEReadInt64();
        reader.BEReadInt64();
        reader.ReadBoolean();
        reader.ReadBoolean();
        reader.ReadBoolean();
        reader.ReadBoolean();
        reader.ReadBoolean();
        reader.BEReadSingle();
        Width = reader.BEReadInt32();
        Height = reader.BEReadInt32();

        Colors = new Color[Width * Height];

        for (int j = 0; j < Width * Height; j++)
        {
            Colors[j].PackedValue = reader.BEReadUInt32();
        }

        //Texture = new Texture2D(GraphicsDeviceProvider.GraphicsDevice, textureWidth, textureHeight);

        //Color[] colors = ArrayPool<Color>.Shared.Rent(textureWidth * textureHeight);

        //for (int j = 0; j < textureWidth * textureHeight; j++)
        //{
        //    colors[j].PackedValue = reader.BEReadUInt32();
        //}

        //Texture.SetData(colors, 0, textureWidth * textureHeight);

        //ArrayPool<Color>.Shared.Return(colors);
    }
}
