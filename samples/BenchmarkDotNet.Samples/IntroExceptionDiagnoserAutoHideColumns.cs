using BenchmarkDotNet.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace BenchmarkDotNet.Samples
{
    [ExceptionDiagnoser]
    public class IntroExceptionDiagnoserAutoHideColumns
    {
        [Benchmark]
        public void Benchmark()
        {
            Thread.Sleep(1);
        }
    }
}
