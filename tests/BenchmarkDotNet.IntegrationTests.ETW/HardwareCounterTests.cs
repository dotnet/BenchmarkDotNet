using System.Collections.Immutable;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Diagnostics.Windows;
using BenchmarkDotNet.Jobs;
using Microsoft.Diagnostics.Tracing.Session;

namespace BenchmarkDotNet.IntegrationTests.ETW;

public class HardwareCounterTests(ITestOutputHelper output) : BenchmarkTestExecutor(output)
{
    [Fact]
    public void CustomHardwareCounterProfileAreSupported()
    {
        // Arrange
        string[] customCounterNames =
        [
            "FakeCacheMisses1",
            "FakeCacheMisses2",
            "FakeCacheMisses3",
        ];

        var config = DefaultConfig.Instance
            .AddJob(Job.Dry)
            .WithOptions(ConfigOptions.DisableOptimizationsValidator)
            .WithHardwareCounterProfile(new CustomHardwareCounterProfile(customCounterNames))
            .AddHardwareCounters(HardwareCounter.CacheMisses)
            .AddDiagnoser(new EtwProfiler
            {
                HardwareCounterProvider = new FakeHardwareCounterProvider(customCounterNames)
            });

        // Act
        var summary = CanExecute<SimpleBenchmark>(config, fullValidation: false);

        // Assert
        Assert.False(summary.HasCriticalValidationErrors, "The \"Summary\" should have NOT \"HasCriticalValidationErrors\"");
        Assert.Empty(summary.ValidationErrors);
    }

    private class CustomHardwareCounterProfile(params string[] variants) : IHardwareCounterProfile
    {
        public IEnumerable<string> GetVariants(HardwareCounter hardwareCounter) => variants;
    }

    /// <summary>
    /// Подменяет реальные счетчики на кастомные.
    /// </summary>
    private class FakeHardwareCounterProvider : IHardwareCounterProvider
    {
        private readonly Dictionary<string, ProfileSourceInfo> counters;

        public FakeHardwareCounterProvider(params string[] counterNames)
        {
            counters = TraceEventProfileSources.GetInfo();
            if (counters.Count == 0)
            {
                throw new Exception("No counters found");
            }

            var replaceCounters = counters.Values
                .Where(c => c.Name.Contains("branch", StringComparison.CurrentCultureIgnoreCase))
                .ToDictionary(s => s.Name, s => s);

            if (replaceCounters.Count == 0)
            {
                throw new Exception("No counters found");
            }

            var appendCounters = new List<ProfileSourceInfo>();
            foreach (string counterName in counterNames.Where(counterName => !counters.ContainsKey(counterName)))
            {
                var profileSource = replaceCounters.Values.First();
                counters.Remove(profileSource.Name);
                replaceCounters.Remove(profileSource.Name);

                appendCounters.Add(new ProfileSourceInfo
                {
                    Name = counterName,
                    ID = profileSource.ID,
                    Interval = profileSource.Interval,
                    MinInterval = profileSource.MinInterval,
                    MaxInterval = profileSource.MaxInterval,
                });
            }

            appendCounters.ForEach(counter => counters.Add(counter.Name, counter));
        }

        public Dictionary<string, ProfileSourceInfo> GetAvailableCounters() => counters;

        public void Configure(IEnumerable<PreciseMachineCounter> machineCounters)
        {
            foreach (var counter in machineCounters)
            {
                if (!counters.ContainsKey(counter.Name))
                {
                    throw new NotImplementedException("Not found counter: " + counter.Name);
                }
            }

            TraceEventProfileSources.Set( // it's a must have to get the events enabled!!
                machineCounters.Select(counter => counter.ProfileSourceId).ToArray(),
                machineCounters.Select(counter => counter.Interval).ToArray());
        }
    }

    public class SimpleBenchmark
    {
        private const int N = 7;
        private readonly int[] sorted = [0, 1, 2, 3, 4, 5, 6];
        private readonly int[] unsorted = [6, 5, 4, 3, 2, 1, 0];

        private static int Branch(int[] data)
        {
            int sum = 0;
            for (int i = 0; i < N; i++)
                if (data[i] >= 128)
                    sum += data[i];
            return sum;
        }

        [Benchmark]
        public int SortedBranch() => Branch(sorted);

        [Benchmark]
        public int UnsortedBranch() => Branch(unsorted);
    }
}