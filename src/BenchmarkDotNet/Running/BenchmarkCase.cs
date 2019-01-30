﻿using System;
using System.Linq;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Parameters;

namespace BenchmarkDotNet.Running
{
    public class BenchmarkCase : IComparable<BenchmarkCase>
    {
        public Descriptor Descriptor { get; }
        public Job Job { get; }
        public ParameterInstances Parameters { get; }
        public ImmutableConfig Config { get; }

        public string FolderInfo => (Descriptor.FolderInfo + "_" + Job.FolderInfo + "_" + Parameters.FolderInfo).Trim('_');
        public string DisplayInfo => (Descriptor.DisplayInfo + ": " + Job.DisplayInfo + " " + Parameters.DisplayInfo).Trim(' ');

        public override string ToString() => DisplayInfo;

        private BenchmarkCase(Descriptor descriptor, Job job, ParameterInstances parameters, ImmutableConfig config)
        {
            Descriptor = descriptor;
            Job = job;
            Parameters = parameters;
            Config = config;
        }

        public int CompareTo(BenchmarkCase other) => string.Compare(FolderInfo, other.FolderInfo, StringComparison.Ordinal);

        public bool HasParameters => Parameters != null && Parameters.Items.Any();

        public bool HasArguments => Parameters != null && Parameters.Items.Any(parameter => parameter.IsArgument);

        public static BenchmarkCase Create(Descriptor descriptor, Job job, ParameterInstances parameters, ImmutableConfig config)
            => new BenchmarkCase(descriptor, job.MakeSettingsUserFriendly(descriptor), parameters, config);
    }
}