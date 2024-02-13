using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Characteristics;
using BenchmarkDotNet.Exporters;
using BenchmarkDotNet.Extensions;
using BenchmarkDotNet.Running;
using Microsoft.TestPlatform.AdapterUtilities;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using System;

namespace BenchmarkDotNet.TestAdapter
{
    /// <summary>
    /// A set of extensions for BenchmarkCase to support converting to VSTest TestCase objects.
    /// </summary>
    internal static class BenchmarkCaseExtensions
    {
        /// <summary>
        /// Converts a BDN BenchmarkCase to a VSTest TestCase.
        /// </summary>
        /// <param name="benchmarkCase">The BenchmarkCase to convert.</param>
        /// <param name="assemblyPath">The dll or exe of the benchmark project.</param>
        /// <param name="includeJobInName">Whether or not the display name should include the job name.</param>
        /// <returns>The VSTest TestCase.</returns>
        internal static TestCase ToVsTestCase(this BenchmarkCase benchmarkCase, string assemblyPath, bool includeJobInName = false)
        {
            var benchmarkMethod = benchmarkCase.Descriptor.WorkloadMethod;
            var fullClassName = benchmarkCase.Descriptor.Type.GetCorrectCSharpTypeName();
            var parametrizedMethodName = FullNameProvider.GetMethodName(benchmarkCase);

            var displayJobInfo = benchmarkCase.GetUnrandomizedJobDisplayInfo();
            var displayMethodName = parametrizedMethodName + (includeJobInName ? $" [{displayJobInfo}]" : "");
            var displayName = $"{fullClassName}.{displayMethodName}";

            // We use displayName as FQN to workaround the Rider/R# problem with FQNs processing
            // See: https://github.com/dotnet/BenchmarkDotNet/issues/2494
            var fullyQualifiedName = displayName;

            var vsTestCase = new TestCase(fullyQualifiedName, VsTestAdapter.ExecutorUri, assemblyPath)
            {
                DisplayName = displayName,
                Id = GetTestCaseId(benchmarkCase)
            };

            var benchmarkAttribute = benchmarkMethod.ResolveAttribute<BenchmarkAttribute>();
            if (benchmarkAttribute != null)
            {
                vsTestCase.CodeFilePath = benchmarkAttribute.SourceCodeFile;
                vsTestCase.LineNumber = benchmarkAttribute.SourceCodeLineNumber;
            }

            var categories = DefaultCategoryDiscoverer.Instance.GetCategories(benchmarkMethod);
            foreach (var category in categories)
                vsTestCase.Traits.Add("Category", category);

            vsTestCase.Traits.Add("", "BenchmarkDotNet");

            return vsTestCase;
        }

        /// <summary>
        /// If an ID is not provided, a random string is used for the ID. This method will identify if randomness was
        /// used for the ID and return the Job's DisplayInfo with that randomness removed so that the same benchmark
        /// can be referenced across multiple processes.
        /// </summary>
        /// <param name="benchmarkCase">The benchmark case.</param>
        /// <returns>The benchmark case' job's DisplayInfo without randomness.</returns>
        internal static string GetUnrandomizedJobDisplayInfo(this BenchmarkCase benchmarkCase)
        {
            var jobDisplayInfo = benchmarkCase.Job.DisplayInfo;
            if (!benchmarkCase.Job.HasValue(CharacteristicObject.IdCharacteristic) &&
                benchmarkCase.Job.ResolvedId.StartsWith("Job-", StringComparison.OrdinalIgnoreCase))
            {
                // Replace Job-ABCDEF with Job
                jobDisplayInfo = "Job" + jobDisplayInfo.Substring(benchmarkCase.Job.ResolvedId.Length);
            }

            return jobDisplayInfo;
        }

        /// <summary>
        /// Gets an ID for a given BenchmarkCase that is uniquely identifiable from discovery to execution phase.
        /// </summary>
        /// <param name="benchmarkCase">The benchmark case.</param>
        /// <returns>The test case ID.</returns>
        internal static Guid GetTestCaseId(this BenchmarkCase benchmarkCase)
        {
            var testIdProvider = new TestIdProvider();
            testIdProvider.AppendString(VsTestAdapter.ExecutorUriString);
            testIdProvider.AppendString(benchmarkCase.Descriptor.Type.Namespace ?? string.Empty);
            testIdProvider.AppendString(benchmarkCase.Descriptor.DisplayInfo);
            testIdProvider.AppendString(benchmarkCase.GetUnrandomizedJobDisplayInfo());
            testIdProvider.AppendString(benchmarkCase.Parameters.DisplayInfo);
            return testIdProvider.GetId();
        }
    }
}