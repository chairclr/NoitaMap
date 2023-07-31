using NoitaMap.Viewer;

namespace NoitaMap;

internal class Program
{
    static void Main(string[] args)
    {
        using ViewerDisplay viewer = new ViewerDisplay();
        viewer.Start();
    }
}
