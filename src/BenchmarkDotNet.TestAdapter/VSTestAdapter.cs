using BenchmarkDotNet.TestAdapter.Remoting;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Adapter;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;

namespace BenchmarkDotNet.TestAdapter
{
    /// <summary>
    /// Discovers and executes benchmarks using the VSTest protocol.
    /// </summary>
    [ExtensionUri(ExecutorUriString)]
    [DefaultExecutorUri(ExecutorUriString)]
    [FileExtension(".dll")]
    [FileExtension(".exe")]
    public class VsTestAdapter : ITestExecutor, ITestDiscoverer
    {
        // This URI is used to identify the adapter.
        internal const string ExecutorUriString = "executor://BenchmarkDotNet.TestAdapter";
        internal static readonly Uri ExecutorUri = new Uri(ExecutorUriString);

        /// <summary>
        /// Cancellation token used to stop any benchmarks that are currently running.
        /// </summary>
        private CancellationTokenSource? cts = null;

        /// <summary>
        /// Discovers the benchmarks.
        /// </summary>
        /// <param name="sources">List of assemblies to search for benchmarks in.</param>
        /// <param name="discoveryContext">A context that the discovery is performed in.</param>
        /// <param name="logger">Logger that sends messages back to VSTest host.</param>
        /// <param name="discoverySink">Interface that provides methods for sending discovered benchmarks back to the host.</param>
        public void DiscoverTests(
            IEnumerable<string> sources,
            IDiscoveryContext discoveryContext,
            IMessageLogger logger,
            ITestCaseDiscoverySink discoverySink)
        {
            foreach (var source in sources)
            {
                ValidateSourceIsAssemblyOrThrow(source);
                foreach (var testCase in GetVsTestCasesFromAssembly(source, logger))
                {
                    discoverySink.SendTestCase(testCase);
                }
            }
        }

        /// <summary>
        /// Runs a given set of test cases that represent benchmarks.
        /// </summary>
        /// <param name="tests">The tests to run.</param>
        /// <param name="runContext">A context that the run is performed in.</param>
        /// <param name="frameworkHandle">Interface used for communicating with the VSTest host.</param>
        public void RunTests(IEnumerable<TestCase>? tests, IRunContext? runContext, IFrameworkHandle? frameworkHandle)
        {
            if (tests == null)
                throw new ArgumentNullException(nameof(tests));
            if (frameworkHandle == null)
                throw new ArgumentNullException(nameof(frameworkHandle));

            cts ??= new CancellationTokenSource();

            foreach (var testsPerAssembly in tests.GroupBy(t => t.Source))
                RunBenchmarks(testsPerAssembly.Key, frameworkHandle, testsPerAssembly);

            cts = null;
        }

        /// <summary>
        /// Runs all benchmarks in the given set of sources (assemblies).
        /// </summary>
        /// <param name="sources">The assemblies to run.</param>
        /// <param name="runContext">A context that the run is performed in.</param>
        /// <param name="frameworkHandle">Interface used for communicating with the VSTest host.</param>
        public void RunTests(IEnumerable<string>? sources, IRunContext? runContext, IFrameworkHandle? frameworkHandle)
        {
            if (sources == null)
                throw new ArgumentNullException(nameof(sources));
            if (frameworkHandle == null)
                throw new ArgumentNullException(nameof(frameworkHandle));

            cts ??= new CancellationTokenSource();

            foreach (var source in sources)
                RunBenchmarks(source, frameworkHandle);

            cts = null;
        }

        /// <summary>
        /// Stops any currently running benchmarks.
        /// </summary>
        public void Cancel()
        {
            cts?.Cancel();
        }

        /// <summary>
        /// Gets the VSTest test cases in the given assembly.
        /// </summary>
        /// <param name="assemblyPath">The dll or exe of the benchmark project.</param>
        /// <param name="logger">A logger that sends logs to VSTest.</param>
        /// <returns>The VSTest test cases inside the given assembly.</returns>
        private static List<TestCase> GetVsTestCasesFromAssembly(string assemblyPath, IMessageLogger logger)
        {
            try
            {
                // Ensure that the test enumeration is done inside the context of the source directory.
                var enumerator = (BenchmarkEnumeratorWrapper)CreateIsolatedType(typeof(BenchmarkEnumeratorWrapper), assemblyPath);
                var testCases = enumerator
                    .GetTestCasesFromAssemblyPathSerialized(assemblyPath)
                    .Select(SerializationHelpers.Deserialize<TestCase>)
                    .ToList();

                // Validate that all test ids are unique
                var idLookup = new Dictionary<Guid, string>();
                foreach (var testCase in testCases)
                {
                    if (idLookup.TryGetValue(testCase.Id, out var matchingCase))
                        throw new Exception($"Encountered Duplicate Test ID: '{testCase.DisplayName}' and '{matchingCase}'");

                    idLookup[testCase.Id] = testCase.DisplayName;
                }

                return testCases;
            }
            catch (Exception ex)
            {
                logger.SendMessage(TestMessageLevel.Error, $"Failed to load benchmarks from assembly\n{ex}");
                throw;
            }
        }

        /// <summary>
        /// Runs the benchmarks in the given source.
        /// </summary>
        /// <param name="source">The dll or exe of the benchmark project.</param>
        /// <param name="frameworkHandle">An interface used to communicate with the VSTest host.</param>
        /// <param name="testCases">
        /// The specific test cases to be run if specified.
        /// If unspecified, runs all the test cases in the source.
        /// </param>
        private void RunBenchmarks(string source, IFrameworkHandle frameworkHandle, IEnumerable<TestCase>? testCases = null)
        {
            ValidateSourceIsAssemblyOrThrow(source);

            // Create a HashSet of all the TestCase IDs to be run if specified.
            var caseIds = testCases == null ? null : new HashSet<Guid>(testCases.Select(c => c.Id));

            try
            {
                // Ensure that test execution is done inside the context of the source directory.
                var executor = (BenchmarkExecutorWrapper)CreateIsolatedType(typeof(BenchmarkExecutorWrapper), source);
                cts?.Token.Register(executor.Cancel);

                executor.RunBenchmarks(source, new TestExecutionRecorderWrapper(frameworkHandle), caseIds);
            }
            catch (Exception ex)
            {
                frameworkHandle.SendMessage(TestMessageLevel.Error, $"Failed to run benchmarks in assembly\n{ex}");
                throw;
            }
        }

        /// <summary>
        /// This will create the given type in a child AppDomain when used in .NET Framework.
        /// If not in the .NET Framework, it will use the current AppDomain.
        /// </summary>
        /// <param name="type">The type to create.</param>
        /// <param name="assemblyPath">The dll or exe of the benchmark project.</param>
        /// <returns>The created object.</returns>
        private static object CreateIsolatedType(Type type, string assemblyPath)
        {
            // .NET Framework runs require a custom AppDomain to be set up to run the benchmarks in because otherwise,
            // all the assemblies will be loaded from the VSTest console rather than from the directory that the BDN
            // program under test lives in. .NET Core assembly resolution is smarter and will correctly load the right
            // assembly versions as needed and does not require a custom AppDomain. Unfortunately, the APIs needed to
            // create the AppDomain for .NET Framework are not part of .NET Standard, and so a multi-targeting solution
            // such as this is required to get this to work. This same approach is also used by other .NET unit testing
            // libraries as well, further justifying this approach to solving how to get the correct assemblies loaded.
#if NETFRAMEWORK
            var appBase = Path.GetDirectoryName(assemblyPath);
            var setup = new AppDomainSetup { ApplicationBase = appBase };
            var domainName = $"Isolated Domain for {type.Name}";
            var appDomain = AppDomain.CreateDomain(domainName, null, setup);
            return appDomain.CreateInstanceAndUnwrap(
                type.Assembly.FullName, type.FullName, false, BindingFlags.Default, null, null, null, null);
#else
            return Activator.CreateInstance(type);
#endif
        }

        private static void ValidateSourceIsAssemblyOrThrow(string source)
        {
            if (string.IsNullOrEmpty(source))
                throw new ArgumentException($"'{nameof(source)}' cannot be null or whitespace.", nameof(source));

            if (!Path.HasExtension(source))
                throw new NotSupportedException($"Missing extension on source '{source}', must have the extension '.dll' or '.exe'.");

            var extension = Path.GetExtension(source);
            if (!string.Equals(extension, ".dll", StringComparison.OrdinalIgnoreCase) && !string.Equals(extension, ".exe", StringComparison.OrdinalIgnoreCase))
                throw new NotSupportedException($"Unsupported extension on source '{source}', must have the extension '.dll' or '.exe'.");
        }
    }
}
