using BenchmarkDotNet.Toolchains;
using BenchmarkDotNet.Toolchains.DotNetCli;
using BenchmarkDotNet.Characteristics;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Portability;

namespace BenchmarkDotNet.Toolchains.MonoAotLLVM
{
    public class MonoAotLLVMToolChain : Toolchain
    {
        public MonoAotLLVMToolChain(string name, IGenerator generator, IBuilder builder, IExecutor executor)
            : base(name, generator, builder, executor)
        {
        }

        public static IToolchain From(NetCoreAppSettings netCoreAppSettings)
            => new MonoAotLLVMToolChain(netCoreAppSettings.Name,
                    new MonoAotLLVMGenerator(netCoreAppSettings.TargetFrameworkMoniker,
                        netCoreAppSettings.CustomDotNetCliPath,
                        netCoreAppSettings.PackagesPath,
                        netCoreAppSettings.CustomRuntimePack,
                        netCoreAppSettings.AOTCompilerPath,
                        netCoreAppSettings.AOTCompilerMode),
                    new MonoAotLLVMBuilder(netCoreAppSettings.TargetFrameworkMoniker,
                        netCoreAppSettings.CustomDotNetCliPath,
                        netCoreAppSettings.Timeout),
                    new Executor());
    }
}
