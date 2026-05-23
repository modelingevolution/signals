namespace ModelingEvolution.Signals;

public interface ISignal<T>
{
    SignalMetadata Metadata { get; }
    bool HasValue { get; }
    T? Value { get; }
    IDisposable Subscribe(Action<Sample<T>> onSample);
}
