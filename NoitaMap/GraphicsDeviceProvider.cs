using System.Diagnostics.CodeAnalysis;
using Microsoft.Xna.Framework.Graphics;

namespace NoitaMap;

internal class GraphicsDeviceProvider
{
    // me lying to the compiler:
    [NotNull]
    public static GraphicsDevice? GraphicsDevice = null;
}
