using System.Collections.Concurrent;
using System.Diagnostics;

namespace NoitaMap.Viewer;

public class TimeStatistics
{
    public static readonly Dictionary<string, TimeSpan> OncePerFrameStats = new Dictionary<string, TimeSpan>();

    public static readonly Dictionary<string, TimeSpan> SummedStats = new Dictionary<string, TimeSpan>();

    public static readonly Dictionary<string, TimeSpan> SingleStats = new Dictionary<string, TimeSpan>();
}

public class StatisticTimer
{
#if TIME_STATS
    private string Name;

    private ThreadLocal<Stopwatch> Stopwatch = new ThreadLocal<Stopwatch>(() => new Stopwatch());
#endif

    public StatisticTimer(string name)
    {
#if TIME_STATS
        Name = name;
#endif
    }

    public StatisticTimer Begin()
    {
#if TIME_STATS
        Stopwatch.Value!.Restart();
#endif

        return this;
    }

    public void End(StatisticMode mode)
    {
#if TIME_STATS
        Stopwatch sw = Stopwatch.Value!;

        sw.Stop();

        switch (mode)
        {
            case StatisticMode.OncePerFrame:
                {
                    lock (TimeStatistics.OncePerFrameStats)
                    {
                        if (!TimeStatistics.OncePerFrameStats.TryAdd(Name, sw.Elapsed))
                        {
                            TimeStatistics.OncePerFrameStats[Name] = sw.Elapsed;
                        }
                    }
                }
                break;
            case StatisticMode.Sum:
                {
                    lock (TimeStatistics.SummedStats)
                    {
                        if (!TimeStatistics.SummedStats.TryAdd(Name, sw.Elapsed))
                        {
                            TimeStatistics.SummedStats[Name] += sw.Elapsed;
                        }
                    }
                }
                break;
            case StatisticMode.Single:
                {
                    TimeStatistics.SingleStats.Add(Name, sw.Elapsed);
                }
                break;
        }
#endif
    }
}

public enum StatisticMode
{
    OncePerFrame,
    Sum,
    Single,
}