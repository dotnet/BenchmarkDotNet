using Microsoft.Testing.Platform.Builder;
using Microsoft.Testing.Platform.Capabilities.TestFramework;
using Microsoft.Testing.Platform.Helpers;
using System.Reflection;

namespace BenchmarkDotNet.TestingPlatform
{
    /// <summary>
    /// Extensions for registering BenchmarkDotNet with a Microsoft.Testing.Platform application.
    /// </summary>
    public static class TestApplicationBuilderExtensions
    {
        /// <summary>
        /// Registers BenchmarkDotNet as the test framework, so that the benchmarks of the entry assembly are exposed
        /// as tests.
        /// </summary>
        /// <param name="builder">The builder of the test application.</param>
        /// <returns>The same builder, so that calls can be chained.</returns>
        public static ITestApplicationBuilder AddBenchmarkDotNet(this ITestApplicationBuilder builder)
            => builder.AddBenchmarkDotNet(
                Assembly.GetEntryAssembly() ?? throw new InvalidOperationException(
                    "There is no entry assembly to look for benchmarks in. Use the overload that takes an assembly."));

        /// <summary>
        /// Registers BenchmarkDotNet as the test framework, so that the benchmarks of the given assembly are exposed
        /// as tests.
        /// </summary>
        /// <param name="builder">The builder of the test application.</param>
        /// <param name="assembly">The assembly to look for benchmarks in.</param>
        /// <returns>The same builder, so that calls can be chained.</returns>
        public static ITestApplicationBuilder AddBenchmarkDotNet(this ITestApplicationBuilder builder, Assembly assembly)
        {
            if (builder == null)
                throw new ArgumentNullException(nameof(builder));
            if (assembly == null)
                throw new ArgumentNullException(nameof(assembly));

            builder.RegisterTestFramework(
                _ => new TestFrameworkCapabilities(),
                (capabilities, serviceProvider) => new BenchmarkTestFramework(capabilities, serviceProvider, assembly));

            // Opts into the tree node filter, which is what backs `--filter "/*/*/MyBenchmarks/*"`.
#pragma warning disable TPEXP // The tree node filter is still marked as experimental by the platform.
            builder.AddTreeNodeFilterService(new BenchmarkDotNetExtension());
#pragma warning restore TPEXP

            return builder;
        }
    }
}
