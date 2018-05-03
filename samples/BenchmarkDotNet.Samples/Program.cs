﻿using System;
using System.Diagnostics;
using System.Reflection;
using System.Text;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Samples.Algorithms;
using BenchmarkDotNet.Samples.Intro;
using BenchmarkDotNet.Validators;
using EncodingInfo = BenchmarkDotNet.Encodings.EncodingInfo;

namespace BenchmarkDotNet.Samples
{
    class Program
    {
        static void Main(string[] args)
        {
            BenchmarkSwitcher.FromAssembly(typeof(Program).GetTypeInfo().Assembly).Run(args);
        }
    }
}