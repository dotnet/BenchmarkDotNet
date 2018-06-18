using System;
using System.Linq;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Parameters;

namespace BenchmarkDotNet.Running
{
    public class BenchmarkCase : IComparable<BenchmarkCase>
    {
        public Target Target { get; }
        public Job Job { get; }
        public ParameterInstances Parameters { get; }

        public string FolderInfo => (Target.FolderInfo + "_" + Job.FolderInfo + "_" + Parameters.FolderInfo).Trim('_');
        public string DisplayInfo => (Target.DisplayInfo + ": " + Job.DisplayInfo + " " + Parameters.DisplayInfo).Trim(' ');

        public override string ToString() => DisplayInfo;

        private BenchmarkCase(Target target, Job job, ParameterInstances parameters)
        {
            Target = target;
            Job = job;
            Parameters = parameters;
        }

        public int CompareTo(BenchmarkCase other) => string.Compare(FolderInfo, other.FolderInfo, StringComparison.Ordinal);

        public bool IsBaseline() => Target.Baseline || Job.Meta.IsBaseline;

        public bool HasParameters => Parameters != null && Parameters.Items.Any();

        public bool HasArguments => Parameters != null && Parameters.Items.Any(parameter => parameter.IsArgument);

        public static BenchmarkCase Create(Target target, Job job, ParameterInstances parameters)
            => new BenchmarkCase(target, job.MakeSettingsUserFriendly(target), parameters);
    }
}