using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BenchmarkDotNet.Characteristics;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Toolchains.Results;

namespace BenchmarkDotNet.Toolchains.Uap
{
    internal class UapBuilder : IBuilder
    {

        private const string Configuration = "Release";

        internal const string OutputDirectory = "binaries";

        private static readonly TimeSpan DefaultTimeout = TimeSpan.FromMinutes(10);

        public BuildResult Build(GenerateResult generateResult, ILogger logger, Benchmark benchmark, 
            IResolver resolver)
        {
            throw new NotImplementedException();
        }
    }
}
