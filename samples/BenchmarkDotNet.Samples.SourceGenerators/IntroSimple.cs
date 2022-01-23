using BenchmarkDotNet.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BenchmarkDotNet.Samples.SourceGenerators
{
    public class IntroSimple
    {
        [Params(1, 2, 3)]
        public int IntegerField;

        [Benchmark]
        public int Test() => IntegerField * IntegerField;
    }
}
