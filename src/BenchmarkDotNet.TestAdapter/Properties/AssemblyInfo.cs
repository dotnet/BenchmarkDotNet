using BenchmarkDotNet.Properties;
using System.Runtime.CompilerServices;

// Drives the adapter's Microsoft.Testing.Platform types directly, to cover what a real test host cannot reach: a
// test execution filter no platform this was built against can produce, and the end of an application whose request
// never completed. See that project's README.
[assembly: InternalsVisibleTo("BenchmarkDotNet.IntegrationTests.TestingPlatform.Internals,PublicKey=" + BenchmarkDotNetInfo.PublicKey)]
