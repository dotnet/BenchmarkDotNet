using BenchmarkDotNet.Properties;
using System.Runtime.CompilerServices;

[assembly: CLSCompliant(true)]

[assembly: InternalsVisibleTo("BenchmarkDotNet.Tests,PublicKey=" + BenchmarkDotNetInfo.PublicKey)]
[assembly: InternalsVisibleTo("BenchmarkDotNet.IntegrationTests,PublicKey=" + BenchmarkDotNetInfo.PublicKey)]
[assembly: InternalsVisibleTo("BenchmarkDotNet.IntegrationTests.MonoBenchmarks,PublicKey=" + BenchmarkDotNetInfo.PublicKey)]
[assembly: InternalsVisibleTo("BenchmarkDotNet.IntegrationTests.WasmBenchmarks,PublicKey=" + BenchmarkDotNetInfo.PublicKey)]
[assembly: InternalsVisibleTo("BenchmarkDotNet.Diagnostics.Windows,PublicKey=" + BenchmarkDotNetInfo.PublicKey)]
[assembly: InternalsVisibleTo("BenchmarkDotNet.Diagnostics.dotTrace,PublicKey=" + BenchmarkDotNetInfo.PublicKey)]
[assembly: InternalsVisibleTo("BenchmarkDotNet.Diagnostics.dotMemory,PublicKey=" + BenchmarkDotNetInfo.PublicKey)]
[assembly: InternalsVisibleTo("BenchmarkDotNet.IntegrationTests.ManualRunning,PublicKey=" + BenchmarkDotNetInfo.PublicKey)]
[assembly: InternalsVisibleTo("BenchmarkDotNet.IntegrationTests.ManualRunning.MultipleFrameworks,PublicKey=" + BenchmarkDotNetInfo.PublicKey)]
[assembly: InternalsVisibleTo("BenchmarkDotNet.IntegrationTests.RuntimeAsync,PublicKey=" + BenchmarkDotNetInfo.PublicKey)]
[assembly: InternalsVisibleTo("BenchmarkDotNet.TestAdapter,PublicKey=" + BenchmarkDotNetInfo.PublicKey)]
