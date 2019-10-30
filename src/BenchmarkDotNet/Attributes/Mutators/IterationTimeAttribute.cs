﻿using BenchmarkDotNet.Horology;
using BenchmarkDotNet.Jobs;
using JetBrains.Annotations;

namespace BenchmarkDotNet.Attributes
{
    /// <summary>
    /// Desired time of execution of an iteration. Used by Pilot stage to estimate the number of invocations per iteration.
    /// The default value is 500 milliseconds.
    /// </summary>
    [PublicAPI]
    public class IterationTimeAttribute : JobMutatorConfigBaseAttribute
    {
        public IterationTimeAttribute(double milliseconds) : base(Job.Default.WithIterationTime(TimeValue.FromMilliseconds(milliseconds)))
        {
        }
    }
}