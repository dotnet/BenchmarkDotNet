using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
#if RELEASE
using BenchmarkDotNet.Properties;
#endif

[assembly: Guid("cbba82d3-e650-407f-a0f0-767891d4f04c")]

[assembly: CLSCompliant(true)]

#if RELEASE
[assembly: InternalsVisibleTo("BenchmarkDotNet.Tests,PublicKey=" + BenchmarkDotNetInfo.PublicKey)]
[assembly: InternalsVisibleTo("BenchmarkDotNet.IntegrationTests,PublicKey=" + BenchmarkDotNetInfo.PublicKey)]
[assembly: InternalsVisibleTo("BenchmarkDotNet.Diagnostics.Windows,PublicKey=" + BenchmarkDotNetInfo.PublicKey)]
[assembly: InternalsVisibleTo("BenchmarkDotNet.Diagnostics.dotTrace,PublicKey=" + BenchmarkDotNetInfo.PublicKey)]
[assembly: InternalsVisibleTo("BenchmarkDotNet.IntegrationTests.ManualRunning,PublicKey=" + BenchmarkDotNetInfo.PublicKey)]
[assembly: InternalsVisibleTo("BenchmarkDotNet.IntegrationTests.ManualRunning.MultipleFrameworks,PublicKey=" + BenchmarkDotNetInfo.PublicKey)]
#else
[assembly: InternalsVisibleTo("BenchmarkDotNet.Tests")]
[assembly: InternalsVisibleTo("BenchmarkDotNet.IntegrationTests")]
[assembly: InternalsVisibleTo("BenchmarkDotNet.Diagnostics.Windows")]
[assembly: InternalsVisibleTo("BenchmarkDotNet.Diagnostics.dotTrace")]
[assembly: InternalsVisibleTo("BenchmarkDotNet.IntegrationTests.ManualRunning")]
[assembly: InternalsVisibleTo("BenchmarkDotNet.IntegrationTests.ManualRunning.MultipleFrameworks")]
#endif