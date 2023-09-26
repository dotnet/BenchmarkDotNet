using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Characteristics;
using BenchmarkDotNet.Exporters;
using BenchmarkDotNet.Extensions;
using BenchmarkDotNet.Running;
using Microsoft.TestPlatform.AdapterUtilities;
using Microsoft.TestPlatform.AdapterUtilities.ManagedNameUtilities;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using System;
using System.Linq;

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
        /// <param name="source">The dll or exe of the benchmark project.</param>
        /// <param name="includeJobInName">Whether or not the display name should include the job name.</param>
        /// <returns>The VSTest TestCase.</returns>
        internal static TestCase ToVSTestCase(this BenchmarkCase benchmarkCase, string source, bool includeJobInName=false)
        {
            var benchmarkMethod = benchmarkCase.Descriptor.WorkloadMethod;
            var fullClassName = benchmarkCase.Descriptor.Type.FullName;
            var benchmarkMethodName = benchmarkCase.Descriptor.WorkloadMethodDisplayInfo;
            var benchmarkFullName = $"{fullClassName}.{benchmarkMethodName}";

            ManagedNameHelper.GetManagedName(benchmarkMethod, out var managedType, out var managedMethod, out var hierarchyValues);
            hierarchyValues[HierarchyConstants.Levels.ContainerIndex] = null; // Gets set by the test explorer window to the test project name
            if (includeJobInName)
            {
                hierarchyValues[HierarchyConstants.Levels.TestGroupIndex] += $" [{benchmarkCase.GetUnrandomizedJobDisplayInfo()}]";
            }

            var hasManagedMethodAndTypeProperties = !string.IsNullOrWhiteSpace(managedType) && !string.IsNullOrWhiteSpace(managedMethod);

            var vsTestCase = new TestCase(benchmarkFullName, VSTestAdapter.ExecutorUri, source)
            {
                DisplayName = FullNameProvider.GetBenchmarkName(benchmarkCase),
                Id = GetTestCaseId(benchmarkCase),
            };

            if (includeJobInName)
            {
                vsTestCase.DisplayName += $" [{benchmarkCase.GetUnrandomizedJobDisplayInfo()}]";
            }

            var benchmarkAttribute = benchmarkMethod.ResolveAttribute<BenchmarkAttribute>();
            if (benchmarkAttribute != null)
            {
                vsTestCase.CodeFilePath = benchmarkAttribute.SourceCodeFile;
                vsTestCase.LineNumber = benchmarkAttribute.SourceCodeLineNumber;
            }

            vsTestCase.SetPropertyValue(VSTestProperties.HierarchyProperty, hierarchyValues.ToArray());
            vsTestCase.SetPropertyValue(VSTestProperties.TestCategoryProperty, benchmarkCase.Descriptor.Categories);
            if (hasManagedMethodAndTypeProperties)
            {
                vsTestCase.SetPropertyValue(VSTestProperties.ManagedTypeProperty, managedType);
                vsTestCase.SetPropertyValue(VSTestProperties.ManagedMethodProperty, managedMethod);
                vsTestCase.SetPropertyValue(VSTestProperties.TestClassNameProperty, managedType);
            }
            else
            {
                vsTestCase.SetPropertyValue(VSTestProperties.TestClassNameProperty, fullClassName);
            }

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
            if (!benchmarkCase.Job.HasValue(CharacteristicObject.IdCharacteristic) && benchmarkCase.Job.ResolvedId.StartsWith("Job-", StringComparison.OrdinalIgnoreCase))
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
            testIdProvider.AppendString(VSTestAdapter.ExecutorUriString);
            testIdProvider.AppendString(benchmarkCase.Descriptor.DisplayInfo);
            testIdProvider.AppendString(benchmarkCase.GetUnrandomizedJobDisplayInfo());
            testIdProvider.AppendString(benchmarkCase.Parameters.DisplayInfo);
            return testIdProvider.GetId();
        }
    }
}
