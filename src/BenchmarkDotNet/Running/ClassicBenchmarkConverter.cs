﻿using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Environments;
using BenchmarkDotNet.Loggers;
using Microsoft.CSharp;

namespace BenchmarkDotNet.Running
{
    public static partial class BenchmarkConverter
    {
        public static BenchmarkRunInfo[] UrlToBenchmarks(string url, IConfig config = null)
        {
            var logger = HostEnvironmentInfo.FallbackLogger;

            url = GetRawUrl(url);
            string benchmarkContent;
            try
            {
                var webRequest = WebRequest.Create(url);
                using (var response = webRequest.GetResponse())
                using (var content = response.GetResponseStream())
                {
                    if (content == null)
                    {
                        logger.WriteLineError("ResponseStream == null");
                        return Array.Empty<BenchmarkRunInfo>();
                    }
                    using (var reader = new StreamReader(content))
                        benchmarkContent = reader.ReadToEnd();
                    if (string.IsNullOrWhiteSpace(benchmarkContent))
                    {
                        logger.WriteLineHint($"content of '{url}' is empty.");
                        return Array.Empty<BenchmarkRunInfo>();
                    }
                }
            }
            catch (Exception e)
            {
                logger.WriteLineError("BuildException: " + e.Message);
                return Array.Empty<BenchmarkRunInfo>();
            }
            return SourceToBenchmarks(benchmarkContent, config);
        }

        public static BenchmarkRunInfo[] SourceToBenchmarks(string source, IConfig config = null)
        {
            string benchmarkContent = source;
            var cSharpCodeProvider = new CSharpCodeProvider();
            string directoryName = Path.GetDirectoryName(typeof(BenchmarkCase).Assembly.Location)
                ?? throw new DirectoryNotFoundException(typeof(BenchmarkCase).Assembly.Location);
            var compilerParameters = new CompilerParameters(
                new[]
                {
                    "mscorlib.dll",
                    "System.dll",
                    "System.Core.dll"
                })
            {
                CompilerOptions = "/unsafe /optimize",
                GenerateInMemory = false,
                OutputAssembly = Path.Combine(
                    directoryName,
                    $"{Path.GetFileNameWithoutExtension(Path.GetTempFileName())}.dll")
            };

            compilerParameters.ReferencedAssemblies.Add(typeof(BenchmarkCase).Assembly.Location);
            var compilerResults = cSharpCodeProvider.CompileAssemblyFromSource(compilerParameters, benchmarkContent);
            if (compilerResults.Errors.HasErrors)
            {
                var logger = HostEnvironmentInfo.FallbackLogger;

                compilerResults.Errors.Cast<CompilerError>().ToList().ForEach(error => logger.WriteLineError(error.ErrorText));
                return Array.Empty<BenchmarkRunInfo>();
            }

            var types = compilerResults.CompiledAssembly.GetTypes();

            var resultBenchmarks = new List<BenchmarkRunInfo>();
            foreach (var type in types)
            {
                var runInfo = TypeToBenchmarks(type, config);
                var benchmarks = runInfo.BenchmarksCases.Select(b =>
                {
                    var target = b.Descriptor;
                    return BenchmarkCase.Create(
                        new Descriptor(target.Type, target.WorkloadMethod, target.GlobalSetupMethod, target.GlobalCleanupMethod,
                            target.IterationSetupMethod, target.IterationCleanupMethod,
                            target.WorkloadMethodDisplayInfo, benchmarkContent, target.Baseline, target.Categories, target.OperationsPerInvoke),
                        b.Job,
                        b.Parameters,
                        b.Config);
                });
                resultBenchmarks.Add(
                    new BenchmarkRunInfo(benchmarks.ToArray(), runInfo.Type, runInfo.Config));
            }

            return resultBenchmarks.ToArray();
        }

        private static string GetRawUrl(string url)
        {
            if (url.StartsWith("https://gist.github.com/") && !(url.EndsWith("/raw") || url.EndsWith("/raw/")))
                return url.TrimEnd('/') + "/raw";
            if (url.StartsWith("https://github.com/") && url.Contains("/blob/"))
                return url.Replace("https://github.com/", "https://raw.githubusercontent.com/").Replace("/blob/", "/");
            return url;
        }
    }
}
