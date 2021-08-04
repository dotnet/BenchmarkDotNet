using System;
using System.Runtime;
using Xunit;
using Xunit.Abstractions;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Jobs;
#if NETFRAMEWORK
using BenchmarkDotNet.Environments;
#endif

namespace BenchmarkDotNet.IntegrationTests
{
    public class GcModeTests : BenchmarkTestExecutor
    {
        public GcModeTests(ITestOutputHelper outputHelper) : base(outputHelper) { }

        private IConfig CreateConfig(GcMode gc) => ManualConfig.CreateEmpty().AddJob(new Job(Job.Dry, gc));

        [Fact]
        public void HostProcessSettingsAreCopiedByDefault()
        {
            var config = CreateConfig(GcMode.Default);

            if (GCSettings.IsServerGC)
                CanExecute<ServerModeEnabled>(config);
            else
                CanExecute<WorkstationGcOnly>(config);
        }

        [Fact]
        public void CanEnableServerGcMode()
        {
            var config = CreateConfig(new GcMode { Server = true });
            CanExecute<ServerModeEnabled>(config);
        }

        [Fact]
        public void CanDisableServerGcMode()
        {
            var config = CreateConfig(new GcMode { Server = false });
            CanExecute<WorkstationGcOnly>(config);
        }

        [Fact]
        public void CanEnableConcurrentGcMode()
        {
            var config = CreateConfig(new GcMode { Concurrent = true });
            CanExecute<ConcurrentModeEnabled>(config);
        }

        [Fact]
        public void CanDisableConcurrentGcMode()
        {
            var config = CreateConfig(new GcMode { Concurrent = false });
            CanExecute<ConcurrentModeDisabled>(config);
        }

        [Fact]
        public void CanAvoidForcingGarbageCollections()
        {
            var config = CreateConfig(new GcMode { Force = false });
            CanExecute<AvoidForcingGarbageCollection>(config);
        }

#if NETFRAMEWORK // not supported by project.json so far
        [Fact]
        public void CanAllowToCreateVeryLargeObjectsFor64Bit()
        {
            var config = ManualConfig.CreateEmpty().AddJob(
                new Job(Job.Dry)
                {
                    Environment =
                    {
                        Platform = Platform.X64,
                        Gc =
                        {
                            AllowVeryLargeObjects = true
                        }
                    }
                });

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
        private int initialCollectionCountGen1;
        private int initialCollectionCountGen2;

        [GlobalSetup]
        public void GlobalSetup()
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