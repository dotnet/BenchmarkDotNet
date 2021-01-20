#if NETFRAMEWORK
using System;
using System.Collections.Generic;
using System.Linq;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Diagnostics.Windows.Tracing;
using BenchmarkDotNet.Engines;
using Xunit;

namespace BenchmarkDotNet.IntegrationTests.Diagnosers
{
    public class ProcessMetricsTests
    {
        [Fact]
        public void TheNumberOfStopEventsMustBeEqualToStartEvents()
        {
            var sut = new ProcessMetrics();

            sut.HandleIterationEvent(0, IterationMode.Overhead, 100); // start but no stop later on

            Assert.Throws<InvalidOperationException>(() => sut.CalculateMetrics(null, Array.Empty<PreciseMachineCounter>()));
        }

        [Fact]
        public void TheNumberOfTotalOperationsPerIterationIsTheSameForAllIterations()
        {
            var sut = new ProcessMetrics();

            sut.HandleIterationEvent(0, IterationMode.Workload, 100); // start

            Assert.Throws<InvalidOperationException>(() => sut.HandleIterationEvent(0, IterationMode.Workload, 100 + 1));
        }

        [Fact]
        public void MetricsAreCorrectlyCalculatedPerIteration()
        {
            const int profileSourceId = 123;
            const int interval = 100;
            const ulong ip = 12345;
            const long totalOperations = 100;

            var sut = new ProcessMetrics();

            for (int relativeTimestamp = 0; relativeTimestamp < 20; relativeTimestamp++)
            {
                sut.HandleIterationEvent(relativeTimestamp, IterationMode.Overhead, totalOperations); // Overhead iteration start at i

                sut.HandleNewSample(relativeTimestamp + 0.1, ip, profileSourceId); // Engine overhead produces one PMC event per iteration

                sut.HandleIterationEvent(relativeTimestamp + 0.5, IterationMode.Overhead, totalOperations); // Overhead iteration stop at i + 0.5
            }

            for (int relativeTimestamp = 20; relativeTimestamp < 40; relativeTimestamp++)
            {
                sut.HandleIterationEvent(relativeTimestamp, IterationMode.Workload, totalOperations); // Workload iteration start at i

                sut.HandleNewSample(relativeTimestamp + 0.1, ip, profileSourceId); // Engine overhead produces one PMC event per iteration
                sut.HandleNewSample(relativeTimestamp + 0.2, ip, profileSourceId); // benchmarked code
                sut.HandleNewSample(relativeTimestamp + 0.3, ip, profileSourceId); // benchmarked code
                sut.HandleNewSample(relativeTimestamp + 0.4, ip, profileSourceId); // benchmarked code

                sut.HandleIterationEvent(relativeTimestamp + 0.5, IterationMode.Workload, totalOperations); // Workload iteration stop at i + 0.5
            }

            var metrics = sut.CalculateMetrics(
                new Dictionary<int, int> { {profileSourceId, interval }},
                new[]{ new PreciseMachineCounter(profileSourceId, "test", HardwareCounter.InstructionRetired, interval), });

            const ulong expected = (4 * interval - interval) / totalOperations; // every workload was 4 events, the overhead was one and totalOperations times per iteration

            Assert.Equal(expected, metrics.Single().Value);
        }
    }
}
#endif