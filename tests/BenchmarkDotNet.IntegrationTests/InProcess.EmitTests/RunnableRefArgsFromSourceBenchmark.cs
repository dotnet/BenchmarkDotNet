using BenchmarkDotNet.Attributes;
// ReSharper disable UnusedMember.Global

namespace BenchmarkDotNet.IntegrationTests.InProcess.EmitTests;

// A source's values are rendered inline when SourceCodeHelper can embed them, exactly as [Arguments] values are,
// so a `ref` parameter only reaches the field the extractor fills when its value is a type it cannot - RefArgBox
// here. The int and string beside it stay constants, so one call mixes both forms.
public readonly struct RefArgBox(double value)
{
    public double Value { get; } = value;
}

public class RunnableRefArgsFromSourceBenchmark
{
    public static IEnumerable<object[]> ManyArgs()
    {
        yield return new object[] { new RefArgBox(123.0), 4, "5" };
    }

    public static IEnumerable<RefArgBox> SingleArg()
    {
        yield return new RefArgBox(123.0);
    }

    private int refResultHolder;

    [Benchmark, ArgumentsSource(nameof(SingleArg))]
    public double RefArgFromSourceCase(ref RefArgBox arg0) => arg0.Value;

    // No `in` case: the compiler puts [IsReadOnly] on an `in` parameter and RunnableEmitter does not, so the
    // two runnables differ on any benchmark taking one, from [Arguments] as readily as from a source.

    [Benchmark, ArgumentsSource(nameof(ManyArgs))]
    public double ManyRefArgsFromSourceCase(ref RefArgBox arg0, int arg1, string arg2) => arg0.Value;

    [Benchmark, ArgumentsSource(nameof(ManyArgs))]
    public ref int RefReturnRefArgFromSourceCase(ref RefArgBox arg0, int arg1, string arg2) => ref refResultHolder;
}
