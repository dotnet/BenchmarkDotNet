using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using BenchmarkDotNet.Properties;

[assembly: Guid("cbba82d3-e650-407f-a0f0-767891d4f04c")]

[assembly: CLSCompliant(true)]

#if RELEASE
[assembly: InternalsVisibleTo("BenchmarkDotNet.Tests,PublicKey=" + BenchmarkDotNetInfo.PublicKey)]
[assembly: InternalsVisibleTo("BenchmarkDotNet.IntegrationTests,PublicKey=" + BenchmarkDotNetInfo.PublicKey)]
[assembly: InternalsVisibleTo("BenchmarkDotNet.Samples,PublicKey=" + BenchmarkDotNetInfo.PublicKey)]
#else
[assembly: InternalsVisibleTo("BenchmarkDotNet.Tests")]
[assembly: InternalsVisibleTo("BenchmarkDotNet.IntegrationTests")]
[assembly: InternalsVisibleTo("BenchmarkDotNet.Samples")]
#endif