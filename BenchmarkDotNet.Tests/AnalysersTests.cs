using System.Collections.Generic;
using System.Linq;
using BenchmarkDotNet.Plugins.Analyzers;
using BenchmarkDotNet.Reports;
using BenchmarkDotNet.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace BenchmarkDotNet.Tests
{
    public class AnalysersTests
    {
        private readonly ITestOutputHelper output;

        public AnalysersTests(ITestOutputHelper output)
        {
            this.output = output;
        }
    }
}