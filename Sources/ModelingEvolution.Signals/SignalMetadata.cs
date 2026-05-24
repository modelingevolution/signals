using ModelingEvolution.Drawing;

namespace ModelingEvolution.Signals;

public sealed record SignalMetadata(
    string Name,
    Uri Uri,
    string? Unit,
    Frequency<float>? Cadence);
