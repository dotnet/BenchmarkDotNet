using System;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Parameters;

namespace BenchmarkDotNet.Running
{
    public class Benchmark : IComparable<Benchmark>
    {
        public Target Target { get; }
        public IJob Job { get; }
        public ParameterInstances Parameters { get; }

        public string ShortInfo => (Target.FullInfo + "_" + Job.GetShortInfo() + "_" + Parameters.FullInfo).Trim('_');
        public string FullInfo => (Target.FullInfo + "_" + Job.GetFullInfo() + "_" + Parameters.FullInfo).Trim('_');
        public override string ToString() => FullInfo;

        public Benchmark(Target target, IJob job, ParameterInstances parameters)
        {
            Target = target;
            Job = job;
            Parameters = parameters;
        }

        public int CompareTo(Benchmark other) => string.Compare(FullInfo, other.FullInfo, StringComparison.Ordinal);
    }
}