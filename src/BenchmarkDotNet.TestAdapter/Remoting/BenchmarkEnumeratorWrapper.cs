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
        /// Gets a list of VSTest TestCases from the given source.
        /// Each test case is serialize into a string so that it can be used across AppDomain boundaries.
        /// </summary>
        /// <param name="source">The dll or exe of the benchmark project.</param>
        /// <returns>The serialized test cases.</returns>
        public List<string> GetTestCasesFromSourceSerialized(string source)
        {
            var serializedTestCases = new List<string>();
            foreach (var runInfo in BenchmarkEnumerator.GetBenchmarksFromSource(source))
            {
                // If all the benchmarks have the same job, then no need to include job info.
                var needsJobInfo = runInfo.BenchmarksCases.Select(c => c.Job.DisplayInfo).Distinct().Count() > 1;
                foreach (var benchmarkCase in runInfo.BenchmarksCases)
                {
                    var testCase = benchmarkCase.ToVSTestCase(source, needsJobInfo);
                    serializedTestCases.Add(SerializationHelpers.Serialize(testCase));
                }
            }

            return serializedTestCases;
        }
    }
}
