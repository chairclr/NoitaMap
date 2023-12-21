using Veldrid;

namespace NoitaMap.Graphics;

public interface IRenderable : IDisposable
{
    public void Update();

    public void Render(CommandList commandList);
}