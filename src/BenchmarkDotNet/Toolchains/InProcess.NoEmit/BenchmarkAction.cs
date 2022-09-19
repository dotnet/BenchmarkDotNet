using System;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Perfolizer.Horology;

namespace BenchmarkDotNet.Toolchains.InProcess.NoEmit
{
    /// <summary>Common API to run the Setup/Clean/Idle/Run methods</summary>
    [PublicAPI]
    public abstract class BenchmarkAction
    {
        public Func<ValueTask> InvokeSingle { get; protected set; }
        public Func<long, IClock, ValueTask<ClockSpan>> InvokeUnroll { get; protected set; }
        public Func<long, IClock, ValueTask<ClockSpan>> InvokeNoUnroll { get; protected set; }
        public virtual object LastRunResult => null;
    }
}