#if CLASSIC
using System;
using System.CodeDom.Compiler;
using System.IO;
using System.Linq;
using System.Net;
using BenchmarkDotNet.Configs;
using Microsoft.CSharp;

namespace BenchmarkDotNet.Running
{
    public static partial class BenchmarkConverter
    {
        public static Benchmark[] UrlToBenchmarks(string url, IConfig config = null)
        {
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
                    Console.WriteLine($"content of '{url}' is empty.");
                    return new Benchmark[0];
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("BuildException: " + e.Message);
                return new Benchmark[0];
            }
            return SourceToBenchmarks(benchmarkContent, config);
        }

        public static Benchmark[] SourceToBenchmarks(string source, IConfig config = null)
        {
            string benchmarkContent = source;
            var cSharpCodeProvider = new CSharpCodeProvider();
            var compilerParameters = new CompilerParameters(new[] { "mscorlib.dll", "System.dll", "System.Core.dll" }) { CompilerOptions = "/unsafe" };
            compilerParameters.ReferencedAssemblies.Add(typeof(BenchmarkRunner).Assembly.Location);
            var compilerResults = cSharpCodeProvider.CompileAssemblyFromSource(compilerParameters, benchmarkContent);
            if (compilerResults.Errors.HasErrors)
            {
                compilerResults.Errors.Cast<CompilerError>().ToList().ForEach(error => Console.WriteLine(error.ErrorText));
                return new Benchmark[0];
            }
            return (
                from type in compilerResults.CompiledAssembly.GetTypes()
                from benchmark in TypeToBenchmarks(type, config)
                let target = benchmark.Target
                select new Benchmark(
                    new Target(target.Type, target.Method, target.SetupMethod, target.MethodTitle, benchmarkContent, target.Baseline, target.OperationsPerInvoke),
                    benchmark.Job,
                    benchmark.Parameters)).ToArray();
        }
    }
}
#endif