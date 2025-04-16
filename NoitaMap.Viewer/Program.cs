using NoitaMap.Viewer;

namespace NoitaMap;

internal class Program
{
    static void Main(string[] args)
    {
        PathService.SetPaths(args);

        using ViewerDisplay viewer = new ViewerDisplay();
    }
}