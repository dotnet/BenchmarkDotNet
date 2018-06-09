using System;
using System.Diagnostics;
using BenchmarkDotNet.Attributes;

namespace BenchmarkDotNet.Samples.Intro
{
    [EncodingAttribute.Unicode]
    public class IntroConfigEncoding
    {
        [Benchmark]
        public long Foo()
        {
            long waitUntil = Stopwatch.GetTimestamp() + 1000;
            while (Stopwatch.GetTimestamp() < waitUntil)
            {
            }
            return waitUntil;
        }
    }
}