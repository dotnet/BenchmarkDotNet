using System;
using System.Runtime;
using Xunit;
using Xunit.Abstractions;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Jobs;

namespace BenchmarkDotNet.IntegrationTests
{
    public class GcModeSettingsTests : BenchmarkTestExecutor
    {
        public GcModeSettingsTests(ITestOutputHelper outputHelper) : base(outputHelper) { }

        [Fact(Skip = "It fails on appveyor")]
        public void CanEnableServerGcMode()
        {
            var config = ManualConfig.CreateEmpty()
                                     .With(Job.Dry.With(new GcMode { Server = true }));

            CanExecute<ServerModeEnabled>(config);
        }

        [Fact]
        public void CanDisableServerGcMode()
        {
            var config = ManualConfig.CreateEmpty()
                                     .With(Job.Dry.With(new GcMode { Server = false }));

            CanExecute<WorkstationGcOnly>(config);
        }

        [Fact]
        public void CanEnableConcurrentGcMode()
        {
            var config = ManualConfig.CreateEmpty()
                                     .With(Job.Dry.With(new GcMode { Concurrent = true }));

            CanExecute<ConcurrentModeEnabled>(config);
        }

        [Fact]
        public void CanDisableConcurrentGcMode()
        {
            var config = ManualConfig.CreateEmpty()
                                     .With(Job.Dry.With(new GcMode { Concurrent = false }));

            CanExecute<ConcurrentModeDisabled>(config);
        }

        [Fact]
        public void CanAvoidForcingGarbageCollections()
        {
            var config = ManualConfig.CreateEmpty()
                                     .With(Job.Dry.With(new GcMode { Force = false }));
            
            CanExecute<AvoidForcingGarbageCollection>(config);
        }

#if CLASSIC // not supported by project.json so far
        [Fact]
        public void CanAllowToCreateVeryLargeObjectsFor64Bit()
        {
            var config = ManualConfig.CreateEmpty()
                                     .With(Job.Dry.With(Platform.X64).With(new GcMode { AllowVeryLargeObjects = true }));

            CanExecute<CreateVeryLargeObjects>(config);
        }
#endif
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

    public class CreateVeryLargeObjects
    {
        [Benchmark]
        public long[] Benchmark()
        {
            return new long[2147483648 / sizeof(long)]; // 2GB is the default limit
        }
    }

}