using System;
using System.Collections.Generic;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Toolchains.Results;
using BenchmarkDotNet.Toolchains.DotNetCli;
using System.IO;
using BenchmarkDotNet.Environments;

namespace BenchmarkDotNet.Toolchains.MonoWasm
{
    public class WasmBuilder : IBuilder
    {
        private readonly DotNetCliBuilder dotNetCliBuilder;
        private readonly string targetFrameworkMoniker;

        public WasmBuilder(string targetFrameworkMoniker, string customDotNetCliPath = null, TimeSpan? timeout = null)
        {
            this.targetFrameworkMoniker = targetFrameworkMoniker;

            dotNetCliBuilder = new DotNetCliBuilder(targetFrameworkMoniker, customDotNetCliPath, timeout);
        }

        public BuildResult Build(GenerateResult generateResult, BuildPartition buildPartition, ILogger logger)
        {
            BuildResult buildResult = dotNetCliBuilder.Build(generateResult, buildPartition, logger);

            WasmRuntime runtime = (WasmRuntime)buildPartition.Runtime;

            if (buildResult.IsBuildSuccess && !runtime.Aot)
            {
                BuildApp(buildPartition.ProgramName, generateResult.ArtifactsPaths.BuildArtifactsDirectoryPath, runtime);
            }

            return buildResult;
        }

        private void BuildApp(string programName, string projectRoot, WasmRuntime runtime)
        {
            string appDir = Path.Combine(projectRoot, $"bin", targetFrameworkMoniker, "browser-wasm", "publish");
            string outputDir = Path.Combine(appDir, "output");

            string mainAssemblyPath = Path.Combine(appDir, $"{programName}.dll");

            if (!File.Exists(mainAssemblyPath))
                throw new ArgumentException($"File MainAssembly='{mainAssemblyPath}' doesn't exist.");

            var assemblies = Directory.GetFiles(appDir, "*.dll");

            // Create app
            Directory.CreateDirectory(outputDir);
            Directory.CreateDirectory(Path.Combine(outputDir, "managed"));
            foreach (var assembly in assemblies)
                File.Copy(assembly, Path.Combine(outputDir, "managed", Path.GetFileName(assembly)), true);

            string timeZoneDat = "dotnet.timezones.blat";
            string icutDat = "icudt.dat";

            foreach (var f in new string[] { "dotnet.wasm", "dotnet.js", timeZoneDat, icutDat })
                File.Copy(Path.Combine(appDir, f), Path.Combine(outputDir, f), true);

            File.Copy(runtime.MainJs.FullName, Path.Combine(outputDir, "runtime.js"), true);

            using (var sw = File.CreateText(Path.Combine(outputDir, "mono-config.js")))
            {
                sw.WriteLine("config = {");
                sw.WriteLine("\t\"assembly_root\": \"managed\",");
                sw.WriteLine("\t\"enable_debugging\": 0,");
                sw.WriteLine("\t\"assets\": [");

                foreach (var assembly in assemblies)
                {
                    sw.Write($"\t\t{{ \"behavior\": \"assembly\", \"name\":  \"{ Path.GetFileName(assembly)}\" }}");
                    sw.WriteLine(",");
                }

                sw.WriteLine($"\t\t{{ \"behavior\": \"icu\", \"name\": \"icudt.dat\", \"load_remote\": false}},");
                sw.WriteLine($"\t\t{{ \"behavior\": \"vfs\", \"name\": \"{timeZoneDat}\", \"virtual_path\": \"/usr/share/zoneinfo/\" }}");

                sw.WriteLine("\t],");
                sw.WriteLine("\t\"files_to_map\": []");

                sw.WriteLine("};");
            }
        }
    }
}
