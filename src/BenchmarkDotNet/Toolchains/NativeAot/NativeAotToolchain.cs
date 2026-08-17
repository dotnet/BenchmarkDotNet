using BenchmarkDotNet.Characteristics;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Toolchains.DotNetCli;
using BenchmarkDotNet.Validators;

namespace BenchmarkDotNet.Toolchains.NativeAot
{
    public class NativeAotToolchain : Toolchain
    {
        /// <summary>
        /// compiled as net7.0.
        /// </summary>
        public static readonly IToolchain Net70 = CreateBuilder()
            .TargetFrameworkMoniker("net7.0")
            .ToToolchain();

        /// <summary>
        /// compiled as net8.0.
        /// </summary>
        public static readonly IToolchain Net80 = CreateBuilder()
            .TargetFrameworkMoniker("net8.0")
            .ToToolchain();

        /// <summary>
        /// compiled as net9.0.
        /// </summary>
        public static readonly IToolchain Net90 = CreateBuilder()
            .TargetFrameworkMoniker("net9.0")
            .ToToolchain();

        /// <summary>
        /// compiled as net10.0.
        /// </summary>
        public static readonly IToolchain Net10_0 = CreateBuilder()
            .TargetFrameworkMoniker("net10.0")
            .ToToolchain();

        /// <summary>
        /// compiled as net11.0.
        /// </summary>
        public static readonly IToolchain Net11_0 = CreateBuilder()
            .TargetFrameworkMoniker("net11.0")
            .ToToolchain();

        internal NativeAotToolchain(string displayName,
            string ilCompilerVersion,
            string runtimeFrameworkVersion, string targetFrameworkMoniker, string runtimeIdentifier,
            string customDotNetCliPath, string packagesRestorePath,
            Dictionary<string, string> feeds, bool useNuGetClearTag, bool useTempFolderForRestore,
            bool rootAllApplicationAssemblies, bool ilcGenerateStackTraceData,
            string ilcOptimizationPreference, string ilcInstructionSet)
            : base(displayName,
                new Generator(ilCompilerVersion, runtimeFrameworkVersion, targetFrameworkMoniker, customDotNetCliPath,
                    runtimeIdentifier, feeds, useNuGetClearTag, useTempFolderForRestore, packagesRestorePath,
                    rootAllApplicationAssemblies, ilcGenerateStackTraceData,
                    ilcOptimizationPreference, ilcInstructionSet),
                new DotNetCliPublisher(targetFrameworkMoniker, customDotNetCliPath, GetExtraArguments(runtimeIdentifier)),
                new Executor())
        {
            CustomDotNetCliPath = customDotNetCliPath;
        }

        internal string CustomDotNetCliPath { get; }

        public static NativeAotToolchainBuilder CreateBuilder() => NativeAotToolchainBuilder.Create();

        public static string GetExtraArguments(string runtimeIdentifier) => $"-r {runtimeIdentifier}";

        public override async IAsyncEnumerable<ValidationError> ValidateAsync(BenchmarkCase benchmarkCase, IResolver resolver)
        {
            await foreach (var error in base.ValidateAsync(benchmarkCase, resolver).ConfigureAwait(false))
            {
                yield return error;
            }

            foreach (var validationError in DotNetSdkValidator.ValidateCoreSdks(CustomDotNetCliPath, benchmarkCase))
            {
                yield return validationError;
            }
        }
    }
}
