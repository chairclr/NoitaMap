using NoitaMap.Map;
using System.IO;
using NoitaMap.Viewer;
using System.Runtime.CompilerServices;
using System.Buffers.Binary;
using NoitaMap.Compression;

namespace NoitaMap;

internal class Program
{
    static void Main(string[] args)
    {
        using ViewerDisplay viewer = new ViewerDisplay();
        viewer.Start();
    }
}
