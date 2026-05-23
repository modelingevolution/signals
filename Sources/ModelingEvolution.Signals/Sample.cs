namespace ModelingEvolution.Signals;

public readonly record struct Sample<T>(long TimestampUs, T Value);
