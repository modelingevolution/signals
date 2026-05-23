using System.Collections.Immutable;
using System.Diagnostics;

namespace ModelingEvolution.Signals;

public sealed class WritableSignal<T> : ISignal<T>, ISignalSink<T>
{
    private sealed record Holder(T Value);

    private readonly SignalMetadata _metadata;
    private Holder? _current;
    private ImmutableArray<Action<Sample<T>>> _subscribers = ImmutableArray<Action<Sample<T>>>.Empty;

    public WritableSignal(SignalMetadata metadata) => _metadata = metadata;

    public SignalMetadata Metadata => _metadata;

    public bool HasValue => Volatile.Read(ref _current) is not null;

    public T? Value
    {
        get
        {
            var current = Volatile.Read(ref _current);
            return current is not null ? current.Value : default;
        }
    }

    public void Set(T value)
    {
        var current = Volatile.Read(ref _current);
        if (current is not null && EqualityComparer<T>.Default.Equals(current.Value, value)) return;
        Volatile.Write(ref _current, new Holder(value));
        var sample = new Sample<T>(GetTimestampUs(), value);
        var subs = _subscribers;
        foreach (var cb in subs) cb(sample);
    }

    public IDisposable Subscribe(Action<Sample<T>> onSample)
    {
        ImmutableInterlocked.Update(ref _subscribers, s => s.Add(onSample));
        return new Subscription(this, onSample);
    }

    private static long GetTimestampUs() =>
        (long)(Stopwatch.GetTimestamp() * (1_000_000.0 / Stopwatch.Frequency));

    private sealed class Subscription(WritableSignal<T> signal, Action<Sample<T>> callback) : IDisposable
    {
        public void Dispose() =>
            ImmutableInterlocked.Update(ref signal._subscribers, s => s.Remove(callback));
    }
}
