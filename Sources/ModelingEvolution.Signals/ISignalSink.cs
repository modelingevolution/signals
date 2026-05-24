namespace ModelingEvolution.Signals;

public interface ISignalSink<T>
{
    SignalMetadata Metadata { get; }
    void Set(T value);
    void Set(Sample<T> sample);
}
