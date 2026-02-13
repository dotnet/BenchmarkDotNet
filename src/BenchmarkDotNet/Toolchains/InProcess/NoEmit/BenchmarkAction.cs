using System;

#nullable enable

namespace BenchmarkDotNet.Toolchains.InProcess.NoEmit
{
    /// <summary>Common API to run the Setup/Clean/Idle/Run methods</summary>
    internal abstract class BenchmarkAction
    {
        /// <summary>Gets or sets invoke single callback.</summary>
        /// <value>Invoke single callback.</value>
        public Action InvokeSingle { get; protected set; } = default!;

        /// <summary>Gets or sets invoke multiple times callback.</summary>
        /// <value>Invoke multiple times callback.</value>
        public Action<long> InvokeUnroll { get; protected set; } = default!;
        
        public Action<long> InvokeNoUnroll { get; protected set; } = default!;
    }
}