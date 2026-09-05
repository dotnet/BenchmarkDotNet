using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Detectors;
using BenchmarkDotNet.Environments;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Parameters;
using BenchmarkDotNet.Portability;
using BenchmarkDotNet.Toolchains;
using BenchmarkDotNet.Toolchains.InProcess.Emit;
using BenchmarkDotNet.Toolchains.InProcess.NoEmit;

namespace BenchmarkDotNet.Running
{
    public class BenchmarkCase : IComparable<BenchmarkCase>, IDisposable, IAsyncDisposable
    {
        public Descriptor Descriptor { get; }
        public Job Job { get; }
        public ParameterInstances Parameters { get; }
        public ImmutableConfig Config { get; }

        public string FolderInfo => (Descriptor.FolderInfo + "_" + Job.FolderInfo + "_" + Parameters.FolderInfo).Trim('_');
        public string DisplayInfo => (Descriptor.DisplayInfo + ": " + Job.DisplayInfo + " " + Parameters.DisplayInfo).Trim(' ');

        public override string ToString() => DisplayInfo;

        internal BenchmarkCase(Descriptor descriptor, Job job, ParameterInstances parameters, ImmutableConfig config)
        {
            Descriptor = descriptor;
            Job = job;
            Parameters = parameters ?? ParameterInstances.Empty;
            Config = config;
        }

        public Runtime GetRuntime() => Job.Infrastructure.HasValue(InfrastructureMode.RuntimeCharacteristic)
                ? Job.Infrastructure.Runtime!
                : RuntimeInformation.GetTargetOrCurrentRuntime(Descriptor.Type.Assembly);

        internal IToolchain GetToolchain()
        {
            if (Job.Infrastructure.TryGetToolchain(out var toolchain))
                return toolchain;

            // On mobile OSes BDN can't spawn a child process to build/run out of process, so benchmarks must run
            // in-process regardless of the runtime (Mono, CoreCLR, NativeAOT): emit IL when dynamic code is
            // available, fall back to reflection-only when it isn't (e.g. iOS AOT).
            if (OsDetector.IsMobile())
                return RuntimeInformation.IsAot ? InProcessNoEmitToolchain.Default : InProcessEmitToolchain.Default;

            return GetRuntime().GetDefaultToolchain(this);
        }

        public ValueTask DisposeAsync() => Parameters.DisposeAsync();

        public void Dispose() => Parameters.Dispose();

        public int CompareTo(BenchmarkCase? other)
        {
            if (ReferenceEquals(this, other)) return 0;
            if (other is null) return 1;

            return string.Compare(FolderInfo, other.FolderInfo, StringComparison.Ordinal);
        }

        public bool HasParameters => Parameters != null && Parameters.Items.Any();

        public bool HasArguments => Parameters != null && Parameters.Items.Any(parameter => parameter.IsArgument);

        public static BenchmarkCase Create(Descriptor descriptor, Job job, ParameterInstances parameters, ImmutableConfig config)
            => new BenchmarkCase(descriptor, job.MakeSettingsUserFriendly(descriptor), parameters, config);
    }
}
