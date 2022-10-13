using BenchmarkDotNet.Toolchains.DotNetCli;
using JetBrains.Annotations;
using BenchmarkDotNet.Characteristics;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Portability;

namespace BenchmarkDotNet.Toolchains.MonoWasm
{
    [PublicAPI]
    public class WasmToolChain : Toolchain
    {
        private string CustomDotNetCliPath { get; }

        private WasmToolChain(string name, IGenerator generator, IBuilder builder, IExecutor executor, string customDotNetCliPath)
            : base(name, generator, builder, executor)
        {
            CustomDotNetCliPath = customDotNetCliPath;
        }

        public override bool IsSupported(BenchmarkCase benchmarkCase, ILogger logger, IResolver resolver)
        {
            if (!base.IsSupported(benchmarkCase, logger, resolver))
                return false;

            if (InvalidCliPath(CustomDotNetCliPath, benchmarkCase, logger))
                return false;

            if (RuntimeInformation.IsWindows())
                logger.WriteLineInfo($"{nameof(WasmToolChain)} is supported only on Unix, benchmark '{benchmarkCase.DisplayInfo}' might not work correctly");

            return true;
        }

        [PublicAPI]
        public static IToolchain From(NetCoreAppSettings netCoreAppSettings)
            => new WasmToolChain(netCoreAppSettings.Name,
                    new WasmGenerator(netCoreAppSettings.TargetFrameworkMoniker,
                        netCoreAppSettings.CustomDotNetCliPath,
                        netCoreAppSettings.PackagesPath,
                        netCoreAppSettings.CustomRuntimePack,
                        netCoreAppSettings.AOTCompilerMode == MonoAotLLVM.MonoAotCompilerMode.wasm),
                    new DotNetCliBuilder(netCoreAppSettings.TargetFrameworkMoniker,
                        netCoreAppSettings.CustomDotNetCliPath,
                        // aot builds can be very slow
                        logOutput: netCoreAppSettings.AOTCompilerMode == MonoAotLLVM.MonoAotCompilerMode.wasm,
                        retryFailedBuildWithNoDeps: false),
                    new WasmExecutor(),
                    netCoreAppSettings.CustomDotNetCliPath);
    }
}
