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
    public class IntroExceptionDiagnoserAfterHideColumns
    {
        [Benchmark]
        public void Benchmark()
        {
            Thread.Sleep(1);
        }

        [Benchmark] public void NoThrow() { }
        [Benchmark]
        public void Throw()
        {
            //try { throw new Exception(); } catch { }
        }
    }
}
