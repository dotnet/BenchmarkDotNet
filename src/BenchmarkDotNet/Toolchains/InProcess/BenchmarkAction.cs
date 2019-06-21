using System;

namespace BenchmarkDotNet.Toolchains.InProcess
{
    /// <summary>Common API to run the Setup/Clean/Idle/Run methods</summary>
    [Obsolete("Please use BenchmarkDotNet.Toolchains.InProcess.NoEmit.* classes")]
    public abstract class BenchmarkAction
    {
        /// <summary>Gets or sets invoke single callback.</summary>
        /// <value>Invoke single callback.</value>
        public Action InvokeSingle { get; protected set; }

        /// <summary>Gets or sets invoke multiple times callback.</summary>
        /// <value>Invoke multiple times callback.</value>
        public Action<long> InvokeMultiple { get; protected set; }

        /// <summary>Gets the last run result.</summary>
        /// <value>The last run result.</value>
        public virtual object LastRunResult => null;
    }
}