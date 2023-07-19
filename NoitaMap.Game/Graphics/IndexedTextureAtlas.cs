using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Markdig.Extensions.Tables;
using osu.Framework.Graphics.Rendering;
using osu.Framework.Graphics.Textures;

namespace NoitaMap.Game.Graphics;

public class IndexedTextureAtlas
{
    private TextureAtlas TextureAtlas;

    private Dictionary<int, Texture> Indexed = new Dictionary<int, Texture>();

    public IndexedTextureAtlas(IRenderer renderer, int width, int height, bool manualMipmaps = false, TextureFilteringMode filteringMode = TextureFilteringMode.Linear)
    {
        TextureAtlas = new TextureAtlas(renderer, width, height, manualMipmaps, filteringMode);
    }

    public bool TryFind(int hash, [NotNullWhen(true)] out Texture? texture)
    {
        return Indexed.TryGetValue(hash, out texture);
    }

    public Texture? Add(int hash, int width, int height)
    {
        if (Indexed.ContainsKey(hash))
        {
            throw new Exception("Already in index");
        }

        Texture? texture = TextureAtlas.Add(width,  height);

        if (texture is not null)
        {
            Indexed.Add(hash, texture);
        }

        return texture;    
    }
}
