using Silk.NET.Maths;
using Veldrid;

namespace NoitaMap.Graphics;

public interface IRenderable : IDisposable
{
    public void Update();

    public void Render(CommandList commandList);

    public virtual void HandleResize(Vector2D<int> newSize) { }
}