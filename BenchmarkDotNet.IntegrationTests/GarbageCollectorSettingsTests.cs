using System;
using System.Runtime;
using Xunit;
using Xunit.Abstractions;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Jobs;

namespace BenchmarkDotNet.IntegrationTests
{
    public class GarbageCollectorSettingsTests
    {
        private readonly ITestOutputHelper output;

        public GarbageCollectorSettingsTests(ITestOutputHelper outputHelper)
        {
            output = outputHelper;
        }

        [Fact]
        public void CanEnableServerGcMode()
        {
            var config = ManualConfig.CreateEmpty()
                                     .With(Job.Dry.With(new GarbageCollection { Server = true }));

            BenchmarkTestExecutor.CanExecute<ServerModeEnabled>(config);
        }

        [Fact]
        public void CanDisableServerGcMode()
        {
            var config = ManualConfig.CreateEmpty()
                                     .With(Job.Dry.With(new GarbageCollection { Server = false }));

            BenchmarkTestExecutor.CanExecute<WorkstationGcOnly>(config);
        }

        [Fact]
        public void CanEnableConcurrentGcMode()
        {
            var config = ManualConfig.CreateEmpty()
                                     .With(Job.Dry.With(new GarbageCollection { Concurrent = true }));

            BenchmarkTestExecutor.CanExecute<ConcurrentModeEnabled>(config);
        }

        [Fact]
        public void CanDisableConcurrentGcMode()
        {
            var config = ManualConfig.CreateEmpty()
                                     .With(Job.Dry.With(new GarbageCollection { Concurrent = false }));

            BenchmarkTestExecutor.CanExecute<ConcurrentModeDisabled>(config);
        }

        [Fact]
        public void CanAvoidForcingGarbageCollections()
        {
            var config = ManualConfig.CreateEmpty()
                                     .With(Job.Dry.With(new GarbageCollection { Force = false }));
            
            BenchmarkTestExecutor.CanExecute<AvoidForcingGarbageCollection>(config);
        }
    }

    public class ServerModeEnabled
    {
        [Benchmark]
        public void Benchmark()
        {
            if (GCSettings.IsServerGC == false)
            {
                throw new InvalidOperationException("Did not enable GC Server mode");
            }
        }
    }

    public class WorkstationGcOnly
    {
        [Benchmark]
        public void Benchmark()
        {
            if (GCSettings.IsServerGC)
            {
                throw new InvalidOperationException("Did not disable GC Server mode");
            }
        }
    }

    public class ConcurrentModeEnabled
    {
        [Benchmark]
        public void Benchmark()
        {
            if (GCSettings.LatencyMode == GCLatencyMode.Batch)
            {
                throw new InvalidOperationException("Did not enable Concurrent GC mode");
            }
        }
    }

    public class ConcurrentModeDisabled
    {
        [Benchmark]
        public void Benchmark()
        {
            if (GCSettings.LatencyMode != GCLatencyMode.Batch)
            {
                throw new InvalidOperationException("Did not disable Concurrent GC mode");
            }
        }
    }

    public class AvoidForcingGarbageCollection
    {
        int initialCollectionCountGen1;
        int initialCollectionCountGen2;

        [Setup]
        public void Setup()
        {
            initialCollectionCountGen1 = GC.CollectionCount(1);
            initialCollectionCountGen2 = GC.CollectionCount(2);
        }

        [Benchmark]
        public void Benchmark()
        {
            if (initialCollectionCountGen1 != GC.CollectionCount(1)
                || initialCollectionCountGen2 != GC.CollectionCount(2))
            {
                throw new InvalidOperationException("Did not disable GC Force");
            }
        }
    }

}