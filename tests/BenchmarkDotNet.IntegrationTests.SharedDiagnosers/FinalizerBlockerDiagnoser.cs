using System.Runtime.CompilerServices;
using BenchmarkDotNet.Analysers;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Engines;
using BenchmarkDotNet.Environments;
using BenchmarkDotNet.Exporters;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Reports;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Validators;

namespace BenchmarkDotNet.IntegrationTests.SharedDiagnosers;

// Lives in its own netstandard2.0 project so it can be referenced by every benchmark target framework
// (net472, net8.0, net10.0). The in-process handler is compiled into the benchmarks, so it must be reachable
// from both the main IntegrationTests project and the net8.0 Mono benchmarks project.
public sealed class FinalizerBlockerDiagnoser : IInProcessDiagnoser
{
    public IEnumerable<string> Ids => [nameof(FinalizerBlockerDiagnoser)];
    public IEnumerable<IExporter> Exporters => [];
    public IEnumerable<IAnalyser> Analysers => [];
    public void DeserializeResults(BenchmarkCase benchmarkCase, string serializedResults) { }
    public void DisplayResults(ILogger logger) { }
    public IAsyncEnumerable<ValidationError> ValidateAsync(ValidationParameters validationParameters) => AsyncEnumerable.Empty<ValidationError>();
    public IEnumerable<Metric> ProcessResults(DiagnoserResults results) => [];
    public ValueTask HandleAsync(HostSignal signal, DiagnoserActionParameters parameters, CancellationToken cancellationToken) => new();
    public RunMode GetRunMode(BenchmarkCase benchmarkCase)
        // Wasm throws PlatformNotSupportedException from Monitor.Wait, and defers finalization to the JS event loop (single-threaded),
        // so it's impossible for us to prevent the finalizer from running. The good thing is that means it cannot run during our synchronous
        // benchmarks, but it also means we should never add any async-yielding benchmark memory tests for Wasm.
        => benchmarkCase.GetRuntime() is WasmRuntime
            ? RunMode.None
            : RunMode.ExtraIteration;
    public InProcessDiagnoserHandlerData GetHandlerData(BenchmarkCase benchmarkCase) => new(typeof(FinalizerBlockerDiagnoserHandler), null);
}

public sealed class FinalizerBlockerDiagnoserHandler : IInProcessDiagnoserHandler
{
    private object? hangLock;

    private sealed class Impl
    {
        // ManualResetEvent(Slim) allocates when it is waited and yields the thread,
        // so we use Monitor.Wait instead which does not allocate managed memory.
        // This behavior is not documented, but was observed with the VS Profiler.
        private readonly object hangLock = new();
        private readonly ManualResetEventSlim enteredFinalizerEvent = new(false);

        ~Impl()
        {
            lock (hangLock)
            {
                enteredFinalizerEvent.Set();
                Monitor.Wait(hangLock);
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static (object hangLock, ManualResetEventSlim enteredFinalizerEvent) CreateWeakly()
        {
            var impl = new Impl();
            return (impl.hangLock, impl.enteredFinalizerEvent);
        }
    }

    private void Start()
    {
        (hangLock, var enteredFinalizerEvent) = Impl.CreateWeakly();
        do
        {
            GC.Collect();
            // Do NOT call GC.WaitForPendingFinalizers.
        }
        while (!enteredFinalizerEvent.IsSet);
    }

    private void Stop()
    {
        lock (hangLock!)
        {
            Monitor.Pulse(hangLock);
        }
    }

    public ValueTask HandleAsync(BenchmarkSignal signal, InProcessDiagnoserActionArgs args, CancellationToken cancellationToken)
    {
        switch (signal)
        {
            case BenchmarkSignal.BeforeExtraIteration:
                Start();
                break;
            case BenchmarkSignal.AfterExtraIteration:
                Stop();
                break;
        }
        return new();
    }

    public void Initialize(string? serializedConfig) { }
    public string SerializeResults() => string.Empty;
}
