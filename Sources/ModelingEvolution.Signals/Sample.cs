using System.Diagnostics;

namespace ModelingEvolution.Signals;

public readonly record struct Sample<T>(long TimestampUs, T Value)
{
    public static long NowUs => (long)(Stopwatch.GetTimestamp() * (1_000_000.0 / Stopwatch.Frequency));
}
