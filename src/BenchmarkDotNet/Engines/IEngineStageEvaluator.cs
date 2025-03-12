using System.Collections.Generic;
using BenchmarkDotNet.Reports;

namespace BenchmarkDotNet.Engines
{
    internal interface IEngineStageEvaluator
    {
        bool EvaluateShouldStop(List<Measurement> measurements, ref long invokeCount);
        int MaxIterationCount { get; }
    }
}