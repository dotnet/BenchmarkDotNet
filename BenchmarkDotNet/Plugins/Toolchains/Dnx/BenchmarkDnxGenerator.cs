using System;
using System.IO;
using BenchmarkDotNet.Helpers;
using BenchmarkDotNet.Plugins.Loggers;
using BenchmarkDotNet.Plugins.Toolchains.Classic;
using BenchmarkDotNet.Tasks;

namespace BenchmarkDotNet.Plugins.Toolchains.Dnx
{
    internal class BenchmarkDnxGenerator : BenchmarkClassicGenerator
    {
        internal const string ProjectFileName = "project.json";

        public BenchmarkDnxGenerator(IBenchmarkLogger logger) : base(logger)
        {
        }

        protected override void GenerateProjectFile(string projectDir, Benchmark benchmark)
        {
            var template = ResourceHelper.LoadTemplate("BenchmarkProject.json");

            var content = SetPlatform(template, benchmark.Task.Configuration.Platform);
    
            string fileName = Path.Combine(projectDir, ProjectFileName);

            File.WriteAllText(fileName, content);
        }

        protected override void GenerateProjectBuildFile(string projectDir)
        {
            // do nothing on purpose, we do not need bat file
        }

        private static string SetPlatform(string template, BenchmarkPlatform platform)
        {
            switch (platform)
            {
                case BenchmarkPlatform.HostPlatform:
                    return template.Replace("\"platform\": \"$PLATFORM\",", string.Empty); // todo: verify that this is the way to go
                case BenchmarkPlatform.AnyCpu:
                    return template.Replace("$PLATFORM", "Any CPU"); // todo: verify name
                case BenchmarkPlatform.X86:
                    return template.Replace("$PLATFORM", "x86");
                case BenchmarkPlatform.X64:
                    return template.Replace("$PLATFORM", "x64");
                default:
                    throw new ArgumentOutOfRangeException(nameof(platform), platform, null);
            }
        }
    }
}