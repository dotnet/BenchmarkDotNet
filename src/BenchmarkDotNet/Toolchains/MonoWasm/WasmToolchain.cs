using System.Collections.Generic;
using BenchmarkDotNet.Characteristics;
using BenchmarkDotNet.Portability;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Toolchains.DotNetCli;
using BenchmarkDotNet.Validators;
using JetBrains.Annotations;

namespace BenchmarkDotNet.Toolchains.MonoWasm
{
    [PublicAPI]
    public class WasmToolchain : Toolchain
    {
        private readonly string CustomDotNetCliPath;

        private WasmToolchain(string name, IGenerator generator, IBuilder builder, IExecutor executor, ISdkProvider sdkProvider)
            : base(name, generator, builder, executor, sdkProvider)
        {
            CustomDotNetCliPath = sdkProvider.CustomDotNetCliPath;
        }

        public override IEnumerable<ValidationError> Validate(BenchmarkCase benchmarkCase, IResolver resolver)
        {
            foreach (var validationError in base.Validate(benchmarkCase, resolver))
            {
                yield return validationError;
            }

            if (RuntimeInformation.IsWindows())
            {
                yield return new ValidationError(true,
                    $"{nameof(WasmToolchain)} is supported only on Unix, benchmark '{benchmarkCase.DisplayInfo}' might not work correctly",
                    benchmarkCase);
            }
            else if (IsCliPathInvalid(CustomDotNetCliPath, benchmarkCase, out var invalidCliError))
            {
                yield return invalidCliError;
            }
        }

        [PublicAPI]
        public static IToolchain From(NetCoreAppSettings netCoreAppSettings)
            => new WasmToolchain(netCoreAppSettings.Name,
                    new WasmGenerator(netCoreAppSettings.TargetFrameworkMoniker,
                        netCoreAppSettings.CustomDotNetCliPath,
                        netCoreAppSettings.PackagesPath,
                        netCoreAppSettings.CustomRuntimePack,
                        netCoreAppSettings.AOTCompilerMode == MonoAotLLVM.MonoAotCompilerMode.wasm),
                    new DotNetCliBuilder(netCoreAppSettings.TargetFrameworkMoniker,
                        netCoreAppSettings.CustomDotNetCliPath,
                        // aot builds can be very slow
                        logOutput: netCoreAppSettings.AOTCompilerMode == MonoAotLLVM.MonoAotCompilerMode.wasm),
                    new WasmExecutor(),
                    new DotNetSdkProvider());
    }
}