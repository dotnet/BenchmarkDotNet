using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Code;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Reports;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Toolchains;
using BenchmarkDotNet.Toolchains.InProcess.Emit;
using BenchmarkDotNet.Toolchains.InProcess.NoEmit;

namespace BenchmarkDotNet.IntegrationTests;

// Covers IAsyncEnumerable sources for [ParamsSource] and [ArgumentsSource] (issue #3120).
// Non-constant values (custom reference types) are used on purpose so the smart-parameter code path
// (host discovery + generated async initialization) is exercised rather than embedded literals.
public class AsyncEnumerableParamsSourceTests(ITestOutputHelper output) : BenchmarkTestExecutor(output)
{
    // For [ArgumentsSource] tests. InProcessNoEmitToolchain is intentionally omitted: it doesn't support arguments (see #687).
    public static IEnumerable<object[]> GetToolchains()
    {
        yield return [InProcessEmitToolchain.Default];

        if (ContinuousIntegration.IsGitHubDraftPR())
            yield break;

        yield return [Job.Default.GetToolchain()];
    }

    // For [ParamsSource] tests, which all in-process toolchains support.
    public static IEnumerable<object[]> GetParamsToolchains()
    {
        yield return [InProcessNoEmitToolchain.Default];
        foreach (var toolchain in GetToolchains())
            yield return toolchain;
    }

    private Summary Run(Type type, IToolchain toolchain)
    {
        IConfig config = CreateSimpleConfig(job: Job.Dry.WithToolchain(toolchain));
        if (!toolchain.IsInProcess)
        {
            // Show the relevant codegen excerpt in test results (the *.notcs is not part of the logs)
            Output.WriteLine("// Benchmarks and CodeGenerator.GetParamsContent()");
            BenchmarkRunInfo runInfo = BenchmarkConverter.TypeToBenchmarks(type, config);
            foreach (BenchmarkCase benchmarkCase in runInfo.BenchmarksCases)
            {
                Output.WriteLine("//   " + benchmarkCase.DisplayInfo);
                Output.WriteLine(CodeGenerator.GetParamsInitializer(benchmarkCase));
            }
        }
        return CanExecute(type, config);
    }

    public class Item
    {
        public int Data { get; init; }
        public override string ToString() => "item" + Data;
    }

    public class StaticParamsSource
    {
        public static async IAsyncEnumerable<object> GetValues()
        {
            await Task.Yield();
            yield return new Item { Data = 1 };
            await Task.Delay(1);
            yield return new Item { Data = 2 };
            yield return new Item { Data = 3 };
        }

        [ParamsSource(nameof(GetValues))]
        public Item Target { get; set; } = null!;

        [Benchmark]
        public int Benchmark()
            => Target.Data > 0 ? Target.Data : throw new InvalidOperationException("Value was not set");
    }

    [Theory, MemberData(nameof(GetParamsToolchains), DisableDiscoveryEnumeration = true)]
    public void StaticAsyncParamsSource_Succeeds(IToolchain toolchain) => Run(typeof(StaticParamsSource), toolchain);

    public class InstanceParamsSource
    {
        public async IAsyncEnumerable<object> GetValues()
        {
            await Task.Yield();
            yield return new Item { Data = 10 };
            yield return new Item { Data = 20 };
        }

        [ParamsSource(nameof(GetValues))]
        public Item Target { get; set; } = null!;

        [Benchmark]
        public int Benchmark()
            => Target.Data > 0 ? Target.Data : throw new InvalidOperationException("Value was not set");
    }

    [Theory, MemberData(nameof(GetParamsToolchains), DisableDiscoveryEnumeration = true)]
    public void InstanceAsyncParamsSource_Succeeds(IToolchain toolchain) => Run(typeof(InstanceParamsSource), toolchain);

    public class PropertyParamsSource
    {
        // A property getter can't be an async iterator, so it returns one from an async iterator method.
        public static IAsyncEnumerable<object> Values => GetValues();

        private static async IAsyncEnumerable<object> GetValues()
        {
            await Task.Yield();
            yield return new Item { Data = 5 };
            yield return new Item { Data = 6 };
        }

        [ParamsSource(nameof(Values))]
        public Item Target { get; set; } = null!;

        [Benchmark]
        public int Benchmark()
            => Target.Data > 0 ? Target.Data : throw new InvalidOperationException("Value was not set");
    }

    [Theory, MemberData(nameof(GetParamsToolchains), DisableDiscoveryEnumeration = true)]
    public void PropertyAsyncParamsSource_Succeeds(IToolchain toolchain) => Run(typeof(PropertyParamsSource), toolchain);

    // A source method may have all-optional parameters, e.g. an async iterator with an
    // [EnumeratorCancellation] CancellationToken; it's invoked with the default (no special handling needed).
    public class EnumeratorCancellationParamsSource
    {
        public static async IAsyncEnumerable<object> GetValues(
            [System.Runtime.CompilerServices.EnumeratorCancellation] System.Threading.CancellationToken cancellationToken = default)
        {
            await Task.Yield();
            yield return new Item { Data = 100 };
            await Task.Delay(1, cancellationToken);
            yield return new Item { Data = 200 };
        }

        [ParamsSource(nameof(GetValues))]
        public Item Target { get; set; } = null!;

        [Benchmark]
        public int Benchmark()
            => Target.Data > 0 ? Target.Data : throw new InvalidOperationException("Value was not set");
    }

    [Theory, MemberData(nameof(GetParamsToolchains), DisableDiscoveryEnumeration = true)]
    public void EnumeratorCancellationParamsSource_Succeeds(IToolchain toolchain) => Run(typeof(EnumeratorCancellationParamsSource), toolchain);

    public class SingleArgumentSource
    {
        public static async IAsyncEnumerable<object> GetArguments()
        {
            await Task.Yield();
            yield return new Item { Data = 1 };
            await Task.Delay(1);
            yield return new Item { Data = 2 };
        }

        [Benchmark]
        [ArgumentsSource(nameof(GetArguments))]
        public int Benchmark(Item item)
            => item.Data > 0 ? item.Data : throw new InvalidOperationException("Argument was not set");
    }

    [Theory, MemberData(nameof(GetToolchains), DisableDiscoveryEnumeration = true)]
    public void SingleAsyncArgumentSource_Succeeds(IToolchain toolchain) => Run(typeof(SingleArgumentSource), toolchain);

    public class MultipleArgumentsSource
    {
        public static async IAsyncEnumerable<object[]> GetArguments()
        {
            await Task.Yield();
            yield return [new Item { Data = 1 }, new Item { Data = 10 }];
            await Task.Delay(1);
            yield return [new Item { Data = 2 }, new Item { Data = 20 }];
        }

        [Benchmark]
        [ArgumentsSource(nameof(GetArguments))]
        public int Benchmark(Item first, Item second)
            => first.Data + second.Data;
    }

    [Theory, MemberData(nameof(GetToolchains), DisableDiscoveryEnumeration = true)]
    public void MultipleAsyncArgumentsSource_Succeeds(IToolchain toolchain) => Run(typeof(MultipleArgumentsSource), toolchain);

    // BenchmarkDotNet never installs a SynchronizationContext.Current, so whatever is ambient when a run starts is
    // still ambient inside it. Reading a source must not capture it: marshaling belongs to the caller, which applies
    // it with ConfigureAwait on the enumerable. The reflection path used for value-type elements handed the
    // continuation to the user's raw awaiter, which captured the context and posted to it.
// Both element kinds, because they are read through different machinery: a value-type element goes through
    // the reflection loop, a reference-type one through the covariance cast, which had no guard here at all.
    [Theory]
    [InlineData(typeof(ValueTypeAsyncParamsSource))]
    [InlineData(typeof(ReferenceTypeAsyncParamsSource))]
    public void ReadingAnAsyncSourceDoesNotPostToTheAmbientSynchronizationContext(Type benchmarkType)
    {
        var recording = new RecordingSynchronizationContext();
        var original = SynchronizationContext.Current;
        SynchronizationContext.SetSynchronizationContext(recording);
        try
        {
            Run(benchmarkType, InProcessEmitToolchain.From(new() { ExecuteOnSeparateThread = false }));
        }
        finally
        {
            SynchronizationContext.SetSynchronizationContext(original);
        }

        Assert.Equal(0, recording.PostCount);
        Assert.Equal(0, recording.SendCount);
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

    public class ValueTypeAsyncParamsSource
    {
        [ParamsSource(nameof(Values))]
        public int Value { get; set; }

        // A value-type element takes the reflection path rather than the covariance cast, and the source configures
        // its own awaits away so that only BenchmarkDotNet's could reach the ambient context.
        public static async IAsyncEnumerable<int> Values()
        {
            await Task.Delay(1).ConfigureAwait(false);
            yield return 1;
            await Task.Delay(1).ConfigureAwait(false);
            yield return 2;
        }

        [Benchmark]
        public int Benchmark() => Value;
    }

    public class ReferenceTypeAsyncParamsSource
    {
        [ParamsSource(nameof(Values))]
        public string Value { get; set; } = "";

        // A reference-type element is read through IAsyncEnumerable<T>'s covariance rather than by reflection, so
        // the awaits are the compiler's own. As above, the source configures its own away, leaving only
        // BenchmarkDotNet's able to reach the ambient context.
        public static async IAsyncEnumerable<string> Values()
        {
            await Task.Delay(1).ConfigureAwait(false);
            yield return "a";
            await Task.Delay(1).ConfigureAwait(false);
            yield return "b";
        }

        [Benchmark]
        public string Benchmark() => Value;
    }
}
