using System;
using System.IO;
using BenchmarkDotNet.Characteristics;
using BenchmarkDotNet.Environments;
using BenchmarkDotNet.Extensions;
using BenchmarkDotNet.Helpers;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Toolchains.DotNetCli;
using System.Reflection;
using BenchmarkDotNet.Loggers;
using JetBrains.Annotations;

namespace BenchmarkDotNet.Toolchains.ProjectJson
{
    [PublicAPI]
    public class ProjectJsonGenerator : DotNetCliGenerator
    {
        public ProjectJsonGenerator(string targetFrameworkMoniker, string extraDependencies, Func<Platform, string> platformProvider, string imports, string runtime = null) 
            : base(targetFrameworkMoniker, extraDependencies, platformProvider, imports, runtime)
        {
        }

        protected override string GetProjectFilePath(string binariesDirectoryPath)
            => Path.Combine(binariesDirectoryPath, "project.json");

        protected override void GenerateProject(Benchmark benchmark, ArtifactsPaths artifactsPaths, IResolver resolver, ILogger logger)
        {
            string template = ResourceHelper.LoadTemplate("BenchmarkProject.json");

            string content = SetPlatform(template, PlatformProvider(benchmark.Job.ResolveValue(EnvMode.PlatformCharacteristic, resolver)));
            content = SetCodeFileName(content, Path.GetFileName(artifactsPaths.ProgramCodePath));
            content = SetDependencyToExecutingAssembly(content, benchmark.Target.Type);
            content = SetTargetFrameworkMoniker(content, TargetFrameworkMoniker);
            content = SetExtraDependencies(content, ExtraDependencies);
            content = SetImports(content, Imports);
            content = SetRuntime(content, Runtime);
            content = SetGcMode(content, benchmark.Job.Env.Gc, resolver);

            File.WriteAllText(artifactsPaths.ProjectFilePath, content);
        }

        private static string SetDependencyToExecutingAssembly(string template, Type benchmarkTarget)
        {
            var assemblyName = benchmarkTarget.GetTypeInfo().Assembly.GetName();
            string packageVersion = GetPackageVersion(assemblyName);

            return template.
                Replace("$EXECUTINGASSEMBLYVERSION$", packageVersion).
                Replace("$EXECUTINGASSEMBLY$", assemblyName.Name);
        }

        private static string SetExtraDependencies(string content, string extraDependencies) => content.Replace("$REQUIREDDEPENDENCY$", extraDependencies);

        private static string SetImports(string content, string imports) => content.Replace("$IMPORTS$", imports);

        private static string SetRuntime(string content, string runtime) => content.Replace("$RUNTIME$", runtime);

        private static string SetGcMode(string content, GcMode gcMode, IResolver resolver)
        {
            if (!gcMode.HasChanges)
                return content.Replace("$GC$", null);

            return content.Replace(
                "$GC$",
                $"\"runtimeOptions\": {{ \"configProperties\": {{ " +
                $"\"System.GC.Concurrent\": {gcMode.ResolveValue(GcMode.ConcurrentCharacteristic, resolver).ToLowerCase()}, " +
                $"\"System.GC.RetainVM\": {gcMode.ResolveValue(GcMode.RetainVmCharacteristic, resolver).ToLowerCase()}, " +
                $"\"System.GC.Server\": {gcMode.ResolveValue(GcMode.ServerCharacteristic, resolver).ToLowerCase()} }} }}, ");
        }

        private static string GetPackageVersion(AssemblyName assemblyName)
        {
            // we can not simply call assemblyName.Version.ToString() because it is different than package version which can contain (and often does) text
            // we are using the wildcard to get latest version of package/project restored
            return $"{assemblyName.Version.Major}.{assemblyName.Version.Minor}.{assemblyName.Version.Build}-*";
        }
    }
}