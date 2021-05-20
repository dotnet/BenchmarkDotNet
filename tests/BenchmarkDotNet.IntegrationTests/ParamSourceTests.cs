using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using Xunit;

namespace BenchmarkDotNet.IntegrationTests
{
    public class ParamSourceTests: BenchmarkTestExecutor
    {
        [Fact]
        public void ParamSourceCanHandleStringWithSurrogates()
        {
            CanExecute<ParamSourceIsStringWithSurrogates>(CreateSimpleConfig());
        }

        public class ParamSourceIsStringWithSurrogates
        {
            public IEnumerable<string> StringValues
            {
                get
                {
                    yield return "a" + string.Join("", Enumerable.Repeat("😀", 40)) + "a";
                    yield return "a" + string.Join("", Enumerable.Repeat("😀", 40));
                    yield return string.Join("", Enumerable.Repeat("😀", 40)) + "a";
                    yield return string.Join("", Enumerable.Repeat("😀", 40));
                }
            }

            [ParamsSource(nameof(StringValues))]
            public string _ { get; set; }

            [Benchmark]
            public void Method() { }
        }
    }
}
