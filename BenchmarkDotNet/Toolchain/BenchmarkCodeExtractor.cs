using BenchmarkDotNet.Logging;
using System.Diagnostics;
using System.Linq;

namespace BenchmarkDotNet
{
    internal class BenchmarkCodeExtractor
    {
        private Process process { get; set; }
        private string codeExeName { get; set; }

        private string fullTypeName { get; set; }
        private string fullMethodName { get; set; }

        private IBenchmarkLogger logger { get; set; }

        public BenchmarkCodeExtractor(Benchmark benchmark, Process process, string codeExeName, IBenchmarkLogger logger)
        {
            this.process = process;
            this.codeExeName = codeExeName;
            this.logger = logger;

            //Method name format: "BenchmarkDotNet.Samples.Infra.RunFast()" (NOTE: WITHOUT the return type)
            var methodInfo = benchmark.Target.Method;
            this.fullTypeName = methodInfo.DeclaringType.FullName;

            var methodParams = string.Join(", ", methodInfo.GetParameters().Select(p => p.ParameterType.FullName));
            this.fullMethodName = $"{fullTypeName}.{methodInfo.Name}({methodParams})";
        }

        internal void PrintCodeForMethod(bool printAssembly = true, bool printIL = true)
        {
            logger?.WriteLine("\n@@@ PrintAssembly={0}, PrintIL={1} @@@\n", printAssembly, printIL);
        }
    }
}
