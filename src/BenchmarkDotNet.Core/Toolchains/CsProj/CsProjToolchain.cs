using BenchmarkDotNet.Characteristics;
using BenchmarkDotNet.Environments;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Toolchains.Core;
using BenchmarkDotNet.Toolchains.DotNetCli;

namespace BenchmarkDotNet.Toolchains.CsProj
{
    /// <summary>
    /// very simple, experimental version
    /// limitations: 
    ///     can not set process priority and/or affinity
    ///     can not break with ctrl+c
    ///     does not support custom GcMode settings (will change in the future)
    ///     does not support x86 settings (will change in the future)
    /// </summary>
    public class CsProjToolchain : Toolchain
    {
        public static readonly IToolchain NetCoreApp11 = From(NetCoreAppSettings.NetCoreApp11);
        public static readonly IToolchain NetCoreApp12 = From(NetCoreAppSettings.NetCoreApp12);
        public static readonly IToolchain NetCoreApp20 = From(NetCoreAppSettings.NetCoreApp20);

        private CsProjToolchain(string name, IGenerator generator, IBuilder builder, IExecutor executor) 
            : base(name, generator, builder, executor)
        {
        }

        private static IToolchain From(NetCoreAppSettings settings)
            => new CsProjToolchain("CsProjCore",
                new CsProjGenerator(settings.TargetFrameworkMoniker, PlatformProvider), 
                new DotNetCliBuilder(settings.TargetFrameworkMoniker), 
                new DotNetRunExecutor());

        // dotnet cli supports only x64 compilation now
        private static string PlatformProvider(Platform platform) => "x64";
    }
}