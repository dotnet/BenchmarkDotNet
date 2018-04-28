using System;
using System.Diagnostics;
using System.Reflection;
using System.Text;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Samples.Algorithms;
using BenchmarkDotNet.Samples.Intro;
using EncodingInfo = BenchmarkDotNet.Encodings.EncodingInfo;

namespace BenchmarkDotNet.Samples
{
    class Program
    {
        static void Main(string[] args)
        {
            //Console.WriteLine(Console.OutputEncoding);
            //Console.OutputEncoding = Encoding.Unicode;
            //EncodingInfo.CurrentEncoding = Encoding.Unicode;
            //Console.WriteLine(Console.OutputEncoding);
            Console.WriteLine("\u03BCs");
            Console.WriteLine();
            //BenchmarkRunner.Run<IntroConfigSource>();
            BenchmarkRunner.Run<Algo_BitCount>();
            //BenchmarkSwitcher.FromAssembly(typeof(Program).GetTypeInfo().Assembly).Run(args);
            Console.Read();
        }
    }
}