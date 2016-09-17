using System;
using System.Reflection;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Attributes;

namespace BenchmarkDotNet.Samples
{
    class Program
    {
        static void Main(string[] args)
        {
            new BenchmarkSwitcher(typeof(Program).GetTypeInfo().Assembly).Run(args);
        }
    }
}