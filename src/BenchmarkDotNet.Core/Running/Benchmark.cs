using System;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Parameters;

namespace BenchmarkDotNet.Running
{
    public class Benchmark : IComparable<Benchmark>
    {
        public Target Target { get; }
        public Job Job { get; }
        public ParameterInstances Parameters { get; }

        public string FolderInfo => (Target.FolderInfo + "_" + Job.FolderInfo + "_" + Parameters.FolderInfo).Trim('_');
        public string DisplayInfo => (Target.DisplayInfo + ": " + Job.DisplayInfo + " " + Parameters.DisplayInfo).Trim(' ');

        public override string ToString() => DisplayInfo;

        public Benchmark(Target target, Job job, ParameterInstances parameters)
        {
            Target = target;
            Job = job;
            Parameters = parameters;
        }

        public int CompareTo(Benchmark other) => string.Compare(FolderInfo, other.FolderInfo, StringComparison.Ordinal);

        public bool IsBaseline() => Target.Baseline || Job.Meta.IsBaseline;
    }
}