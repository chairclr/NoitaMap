using Veldrid;

namespace NoitaMap.Graphics;

public class StagingResourcePool : IDisposable
{
    private readonly GraphicsDevice GraphicsDevice;

    private readonly List<Texture> Available = new List<Texture>();

    private readonly List<Texture> Used = new List<Texture>();

    private bool Disposed;

    public StagingResourcePool(GraphicsDevice graphicsDevice)
    {
        GraphicsDevice = graphicsDevice;
    }

    public Texture Rent(Texture target)
    {
        // Look for any textures that are available and match the target
        for (int i = 0; i < Available.Count; i++)
        {
            Texture texture = Available[i];

            if (target.Type == texture.Type && target.Format == texture.Format)
            {
                switch (target.Type)
                {
                    case TextureType.Texture1D:
                        if (target.Width == texture.Width)
                        {
                            Available.RemoveAt(i--);
                            Used.Add(texture);

                            return texture;
                        }
                        break;
                    case TextureType.Texture2D:
                        if (target.Width == texture.Width && target.Height == texture.Height)
                        {
                            Available.RemoveAt(i--);
                            Used.Add(texture);

                            return texture;
                        }
                        break;
                    case TextureType.Texture3D:
                        if (target.Width == texture.Width && target.Height == texture.Height && target.Depth == texture.Depth)
                        {
                            Available.RemoveAt(i--);
                            Used.Add(texture);

                            return texture;
                        }
                        break;

                }
            }
        }

        // Create a new texture based on the target
        Texture pooled = GraphicsDevice.ResourceFactory.CreateTexture(new TextureDescription()
        {
            Width = target.Width,
            Height = target.Height,
            Depth = target.Depth,
            Format = target.Format,
            Type = target.Type,
            ArrayLayers = target.ArrayLayers,
            MipLevels = 1,
            Usage = TextureUsage.Staging,
            SampleCount = TextureSampleCount.Count1
        });

        Used.Add(pooled);

        return pooled;
    }

    public Texture Rent(TextureDescription target)
    {
        // Look for any textures that are available and match the target
        for (int i = 0; i < Available.Count; i++)
        {
            Texture texture = Available[i];

            if (target.Type == texture.Type && target.Format == texture.Format)
            {
                switch (target.Type)
                {
                    case TextureType.Texture1D:
                        if (target.Width == texture.Width)
                        {
                            Available.RemoveAt(i--);
                            Used.Add(texture);

                            return texture;
                        }
                        break;
                    case TextureType.Texture2D:
                        if (target.Width == texture.Width && target.Height == texture.Height)
                        {
                            Available.RemoveAt(i--);
                            Used.Add(texture);

                            return texture;
                        }
                        break;
                    case TextureType.Texture3D:
                        if (target.Width == texture.Width && target.Height == texture.Height && target.Depth == texture.Depth)
                        {
                            Available.RemoveAt(i--);
                            Used.Add(texture);

                            return texture;
                        }
                        break;

                }
            }
        }

        // Create a new texture based on the target
        Texture pooled = GraphicsDevice.ResourceFactory.CreateTexture(new TextureDescription()
        {
            Width = target.Width,
            Height = target.Height,
            Depth = target.Depth,
            Format = target.Format,
            Type = target.Type,
            ArrayLayers = target.ArrayLayers,
            MipLevels = 1,
            Usage = TextureUsage.Staging,
            SampleCount = TextureSampleCount.Count1
        });

        Used.Add(pooled);

        return pooled;
    }

    public void Return(Texture resource)
    {
        if (!Used.Contains(resource))
        {
            throw new ArgumentException("Pool does not own that resource", nameof(resource));
        }

        Used.Remove(resource);
        Available.Add(resource);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!Disposed)
        {
            foreach (Texture texture in Available)
            {
                texture.Dispose();
            }

            Available.Clear();

            foreach (Texture texture in Used)
            {
                texture.Dispose();
            }

            Used.Clear();

            Disposed = true;
        }
    }

    public void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }
}
