using System;
using System.Runtime.CompilerServices;
using BenchmarkDotNet.Properties;

[assembly: CLSCompliant(true)]

#if RELEASE
[assembly: InternalsVisibleTo("BenchmarkDotNet.Tests,PublicKey=" + BenchmarkDotNetInfo.PublicKey)]
#else
[assembly: InternalsVisibleTo("BenchmarkDotNet.Tests")]
#endif