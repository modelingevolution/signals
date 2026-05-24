using System.Collections.Immutable;

namespace ModelingEvolution.Signals;

public sealed class WritableSignal<T> : ISignal<T>, ISignalSink<T>, IDisposable
{
    private sealed record Holder(T Value);

    private readonly SignalMetadata _metadata;
    private Holder? _current;
    private ImmutableArray<Action<Sample<T>>> _subscribers = ImmutableArray<Action<Sample<T>>>.Empty;
    private volatile bool _disposed;

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

    public void Set(T value) => Set(new Sample<T>(Sample<T>.NowUs, value));

    public void Set(Sample<T> sample)
    {
        if (_disposed) return;
        var current = Volatile.Read(ref _current);
        if (current is not null && EqualityComparer<T>.Default.Equals(current.Value, sample.Value)) return;
        Volatile.Write(ref _current, new Holder(sample.Value));
        var subs = _subscribers;
        foreach (var cb in subs) cb(sample);
    }

    public IDisposable Subscribe(Action<Sample<T>> onSample)
    {
        ImmutableInterlocked.Update(ref _subscribers, s => s.Add(onSample));
        return new Subscription(this, onSample);
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        ImmutableInterlocked.Update(ref _subscribers, _ => ImmutableArray<Action<Sample<T>>>.Empty);
    }

    private sealed class Subscription(WritableSignal<T> signal, Action<Sample<T>> callback) : IDisposable
    {
        public void Dispose() =>
            ImmutableInterlocked.Update(ref signal._subscribers, s => s.Remove(callback));
    }
}
