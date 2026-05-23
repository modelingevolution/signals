using ModelingEvolution.Drawing;

namespace ModelingEvolution.Signals;

public abstract record SignalCadence
{
    public sealed record Periodic(Frequency<float> Rate) : SignalCadence;
    public sealed record EventDriven : SignalCadence;
    public sealed record Unknown : SignalCadence;
}
