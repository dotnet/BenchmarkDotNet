using Perfolizer.Horology;

namespace BenchmarkDotNet.Toolchains.InProcess.NoEmit;

public interface IBenchmarkAction
{
    Func<ValueTask> InvokeSingle { get; }
    Func<long, IClock, ValueTask<ClockSpan>> InvokeUnroll { get; }
    Func<long, IClock, ValueTask<ClockSpan>> InvokeNoUnroll { get; }
    void Complete();
}