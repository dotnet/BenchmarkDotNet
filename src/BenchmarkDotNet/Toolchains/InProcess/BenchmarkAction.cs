using Perfolizer.Horology;
using System;
using System.Threading.Tasks;

namespace BenchmarkDotNet.Toolchains.InProcess
{
    /// <summary>Common API to run the Setup/Clean/Idle/Run methods</summary>
    [Obsolete("Please use BenchmarkDotNet.Toolchains.InProcess.NoEmit.* classes")]
    public abstract class BenchmarkAction
    {
        public Func<ValueTask> InvokeSingle { get; protected set; }
        public Func<long, IClock, ValueTask<ClockSpan>> InvokeUnroll { get; protected set; }
        public Func<long, IClock, ValueTask<ClockSpan>> InvokeNoUnroll { get; protected set; }
        public virtual object LastRunResult => null;
    }
}