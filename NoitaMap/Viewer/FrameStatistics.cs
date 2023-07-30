using System.Diagnostics;

namespace NoitaMap.Viewer;

public class FrameStatistics
{
    public static Dictionary<string, double> FrameStatTimers = new Dictionary<string, double>();

    public static void AddStatTime(Stopwatch stopwatch, string name)
    {
        stopwatch.Stop();

        if (!FrameStatTimers.ContainsKey(name))
            FrameStatTimers.Add(name, stopwatch.Elapsed.TotalSeconds);
        else
            FrameStatTimers[name] = stopwatch.Elapsed.TotalSeconds;
    }
}
