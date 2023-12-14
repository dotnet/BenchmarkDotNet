using System;
using System.Collections.Generic;
using System.Linq;

namespace BenchmarkDotNet.TestAdapter.Remoting
{
    /// <summary>
    /// A wrapper around the BenchmarkEnumerator for passing data across AppDomain boundaries.
    /// </summary>
    internal class BenchmarkEnumeratorWrapper : MarshalByRefObject
    {
        /// <summary>
        /// Gets a list of VSTest TestCases from the given assembly.
        /// Each test case is serialized into a string so that it can be used across AppDomain boundaries.
        /// </summary>
        /// <param name="assemblyPath">The dll or exe of the benchmark project.</param>
        /// <returns>The serialized test cases.</returns>
        public List<string> GetTestCasesFromAssemblyPathSerialized(string assemblyPath)
        {
            var serializedTestCases = new List<string>();
            foreach (var runInfo in BenchmarkEnumerator.GetBenchmarksFromAssemblyPath(assemblyPath))
            {
                // If all the benchmarks have the same job, then no need to include job info.
                var needsJobInfo = runInfo.BenchmarksCases.Select(c => c.Job.DisplayInfo).Distinct().Count() > 1;
                foreach (var benchmarkCase in runInfo.BenchmarksCases)
                {
                    var testCase = benchmarkCase.ToVsTestCase(assemblyPath, needsJobInfo);
                    serializedTestCases.Add(SerializationHelpers.Serialize(testCase));
                }
            }

            return serializedTestCases;
        }
    }
}
