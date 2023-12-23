using System.Collections.Concurrent;
using System.Diagnostics;

namespace NoitaMap.Viewer;

public class Statistics
{
    public static readonly ConcurrentDictionary<string, TimeSpan> OncePerFrameTimeStats = new ConcurrentDictionary<string, TimeSpan>();

    public static readonly ConcurrentDictionary<string, TimeSpan> SummedTimeStats = new ConcurrentDictionary<string, TimeSpan>();

    public static readonly Dictionary<string, TimeSpan> SingleTimeStats = new Dictionary<string, TimeSpan>();

    public static readonly Dictionary<string, Func<string>> Metrics = new Dictionary<string, Func<string>>();
}

public class StatisticTimer
{
#if TIME_STATS
    private readonly string Name;

    private readonly ThreadLocal<Stopwatch> Stopwatch = new ThreadLocal<Stopwatch>(() => new Stopwatch());
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
                    if (!Statistics.OncePerFrameTimeStats.TryAdd(Name, sw.Elapsed))
                    {
                        Statistics.OncePerFrameTimeStats[Name] = sw.Elapsed;
                    }
                }
                break;
            case StatisticMode.Sum:
                {
                    if (!Statistics.SummedTimeStats.TryAdd(Name, sw.Elapsed))
                    {
                        lock (Statistics.SummedTimeStats)
                        {
                            Statistics.SummedTimeStats[Name] += sw.Elapsed;
                        }
                    }
                }
                break;
            case StatisticMode.Single:
                {
                    Statistics.SingleTimeStats.TryAdd(Name, sw.Elapsed);
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