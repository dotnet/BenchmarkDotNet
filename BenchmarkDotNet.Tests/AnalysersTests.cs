using System.Collections.Generic;
using System.Linq;
using BenchmarkDotNet.Reports;
using BenchmarkDotNet.Jobs;
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