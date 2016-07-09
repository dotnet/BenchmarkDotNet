using System;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Attributes.Jobs;

namespace BenchmarkDotNet.Samples.Other
{
    // You need RyuJit and AVX support for this benchmark
    [RyuJitX64Job]
    public class Math_DoubleSqrtAvx
    {
        // vxorpd      xmm0,xmm0,xmm0
        // vsqrtsd     xmm0,xmm0,xmm0
        // vsqrtsd     xmm1,xmm0,mmword ptr [7FFF83C14D88h]
        // vaddsd      xmm0,xmm0,xmm1
        // vsqrtsd     xmm1,xmm0,mmword ptr [7FFF83C14D90h]
        // vaddsd      xmm0,xmm0,xmm1
        // vsqrtsd     xmm1,xmm0,mmword ptr [7FFF83C14D98h]
        // vaddsd      xmm0,xmm0,xmm1
        // vsqrtsd     xmm1,xmm0,mmword ptr [7FFF83C14DA0h]
        // vaddsd      xmm0,xmm0,xmm1
        // vsqrtsd     xmm1,xmm0,mmword ptr [7FFF83C14DA8h]
        // vaddsd      xmm0,xmm0,xmm1
        // vsqrtsd     xmm1,xmm0,mmword ptr [7FFF83C14DB0h]
        // vaddsd      xmm0,xmm0,xmm1
        // vsqrtsd     xmm1,xmm0,mmword ptr [7FFF83C14DB8h]
        // vaddsd      xmm0,xmm0,xmm1
        // vsqrtsd     xmm1,xmm0,mmword ptr [7FFF83C14DC0h]
        // vaddsd      xmm0,xmm0,xmm1
        // vsqrtsd     xmm1,xmm0,mmword ptr [7FFF83C14DC8h]
        // vaddsd      xmm0,xmm0,xmm1
        // vsqrtsd     xmm1,xmm0,mmword ptr [7FFF83C14DD0h]
        // vaddsd      xmm0,xmm0,xmm1
        // vsqrtsd     xmm1,xmm0,mmword ptr [7FFF83C14DD8h]
        // vaddsd      xmm0,xmm0,xmm1
        // vsqrtsd     xmm1,xmm0,mmword ptr [7FFF83C14DE0h]
        // vaddsd      xmm0,xmm0,xmm1
        [Benchmark]
        public double Sqrt13()
        {
            return
                Math.Sqrt(1) + Math.Sqrt(2) + Math.Sqrt(3) + Math.Sqrt(4) + Math.Sqrt(5) + Math.Sqrt(6) + Math.Sqrt(7) + Math.Sqrt(8) + Math.Sqrt(9) + Math.Sqrt(10) +
                Math.Sqrt(11) + Math.Sqrt(12) + Math.Sqrt(13);
        }

        // vmovsd      xmm0,qword ptr [7FFF83C04CE0h]
        // ret
        [Benchmark]
        public double Sqrt14()
        {
            return
                Math.Sqrt(1) + Math.Sqrt(2) + Math.Sqrt(3) + Math.Sqrt(4) + Math.Sqrt(5) + Math.Sqrt(6) + Math.Sqrt(7) + Math.Sqrt(8) + Math.Sqrt(9) + Math.Sqrt(10) +
                Math.Sqrt(11) + Math.Sqrt(12) + Math.Sqrt(13) + Math.Sqrt(14);
        }
    }
}