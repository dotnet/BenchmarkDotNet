using BenchmarkDotNet.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BenchmarkDotNet.Samples
{
    public class IntroMemoryRandomization
    {
        [Params(512 * 4)]
        public int Size;

        private int[] _array;
        private int[] _destination;

        [GlobalSetup]
        public void Setup()
        {
            _array = new int[Size];
            _destination = new int[Size];
        }

        [Benchmark]
        public void Array() => System.Array.Copy(_array, _destination, Size);
    }
}
