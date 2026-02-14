using Perfolizer.Horology;
using System;
using System.Threading.Tasks;

#nullable enable

namespace BenchmarkDotNet.Toolchains.InProcess.NoEmit;

internal abstract class BenchmarkAction
{
    public Func<ValueTask> InvokeSingle { get; protected set; } = default!;
    public Func<long, IClock, ValueTask<ClockSpan>> InvokeUnroll { get; protected set; } = default!;
    public Func<long, IClock, ValueTask<ClockSpan>> InvokeNoUnroll { get; protected set; } = default!;
    public abstract void Complete();
}