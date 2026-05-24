using FluentAssertions;
using ModelingEvolution.Drawing;
using Xunit;

namespace ModelingEvolution.Signals.Tests;

public sealed class WritableSignalTests
{
    private static SignalMetadata IntMetadata() => new(
        Name: "test.signal",
        Uri: new Uri("signal://test/value"),
        ValueType: typeof(int),
        Unit: "V",
        Cadence: new SignalCadence.Periodic(Frequency<float>.FromHertz(10f)),
        PayloadBytesHint: 4);

    [Fact]
    public void Value_HasNoValue_BeforeFirstSet()
    {
        var signal = new WritableSignal<int>(IntMetadata());

        signal.HasValue.Should().BeFalse();
        signal.Value.Should().Be(default(int));
    }

    [Fact]
    public void Value_ReturnsLatestAfterSet()
    {
        var signal = new WritableSignal<int>(IntMetadata());

        signal.Set(42);

        signal.HasValue.Should().BeTrue();
        signal.Value.Should().Be(42);
    }

    [Fact]
    public void Subscribe_ReceivesSample_OnSet()
    {
        var signal = new WritableSignal<int>(IntMetadata());
        var received = new List<Sample<int>>();

        using var _ = signal.Subscribe(received.Add);
        signal.Set(99);

        received.Should().HaveCount(1);
        received[0].Value.Should().Be(99);
        received[0].TimestampUs.Should().BeGreaterThan(0);
    }

    [Fact]
    public void DistinctUntilChanged_DoesNotFire_WhenValueUnchanged()
    {
        var signal = new WritableSignal<int>(IntMetadata());
        var callCount = 0;

        using var _ = signal.Subscribe(_ => callCount++);
        signal.Set(5);
        signal.Set(5);

        callCount.Should().Be(1);
    }

    [Fact]
    public void Dispose_DetachesSubscriber()
    {
        var signal = new WritableSignal<int>(IntMetadata());
        var callCount = 0;

        var subscription = signal.Subscribe(_ => callCount++);
        subscription.Dispose();
        signal.Set(1);

        callCount.Should().Be(0);
    }

    [Fact]
    public void Dispose_DoesNotAffectOtherSubscribers()
    {
        var signal = new WritableSignal<int>(IntMetadata());
        var aCount = 0;
        var bCount = 0;

        var subA = signal.Subscribe(_ => aCount++);
        using var subB = signal.Subscribe(_ => bCount++);

        subA.Dispose();
        signal.Set(1);

        aCount.Should().Be(0);
        bCount.Should().Be(1);
    }

    [Fact]
    public void TimestampUs_IsMonotonicallyIncreasing()
    {
        var signal = new WritableSignal<int>(IntMetadata());
        var samples = new List<Sample<int>>();

        using var _ = signal.Subscribe(samples.Add);
        signal.Set(1);
        signal.Set(2);

        samples.Should().HaveCount(2);
        samples[1].TimestampUs.Should().BeGreaterThanOrEqualTo(samples[0].TimestampUs);
    }

    [Fact]
    public void Dispose_ClearsAllSubscribers()
    {
        var signal = new WritableSignal<int>(IntMetadata());
        var aCount = 0;
        var bCount = 0;

        signal.Subscribe(_ => aCount++);
        signal.Subscribe(_ => bCount++);
        signal.Dispose();
        signal.Set(1);

        aCount.Should().Be(0);
        bCount.Should().Be(0);
    }

    [Fact]
    public void Set_AfterDispose_IsSilentNoOp()
    {
        var signal = new WritableSignal<int>(IntMetadata());

        signal.Set(7);
        signal.Dispose();
        signal.Set(42);

        signal.Value.Should().Be(7);
    }

    [Fact]
    public void Dispose_IsIdempotent()
    {
        var signal = new WritableSignal<int>(IntMetadata());

        var act = () =>
        {
            signal.Dispose();
            signal.Dispose();
            signal.Dispose();
        };

        act.Should().NotThrow();
    }

    [Fact]
    public void SignalCadence_Periodic_RoundTrip()
    {
        var cadence = new SignalCadence.Periodic(Frequency<float>.FromHertz(10f));

        cadence.Rate.Hertz.Should().BeApproximately(10f, 1e-6f);
    }

    [Fact]
    public void SignalMetadata_PropertiesPreserved()
    {
        var uri = new Uri("signal://device/temperature");
        var cadence = new SignalCadence.Periodic(Frequency<float>.FromHertz(50f));

        var metadata = new SignalMetadata(
            Name: "temperature",
            Uri: uri,
            ValueType: typeof(double),
            Unit: "C",
            Cadence: cadence,
            PayloadBytesHint: 8);

        metadata.Name.Should().Be("temperature");
        metadata.Uri.Should().Be(uri);
        metadata.ValueType.Should().Be(typeof(double));
        metadata.Unit.Should().Be("C");
        metadata.Cadence.Should().BeSameAs(cadence);
        metadata.PayloadBytesHint.Should().Be(8);
    }
}
