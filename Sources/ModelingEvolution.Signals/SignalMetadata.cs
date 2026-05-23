namespace ModelingEvolution.Signals;

public sealed record SignalMetadata(
    string Name,
    Uri Uri,
    Type ValueType,
    string? Unit,
    SignalCadence Cadence,
    int? PayloadBytesHint
);
