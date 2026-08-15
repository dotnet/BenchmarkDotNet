using Microsoft.Testing.Platform.Builder;
using System.ComponentModel;

namespace BenchmarkDotNet.TestingPlatform
{
    /// <summary>
    /// The hook Microsoft.Testing.Platform.MSBuild calls from the entry point it generates for the benchmark project.
    /// </summary>
    /// <remarks>
    /// This is wired up by the TestingPlatformBuilderHook item in BenchmarkDotNet.TestingPlatform.props.
    /// It is public because the generated code lives in the benchmark assembly, but it is not meant to be called
    /// directly; use <see cref="TestApplicationBuilderExtensions.AddBenchmarkDotNet(ITestApplicationBuilder)"/>
    /// instead when writing an entry point by hand.
    /// </remarks>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public static class TestingPlatformBuilderHook
    {
        /// <summary>
        /// Registers BenchmarkDotNet with the test application being built.
        /// </summary>
        /// <param name="builder">The builder of the test application.</param>
        /// <param name="arguments">The command line arguments of the process. Unused.</param>
        public static void AddExtensions(ITestApplicationBuilder builder, string[] arguments)
            => builder.AddBenchmarkDotNet();
    }
}
