using System.Collections.Generic;
using BenchmarkDotNet.Characteristics;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Toolchains.DotNetCli;
using BenchmarkDotNet.Validators;
using JetBrains.Annotations;

namespace BenchmarkDotNet.Toolchains.MonoWasm
{
    [PublicAPI]
    public class WasmToolchain : Toolchain
    {
        private string CustomDotNetCliPath { get; }

        private WasmToolchain(string name, IGenerator generator, IBuilder builder, IExecutor executor, string customDotNetCliPath)
            : base(name, generator, builder, executor)
        {
            CustomDotNetCliPath = customDotNetCliPath;
        }

        public override IEnumerable<ValidationError> Validate(BenchmarkCase benchmarkCase, IResolver resolver)
        {
            foreach (var validationError in base.Validate(benchmarkCase, resolver))
            {
                yield return validationError;
            }

            foreach (var validationError in DotNetSdkValidator.ValidateCoreSdks(CustomDotNetCliPath, benchmarkCase))
            {
                yield return validationError;
            }
        }

        [PublicAPI]
        public static IToolchain From(NetCoreAppSettings netCoreAppSettings)
        {
            var generator = new WasmGenerator(netCoreAppSettings.TargetFrameworkMoniker,
                                netCoreAppSettings.CustomDotNetCliPath,
                                netCoreAppSettings.PackagesPath,
                                netCoreAppSettings.CustomRuntimePack,
                                netCoreAppSettings.AOTCompilerMode == MonoAotLLVM.MonoAotCompilerMode.wasm);

            var cliBuilder = new DotNetCliBuilder(netCoreAppSettings.TargetFrameworkMoniker,
                                 netCoreAppSettings.CustomDotNetCliPath,
                                 logOutput: netCoreAppSettings.AOTCompilerMode == MonoAotLLVM.MonoAotCompilerMode.wasm);

            var executor = new WasmExecutor();

            return new WasmToolchain(netCoreAppSettings.Name, generator, cliBuilder, executor, netCoreAppSettings.CustomDotNetCliPath);
        }
    }
}