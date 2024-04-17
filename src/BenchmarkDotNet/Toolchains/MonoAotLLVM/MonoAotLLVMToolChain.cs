using BenchmarkDotNet.Characteristics;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Toolchains.DotNetCli;
using BenchmarkDotNet.Validators;
using System.Collections.Generic;

namespace BenchmarkDotNet.Toolchains.MonoAotLLVM
{
    public class MonoAotLLVMToolChain : Toolchain
    {
        private readonly string _customDotNetCliPath;

        public MonoAotLLVMToolChain(string name, IGenerator generator, IBuilder builder, IExecutor executor, string customDotNetCliPath)
            : base(name, generator, builder, executor)
        {
            _customDotNetCliPath = customDotNetCliPath;
        }

        public static IToolchain From(NetCoreAppSettings netCoreAppSettings)
        => new MonoAotLLVMToolChain(netCoreAppSettings.Name,
                new MonoAotLLVMGenerator(netCoreAppSettings.TargetFrameworkMoniker,
                    netCoreAppSettings.CustomDotNetCliPath,
                    netCoreAppSettings.PackagesPath,
                    netCoreAppSettings.CustomRuntimePack,
                    netCoreAppSettings.AOTCompilerPath,
                    netCoreAppSettings.AOTCompilerMode),
                // We have no tests for this toolchain, so not including ArtifactsPath in case it fails the same as wasm.
                new DotNetCliBuilder(netCoreAppSettings.CustomDotNetCliPath, logOutput: false, useArtifactsPathIfSupported: false),
                new Executor(),
                netCoreAppSettings.CustomDotNetCliPath);

        public override IEnumerable<ValidationError> Validate(BenchmarkCase benchmarkCase, IResolver resolver)
        {
            foreach (var validationError in base.Validate(benchmarkCase, resolver))
            {
                yield return validationError;
            }

            foreach (var validationError in DotNetSdkValidator.ValidateCoreSdks(_customDotNetCliPath, benchmarkCase))
            {
                yield return validationError;
            }
        }
    }
}
