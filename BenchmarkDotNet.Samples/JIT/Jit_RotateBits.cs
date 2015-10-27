using BenchmarkDotNet.Tasks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;

namespace BenchmarkDotNet.Samples.JIT
{
    [BenchmarkTask(platform: BenchmarkPlatform.X64, jitVersion: BenchmarkJitVersion.LegacyJit, runtime: BenchmarkRuntime.Default)]
    //[BenchmarkTask(platform: BenchmarkPlatform.X64, runtime: BenchmarkRuntime.CoreClr)]
    public class Jit_RotateBits
    {
        private ulong a = 2340988;
        private ulong b = 123444;
        private ulong c = 1;
        private ulong d = 23444111111;

        [Benchmark]
        [OperationsPerInvoke(4)]        
        public void RotateBits()
        {
            RotateRight64(a, 16);
            RotateRight64(b, 24);
            RotateRight64(c, 32);
            RotateRight64(d, 48);
        }
        
        public static ulong RotateRight64(ulong value, int count)
        {
            return (value >> count) | (value << (64 - count));
        }
    }
}
