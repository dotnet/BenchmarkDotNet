using Perfolizer.Horology;
using System;
using System.Threading.Tasks;

namespace BenchmarkDotNet.Toolchains.InProcess.NoEmit;

internal abstract class BenchmarkAction
{
    public Func<ValueTask> InvokeSingle { get; protected set; }
    public Func<long, IClock, ValueTask<ClockSpan>> InvokeUnroll { get; protected set; }
    public Func<long, IClock, ValueTask<ClockSpan>> InvokeNoUnroll { get; protected set; }
    public abstract void Complete();
}