namespace NoitaMap;

internal class Program
{
    static void Main(string[] args)
    {
        Viewer viewer = new Viewer(args);

        viewer.Run();
    }
}
