using JetBrains.Annotations;
using System;
using System.Collections.Generic;
using System.Text;

namespace BenchmarkDotNet.Diagnosers
{
    public class ThreadingDiagnoserConfig
    {
        /// <param name="displayLockContentionWhenZero">Display configuration for 'LockContentionCount' when it is empty. True (displayed) by default.</param>
        /// <param name="displayCompletedWorkItemCountWhenZero">Display configuration for 'CompletedWorkItemCount' when it is empty. True (displayed) by default.</param>

        [PublicAPI]
        public ThreadingDiagnoserConfig(bool displayLockContentionWhenZero = true, bool displayCompletedWorkItemCountWhenZero = true)
        {
            DisplayLockContentionWhenZero = displayLockContentionWhenZero;
            DisplayCompletedWorkItemCountWhenZero = displayCompletedWorkItemCountWhenZero;
        }

        public bool DisplayLockContentionWhenZero { get; }
        public bool DisplayCompletedWorkItemCountWhenZero { get; }
    }
}
