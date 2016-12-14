using BenchmarkDotNet.Running;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace BenchmarkDotNet.Samples.Runner
{
    public class Program
    {
        public static void Main(string[] args)
        {
            BenchmarkSwitcher.FromAssembly(typeof(BenchmarkDotNet.Samples.Program).GetTypeInfo().Assembly).Run(args);
        }
    }
}
