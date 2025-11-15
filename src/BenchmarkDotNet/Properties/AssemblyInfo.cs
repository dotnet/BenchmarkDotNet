using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using BenchmarkDotNet.Properties;

[assembly: Guid("cbba82d3-e650-407f-a0f0-767891d4f04c")]

[assembly: CLSCompliant(true)]

[assembly: InternalsVisibleTo("BenchmarkDotNet.Tests,PublicKey=" + BenchmarkDotNetInfo.PublicKey)]
[assembly: InternalsVisibleTo("BenchmarkDotNet.IntegrationTests,PublicKey=" + BenchmarkDotNetInfo.PublicKey)]
[assembly: InternalsVisibleTo("BenchmarkDotNet.Diagnostics.Windows,PublicKey=" + BenchmarkDotNetInfo.PublicKey)]
[assembly: InternalsVisibleTo("BenchmarkDotNet.Diagnostics.dotTrace,PublicKey=" + BenchmarkDotNetInfo.PublicKey)]
[assembly: InternalsVisibleTo("BenchmarkDotNet.Diagnostics.dotMemory,PublicKey=" + BenchmarkDotNetInfo.PublicKey)]
[assembly: InternalsVisibleTo("BenchmarkDotNet.Disassembler,PublicKey=" + BenchmarkDotNetInfo.PublicKey)]
[assembly: InternalsVisibleTo("BenchmarkDotNet.IntegrationTests.ManualRunning,PublicKey=" + BenchmarkDotNetInfo.PublicKey)]
[assembly: InternalsVisibleTo("BenchmarkDotNet.IntegrationTests.ManualRunning.MultipleFrameworks,PublicKey=" + BenchmarkDotNetInfo.PublicKey)]
[assembly: InternalsVisibleTo("BenchmarkDotNet.TestAdapter,PublicKey=" + BenchmarkDotNetInfo.PublicKey)]
