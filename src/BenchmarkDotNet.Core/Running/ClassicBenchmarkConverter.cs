#if CLASSIC
using System;
using System.CodeDom.Compiler;
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
        public static Benchmark[] UrlToBenchmarks(string url, IConfig config = null)
        {
            var logger = config?.GetCompositeLogger() ?? HostEnvironmentInfo.FallbackLogger;

            url = GetRawUrl(url);
            string benchmarkContent;
            try
            {
                var webRequest = WebRequest.Create(url);
                using (var response = webRequest.GetResponse())
                using (var content = response.GetResponseStream())
                using (var reader = new StreamReader(content))
                    benchmarkContent = reader.ReadToEnd();
                if (string.IsNullOrWhiteSpace(benchmarkContent))
                {
                    logger.WriteLineHint($"content of '{url}' is empty.");
                    return Array.Empty<Benchmark>();
                }
            }
            catch (Exception e)
            {
                logger.WriteLineError("BuildException: " + e.Message);
                return Array.Empty<Benchmark>();
            }
            return SourceToBenchmarks(benchmarkContent, config);
        }

        public static Benchmark[] SourceToBenchmarks(string source, IConfig config = null)
        {
            string benchmarkContent = source;
            var cSharpCodeProvider = new CSharpCodeProvider();
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
                    Path.GetDirectoryName(typeof(Benchmark).Assembly.Location), 
                    $"{Path.GetFileNameWithoutExtension(Path.GetTempFileName())}.dll")
            };
            
            compilerParameters.ReferencedAssemblies.Add(typeof(Benchmark).Assembly.Location);
            var compilerResults = cSharpCodeProvider.CompileAssemblyFromSource(compilerParameters, benchmarkContent);
            if (compilerResults.Errors.HasErrors)
            {
                var logger = config?.GetCompositeLogger() ?? HostEnvironmentInfo.FallbackLogger;
    
                compilerResults.Errors.Cast<CompilerError>().ToList().ForEach(error => logger.WriteLineError(error.ErrorText));
                return Array.Empty<Benchmark>();
            }
            return (
                from type in compilerResults.CompiledAssembly.GetTypes()
                from benchmark in TypeToBenchmarks(type, config)
                let target = benchmark.Target
                select new Benchmark(
                    new Target(target.Type, target.Method, target.GlobalSetupMethod, target.GlobalCleanupMethod, 
                        target.IterationSetupMethod, target.IterationCleanupMethod, 
                        target.MethodDisplayInfo, benchmarkContent, target.Baseline, target.Categories, target.OperationsPerInvoke),
                    benchmark.Job,
                    benchmark.Parameters)).ToArray();
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
#endif