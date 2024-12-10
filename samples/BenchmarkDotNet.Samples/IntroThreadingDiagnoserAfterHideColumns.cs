﻿using BenchmarkDotNet.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace BenchmarkDotNet.Samples
{
    [ThreadingDiagnoser(false, true)]
    public class IntroThreadingDiagnoserAfterHideColumns
    {
        [Benchmark]
        public void Benchmark()
        {
            Thread.Sleep(1);
        }
    }
}