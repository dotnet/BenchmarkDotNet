using System;
using System.Runtime.CompilerServices;
using BenchmarkDotNet.Properties;

[assembly: CLSCompliant(true)]

[assembly: InternalsVisibleTo("BenchmarkDotNet.Tests,PublicKey=" + BenchmarkDotNetInfo.PublicKey)]
