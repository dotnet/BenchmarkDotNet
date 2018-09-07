using System;
using System.Collections.Generic;
using BenchmarkDotNet.Diagnostics.Windows;
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
            
            sut.HandleIterationEvent(0, IterationMode.Overhead); // start but no stop later on

            Assert.Throws<InvalidOperationException>(() => sut.CalculateMetrics(null));
        }

        [Fact]
        public void MetricsAreCorrectlyCalculatedPerIteration()
        {
            const int profileSourceId = 123;
            const int interval = 100;
            const ulong ip = 12345;
            
            var sut = new ProcessMetrics();
            
            for (int i = 0; i < 20; i++)
            {
                sut.HandleIterationEvent(i, IterationMode.Overhead); // Overhead iteration start at i
            
                sut.HandleNewSample(i + 0.1, ip, profileSourceId); // Engine overhead produces one PMC event per iteration
            
                sut.HandleIterationEvent(i + 0.5, IterationMode.Overhead); // Overhead iteration stop at i + 0.5
            }
            
            for (int i = 20; i < 40; i++)
            {
                sut.HandleIterationEvent(i, IterationMode.Workload); // Workload iteration start at i
            
                sut.HandleNewSample(i + 0.1, ip, profileSourceId); // Engine overhead produces one PMC event per iteration
                sut.HandleNewSample(i + 0.2, ip, profileSourceId); // benchmarked code
                sut.HandleNewSample(i + 0.3, ip, profileSourceId); // benchmarked code
                sut.HandleNewSample(i + 0.4, ip, profileSourceId); // benchmarked code
            
                sut.HandleIterationEvent(i + 0.5, IterationMode.Workload); // Workload iteration stop at i + 0.5
            }

            var metrics = sut.CalculateMetrics(new Dictionary<int, int> { {profileSourceId, interval }});

            const ulong expected = 4 * interval - interval; // every workload was 4 events, the overhead was one
            
            Assert.All(metrics, metric => Assert.Equal(expected, metric.Value));
        }
    }
}