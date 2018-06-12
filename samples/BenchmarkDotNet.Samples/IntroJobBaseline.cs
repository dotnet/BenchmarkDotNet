﻿using BenchmarkDotNet.Attributes;

namespace BenchmarkDotNet.Samples
{
    [ClrJob(isBaseline: true)]
    [MonoJob]
    [CoreJob]
    public class IntroJobBaseline
    {
        [Benchmark]
        public int SplitJoin() 
            => string.Join(",", new string[1000]).Split(',').Length;
    }
}