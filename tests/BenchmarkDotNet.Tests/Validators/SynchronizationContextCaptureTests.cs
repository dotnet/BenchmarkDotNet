using BenchmarkDotNet.Analysers;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Engines;
using BenchmarkDotNet.Exporters;
using BenchmarkDotNet.Helpers;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Reports;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Validators;
using System.Collections.Immutable;

namespace BenchmarkDotNet.Tests.Validators;

/// <summary>
/// BenchmarkDotNet never installs a SynchronizationContext.Current, so whatever is ambient around a run is still
/// ambient inside it. Nothing that drives a suspending sequence may capture it: the continuation would be posted to
/// the caller's context, which may be single-threaded and - while the pump blocks its thread - unable to run it.
/// This is why the composites are written out instead of composed with async LINQ, whose operators decide for
/// themselves whether to capture.
/// </summary>
public class SynchronizationContextCaptureTests
{
    [Fact]
    public void CompositeValidatorDoesNotCaptureTheAmbientContext()
    {
        var errors = DrainUnderPump(
            () => new CompositeValidator([new SuspendingValidator("first"), new SuspendingValidator("second")])
                .ValidateAsync(Array.Empty<BenchmarkCase>()));

        // The validators are held in an ImmutableHashSet, so the order they are visited in is unspecified.
        Assert.Equal(["first", "second"], errors.Select(error => error.Message).OrderBy(message => message));
    }

    [Fact]
    public void CompositeValidatorDeduplicatesErrors()
    {
        // The rewrite replaced async LINQ's Distinct(); the behaviour it stood for has to survive.
        var errors = DrainUnderPump(
            () => new CompositeValidator([new SuspendingValidator("same"), new SuspendingValidator("same")])
                .ValidateAsync(Array.Empty<BenchmarkCase>()));

        Assert.Equal(["same"], errors.Select(error => error.Message));
    }

    [Fact]
    public void CompositeDiagnoserDoesNotCaptureTheAmbientContext()
    {
        var errors = DrainUnderPump(
            () => new CompositeDiagnoser([new SuspendingDiagnoser("diagnoser")])
                .ValidateAsync(Array.Empty<BenchmarkCase>()));

        Assert.Equal(["diagnoser"], errors.Select(error => error.Message));
    }

    // Drives the sequence the way a run does - under BenchmarkDotNet's pump, on a thread carrying an ambient context -
    // and asserts the ambient one is never posted to. The pump is what makes this meaningful: without it the pumping
    // ConfigureAwait falls back to ConfigureAwait(true) and capturing the caller's context is the intended behaviour.
    private static List<ValidationError> DrainUnderPump(Func<IAsyncEnumerable<ValidationError>> sequence)
    {
        var recording = new RecordingSynchronizationContext();
        var original = SynchronizationContext.Current;
        SynchronizationContext.SetSynchronizationContext(recording);
        try
        {
            using var pump = BenchmarkSynchronizationContext.CreateAndSetCurrent();
            var drained = pump.ExecuteUntilComplete(DrainAsync(sequence()));

            Assert.Equal(0, recording.PostCount);
            Assert.Equal(0, recording.SendCount);
            return drained;
        }
        finally
        {
            SynchronizationContext.SetSynchronizationContext(original);
        }
    }

    private static async ValueTask<List<ValidationError>> DrainAsync(IAsyncEnumerable<ValidationError> errors)
    {
        var drained = new List<ValidationError>();
#pragma warning disable CA2007
        await foreach (var error in errors.ConfigureAwait())
#pragma warning restore CA2007
        {
            drained.Add(error);
        }
        return drained;
    }

    private sealed class RecordingSynchronizationContext : SynchronizationContext
    {
        private int postCount;
        private int sendCount;

        public int PostCount => Volatile.Read(ref postCount);
        public int SendCount => Volatile.Read(ref sendCount);

        public override void Post(SendOrPostCallback d, object? state)
        {
            Interlocked.Increment(ref postCount);
            base.Post(d, state);
        }

        public override void Send(SendOrPostCallback d, object? state)
        {
            Interlocked.Increment(ref sendCount);
            base.Send(d, state);
        }
    }

    // Suspends before yielding, and configures its own await away so only the consumer's machinery could capture.
    private sealed class SuspendingValidator(string message) : IValidator
    {
        public bool TreatsWarningsAsErrors => true;

        public async IAsyncEnumerable<ValidationError> ValidateAsync(ValidationParameters validationParameters)
        {
            await Task.Delay(1).ConfigureAwait(false);
            yield return new ValidationError(TreatsWarningsAsErrors, message);
        }
    }

    private sealed class SuspendingDiagnoser(string message) : IDiagnoser
    {
        public IEnumerable<string> Ids => [message];
        public IEnumerable<IExporter> Exporters => [];
        public IEnumerable<IAnalyser> Analysers => [];
        public RunMode GetRunMode(BenchmarkCase benchmarkCase) => RunMode.None;
        public ValueTask HandleAsync(HostSignal signal, DiagnoserActionParameters parameters, CancellationToken cancellationToken) => default;
        public IEnumerable<Metric> ProcessResults(DiagnoserResults results) => [];
        public void DisplayResults(ILogger logger) { }

        public async IAsyncEnumerable<ValidationError> ValidateAsync(ValidationParameters validationParameters)
        {
            await Task.Delay(1).ConfigureAwait(false);
            yield return new ValidationError(true, message);
        }
    }
}
