using NoitaMap.Viewer;

namespace NoitaMap;

internal class Program
{
    static void Main(string[] args)
    {
        PathService pathService = new PathService(args);

        using ViewerDisplay viewer = new ViewerDisplay(pathService);
        viewer.Start();
    }
}
