using FluentAssertions;
using ModelingEvolution.Drawing;
using Xunit;

namespace ModelingEvolution.Signals.Tests;

public sealed class WritableSignalTests
{
    private static SignalMetadata IntMetadata() => new(
        Name: "test.signal",
        Uri: new Uri("signal://test/value"),
        Unit: "V",
        Cadence: Frequency<float>.FromHertz(10f));

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
    public void Cadence_Periodic_RoundTrip()
    {
        var cadence = Frequency<float>.FromHertz(10f);
        var metadata = new SignalMetadata(
            Name: "test",
            Uri: new Uri("signal://test"),
            Unit: "Hz",
            Cadence: cadence);

        metadata.Cadence.Should().NotBeNull();
        metadata.Cadence!.Value.Hertz.Should().BeApproximately(10f, 1e-6f);
    }

    [Fact]
    public void Cadence_Null_Allowed_ForEventDrivenOrUnknown()
    {
        var metadata = new SignalMetadata(
            Name: "test",
            Uri: new Uri("signal://test"),
            Unit: null,
            Cadence: null);

        metadata.Cadence.Should().BeNull();
    }

    [Fact]
    public void SignalMetadata_PropertiesPreserved()
    {
        var uri = new Uri("signal://device/temperature");
        var cadence = Frequency<float>.FromHertz(50f);

        var metadata = new SignalMetadata(
            Name: "temperature",
            Uri: uri,
            Unit: "C",
            Cadence: cadence);

        metadata.Name.Should().Be("temperature");
        metadata.Uri.Should().Be(uri);
        metadata.Unit.Should().Be("C");
        metadata.Cadence.Should().Be(cadence);
    }

    [Fact]
    public void Set_T_And_Set_Sample_ProduceEquivalentObservableBehavior()
    {
        // Both overloads must end at the same code path; observers see Sample<T> either way.
        var signalA = new WritableSignal<int>(IntMetadata());
        var signalB = new WritableSignal<int>(IntMetadata());
        var receivedA = new List<Sample<int>>();
        var receivedB = new List<Sample<int>>();

        using var _a = signalA.Subscribe(receivedA.Add);
        using var _b = signalB.Subscribe(receivedB.Add);

        signalA.Set(42);
        signalB.Set(new Sample<int>(Sample.NowUs, 42));

        receivedA.Should().HaveCount(1);
        receivedB.Should().HaveCount(1);
        receivedA[0].Value.Should().Be(receivedB[0].Value);
        signalA.Value.Should().Be(signalB.Value);
        signalA.HasValue.Should().Be(signalB.HasValue);
    }

    [Fact]
    public void Set_Sample_PreservesProvidedTimestamp()
    {
        // Set(Sample<T>) lets callers thread an external timestamp through unchanged.
        var signal = new WritableSignal<int>(IntMetadata());
        var received = new List<Sample<int>>();
        using var _ = signal.Subscribe(received.Add);
        var fixedTs = 1_234_567L;

        signal.Set(new Sample<int>(fixedTs, 99));

        received.Should().HaveCount(1);
        received[0].TimestampUs.Should().Be(fixedTs);
        received[0].Value.Should().Be(99);
    }

    [Fact]
    public void Set_Sample_RespectsDistinctUntilChanged()
    {
        var signal = new WritableSignal<int>(IntMetadata());
        var callCount = 0;
        using var _ = signal.Subscribe(_ => callCount++);

        signal.Set(new Sample<int>(100L, 5));
        signal.Set(new Sample<int>(200L, 5));

        callCount.Should().Be(1);
    }

    [Fact]
    public void Sample_NowUs_ReturnsPositiveMonotonic()
    {
        var t1 = Sample.NowUs;
        var t2 = Sample.NowUs;

        t1.Should().BeGreaterThan(0);
        t2.Should().BeGreaterThanOrEqualTo(t1);
    }
}
