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

        internal string? MainJsTemplatePath { get; }


        private WasmToolchain(string name, IGenerator generator, IBuilder builder, IExecutor executor, string customDotNetCliPath, string? mainJsTemplatePath)
            : base(name, generator, builder, executor)
        {
            CustomDotNetCliPath = customDotNetCliPath;
            MainJsTemplatePath = mainJsTemplatePath;
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
        public static IToolchain From(NetCoreAppSettings netCoreAppSettings, string? mainJsTemplatePath = null)
        {
            var generator = new WasmGenerator(netCoreAppSettings.TargetFrameworkMoniker,
                                netCoreAppSettings.CustomDotNetCliPath,
                                netCoreAppSettings.PackagesPath,
                                netCoreAppSettings.CustomRuntimePack,
                                netCoreAppSettings.AOTCompilerMode == MonoAotLLVM.MonoAotCompilerMode.wasm,
                                mainJsTemplatePath);

            var cliBuilder = new DotNetCliBuilder(netCoreAppSettings.TargetFrameworkMoniker,
                                 netCoreAppSettings.CustomDotNetCliPath,
                                 logOutput: netCoreAppSettings.AOTCompilerMode == MonoAotLLVM.MonoAotCompilerMode.wasm);

            var executor = new WasmExecutor();

            return new WasmToolchain(netCoreAppSettings.Name, generator, cliBuilder, executor, netCoreAppSettings.CustomDotNetCliPath, mainJsTemplatePath);
        }
    }
}