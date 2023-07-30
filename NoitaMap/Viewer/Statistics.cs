using System.Diagnostics;

namespace NoitaMap.Viewer;

public class Statistics
{
    public static readonly Dictionary<string, TimeSpan> OncePerFrameTimeStats = new Dictionary<string, TimeSpan>();

    public static readonly Dictionary<string, TimeSpan> SummedTimeStats = new Dictionary<string, TimeSpan>();

    public static readonly Dictionary<string, TimeSpan> SingleTimeStats = new Dictionary<string, TimeSpan>();

    public static readonly Dictionary<string, double> Metrics = new Dictionary<string, double>();
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
                    lock (Statistics.OncePerFrameTimeStats)
                    {
                        if (!Statistics.OncePerFrameTimeStats.TryAdd(Name, sw.Elapsed))
                        {
                            Statistics.OncePerFrameTimeStats[Name] = sw.Elapsed;
                        }
                    }
                }
                break;
            case StatisticMode.Sum:
                {
                    lock (Statistics.SummedTimeStats)
                    {
                        if (!Statistics.SummedTimeStats.TryAdd(Name, sw.Elapsed))
                        {
                            Statistics.SummedTimeStats[Name] += sw.Elapsed;
                        }
                    }
                }
                break;
            case StatisticMode.Single:
                {
                    Statistics.SingleTimeStats.Add(Name, sw.Elapsed);
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