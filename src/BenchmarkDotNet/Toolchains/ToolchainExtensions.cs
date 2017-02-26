using System;
using System.IO;
using System.Linq;
using BenchmarkDotNet.Environments;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Toolchains.CsProj;
using BenchmarkDotNet.Toolchains.DotNetCli;
using BenchmarkDotNet.Toolchains.ProjectJson;

namespace BenchmarkDotNet.Toolchains
{
    internal static class ToolchainExtensions
    {
        private static readonly Lazy<bool> isUsingProjectJson = new Lazy<bool>(IsUsingProjectJson);

        internal static IToolchain GetToolchain(this Job job)
        {
            return job.HasValue(InfrastructureMode.ToolchainCharacteristic)
                ? job.Infrastructure.Toolchain
                : GetToolchain(job.ResolveValue(EnvMode.RuntimeCharacteristic, EnvResolver.Instance));
        }

        internal static IToolchain GetToolchain(this Runtime runtime)
        {
            switch (runtime)
            {
                case ClrRuntime clr:
                case MonoRuntime mono:
#if CLASSIC
                    return new Roslyn.RoslynToolchain();
#else
                    return isUsingProjectJson.Value ? ProjectJsonNet46Toolchain.Instance : CsProjNet46Toolchain.Instance;
#endif
                case CoreRuntime core:
                    return isUsingProjectJson.Value ? ProjectJsonCoreToolchain.Current.Value : CsProjCoreToolchain.Current.Value;
                default:
                    throw new ArgumentOutOfRangeException(nameof(runtime), runtime, "Runtime not supported");
            }
        }

        private static bool IsUsingProjectJson() => 
            HostEnvironmentInfo.GetCurrent().DotNetCliVersion.Value.Contains("preview") 
            && SolutionDirectoryContainsProjectJsonFiles();

        private static bool SolutionDirectoryContainsProjectJsonFiles()
        {
            if (!DotNetCliGenerator.GetSolutionRootDirectory(out var solutionRootDirectory))
            {
                solutionRootDirectory = new DirectoryInfo(Directory.GetCurrentDirectory());
            }

            return solutionRootDirectory.EnumerateFiles("project.json", SearchOption.AllDirectories).Any();
        }
    }
}