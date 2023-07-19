using NoitaMap.Viewer;
using Silk.NET.Windowing;
using Silk.NET.Windowing.Extensions.Veldrid;

namespace NoitaMap;

internal class Program
{
    static void Main(string[] args)
    {
        using ViewerDisplay viewer = new ViewerDisplay();
        viewer.Start();
    }
}
