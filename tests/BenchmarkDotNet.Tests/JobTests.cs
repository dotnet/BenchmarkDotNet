using System;
using System.Linq;
using BenchmarkDotNet.Characteristics;
using BenchmarkDotNet.Engines;
using BenchmarkDotNet.Environments;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Toolchains.CsProj;
using Xunit;

namespace BenchmarkDotNet.IntegrationTests
{
    [Trait("Category", "JobTests")]
    public static class JobTests
    {
        private static void AssertProperties(CharacteristicObject obj, string properties) =>
            Assert.Equal(CharacteristicObject.IdCharacteristic.ResolveValueCore(obj, null), properties);

        [Fact]
        public static void Test01Create()
        {
            var j = new Job("CustomId");
            Assert.False(j.Frozen);
            Assert.False(j.Env.Frozen);
            Assert.False(j.Run.Frozen);
            Assert.False(j.Env.Gc.AllowVeryLargeObjects);
            Assert.Equal(Platform.AnyCpu, j.Env.Platform);
            Assert.Equal(RunStrategy.Throughput, j.Run.RunStrategy); // set by default
            Assert.Equal("CustomId", j.Id);
            Assert.Equal("CustomId", j.DisplayInfo);
            Assert.Equal("CustomId", j.ResolvedId);
            Assert.Equal(j.ResolvedId, j.FolderInfo);
            Assert.Equal("CustomId", j.Env.Id);

            // freeze
            var old = j;
            j = j.Freeze();
            Assert.Same(old, j);
            j = j.Freeze();
            Assert.Same(old, j);
            Assert.True(j.Frozen);
            Assert.True(j.Env.Frozen);
            Assert.True(j.Run.Frozen);
            Assert.False(j.Env.Gc.AllowVeryLargeObjects);
            Assert.Equal(Platform.AnyCpu, j.Env.Platform);
            Assert.Equal(RunStrategy.Throughput, j.Run.RunStrategy); // set by default
            Assert.Equal("CustomId", j.Id);
            Assert.Equal("CustomId", j.DisplayInfo);
            Assert.Equal("CustomId", j.ResolvedId);
            Assert.Equal(j.ResolvedId, j.FolderInfo);
            Assert.Equal("CustomId", j.Env.Id);

            // unfreeze
            old = j;
            j = j.UnfreezeCopy();
            Assert.NotSame(old, j);
            Assert.False(j.Frozen);
            Assert.False(j.Env.Frozen);
            Assert.False(j.Run.Frozen);
            Assert.False(j.Env.Gc.AllowVeryLargeObjects);
            Assert.Equal(Platform.AnyCpu, j.Env.Platform);
            Assert.Equal(RunStrategy.Throughput, j.Run.RunStrategy); // set by default
            Assert.Equal("Default", j.Id); // id reset
            Assert.True(j.DisplayInfo == "DefaultJob", "DisplayInfo = " + j.DisplayInfo);
            Assert.True(j.ResolvedId == "DefaultJob", "ResolvedId = " + j.ResolvedId);
            Assert.Equal(j.ResolvedId, j.FolderInfo);
            Assert.Equal("Default", j.Env.Id);

            // new job
            j = new Job(j.Freeze());
            Assert.False(j.Frozen);
            Assert.False(j.Env.Frozen);
            Assert.False(j.Run.Frozen);
            Assert.False(j.Env.Gc.AllowVeryLargeObjects);
            Assert.Equal(Platform.AnyCpu, j.Env.Platform);
            Assert.Equal(RunStrategy.Throughput, j.Run.RunStrategy); // set by default
            Assert.Equal("Default", j.Id); // id reset
            Assert.True(j.DisplayInfo == "DefaultJob", "DisplayInfo = " + j.DisplayInfo);
            Assert.True(j.ResolvedId == "DefaultJob", "ResolvedId = " + j.ResolvedId);
            Assert.Equal(j.ResolvedId, j.FolderInfo);
            Assert.Equal("Default", j.Env.Id);
        }

        [Fact]
        public static void Test02Modify()
        {
            var j = new Job("SomeId");

            Assert.Equal("SomeId", j.Id);
            Assert.Equal(Platform.AnyCpu, j.Env.Platform);
            Assert.Equal(0, j.Run.LaunchCount);

            Assert.False(j.HasValue(EnvMode.PlatformCharacteristic));
            Assert.False(j.Env.HasValue(EnvMode.PlatformCharacteristic));
            Assert.False(j.HasValue(RunMode.LaunchCountCharacteristic));
            Assert.False(j.Run.HasValue(RunMode.LaunchCountCharacteristic));

            AssertProperties(j, "Default");
            AssertProperties(j.Env, "Default");

            // 1. change values
            j.Env.Platform = Platform.X64;
            j.Run.LaunchCount = 1;

            Assert.Equal("SomeId", j.Id);
            Assert.Equal(Platform.X64, j.Env.Platform);
            Assert.Equal(1, j.Run.LaunchCount);

            Assert.True(j.HasValue(EnvMode.PlatformCharacteristic));
            Assert.True(j.Env.HasValue(EnvMode.PlatformCharacteristic));
            Assert.True(j.HasValue(RunMode.LaunchCountCharacteristic));
            Assert.True(j.Run.HasValue(RunMode.LaunchCountCharacteristic));

            AssertProperties(j, "Platform=X64, LaunchCount=1");
            AssertProperties(j.Env, "Platform=X64");
            AssertProperties(j.Run, "LaunchCount=1");

            // 2. reset Env mode (hack via Characteristic setting)
            var oldEnv = j.Env;
            Job.EnvCharacteristic[j] = new EnvMode();

            Assert.Equal("SomeId", j.Id);
            Assert.Equal(Platform.AnyCpu, j.Env.Platform);
            Assert.Equal(1, j.Run.LaunchCount);

            Assert.False(j.HasValue(EnvMode.PlatformCharacteristic));
            Assert.False(j.Env.HasValue(EnvMode.PlatformCharacteristic));
            Assert.True(j.HasValue(RunMode.LaunchCountCharacteristic));
            Assert.True(j.Run.HasValue(RunMode.LaunchCountCharacteristic));

            AssertProperties(j, "LaunchCount=1");
            AssertProperties(j.Env, "Default");
            AssertProperties(j.Run, "LaunchCount=1");

            // 2.1 proof that oldEnv was the same
            Assert.Equal("SomeId", j.Id);
            Assert.Equal(Platform.X64, oldEnv.Platform);
            Assert.True(oldEnv.HasValue(EnvMode.PlatformCharacteristic));
            Assert.Equal("Platform=X64", oldEnv.Id);

            // 3. update Env mode (hack via Characteristic setting)
            Job.EnvCharacteristic[j] = new EnvMode()
            {
                Platform = Platform.X86
            };

            Assert.Equal("SomeId", j.Id);
            Assert.Equal(Platform.X86, j.Env.Platform);
            Assert.Equal(1, j.Run.LaunchCount);

            Assert.True(j.HasValue(EnvMode.PlatformCharacteristic));
            Assert.True(j.Env.HasValue(EnvMode.PlatformCharacteristic));
            Assert.True(j.HasValue(RunMode.LaunchCountCharacteristic));
            Assert.True(j.Run.HasValue(RunMode.LaunchCountCharacteristic));

            AssertProperties(j, "Platform=X86, LaunchCount=1");
            AssertProperties(j.Env, "Platform=X86");
            AssertProperties(j.Run, "LaunchCount=1");

            // 4. Freeze-unfreeze:
            j = j.Freeze().UnfreezeCopy();

            Assert.Equal("Platform=X86, LaunchCount=1", j.Id);
            Assert.Equal(Platform.X86, j.Env.Platform);
            Assert.Equal(1, j.Run.LaunchCount);

            Assert.True(j.HasValue(EnvMode.PlatformCharacteristic));
            Assert.True(j.Env.HasValue(EnvMode.PlatformCharacteristic));
            Assert.True(j.HasValue(RunMode.LaunchCountCharacteristic));
            Assert.True(j.Run.HasValue(RunMode.LaunchCountCharacteristic));

            AssertProperties(j, "Platform=X86, LaunchCount=1");
            AssertProperties(j.Env, "Platform=X86");
            AssertProperties(j.Run, "LaunchCount=1");

            // 5. Test .With extensions
            j = j.Freeze()
                .WithId("NewId");
            Assert.Equal("NewId", j.Id); // id set

            j = j.Freeze()
                .With(Platform.X64)
                .WithLaunchCount(2);

            Assert.Equal("NewId", j.Id); // id not lost
            Assert.Equal("NewId(Platform=X64, LaunchCount=2)", j.DisplayInfo);
            Assert.Equal(Platform.X64, j.Env.Platform);
            Assert.Equal(2, j.Run.LaunchCount);

            Assert.True(j.HasValue(EnvMode.PlatformCharacteristic));
            Assert.True(j.Env.HasValue(EnvMode.PlatformCharacteristic));
            Assert.True(j.HasValue(RunMode.LaunchCountCharacteristic));
            Assert.True(j.Run.HasValue(RunMode.LaunchCountCharacteristic));

            AssertProperties(j, "Platform=X64, LaunchCount=2");
            AssertProperties(j.Env, "Platform=X64");
            AssertProperties(j.Run, "LaunchCount=2");
        }

        [Fact]
        public static void Test03IdDoesNotFlow()
        {
            var j = new Job(EnvMode.LegacyJitX64, RunMode.Long); // id will not flow, new Job
            Assert.False(j.HasValue(CharacteristicObject.IdCharacteristic));
            Assert.False(j.Env.HasValue(CharacteristicObject.IdCharacteristic));

            Job.EnvCharacteristic[j] = EnvMode.LegacyJitX86.UnfreezeCopy(); // id will not flow
            Assert.False(j.HasValue(CharacteristicObject.IdCharacteristic));
            Assert.False(j.Env.HasValue(CharacteristicObject.IdCharacteristic));

            var c = new CharacteristicSet(EnvMode.LegacyJitX64, RunMode.Long); // id will not flow, new CharacteristicSet
            Assert.False(c.HasValue(CharacteristicObject.IdCharacteristic));

            Job.EnvCharacteristic[c] = EnvMode.LegacyJitX86.UnfreezeCopy(); // id will not flow
            Assert.False(c.HasValue(CharacteristicObject.IdCharacteristic));

            CharacteristicObject.IdCharacteristic[c] = "MyId"; // id set explicitly
            Assert.Equal("MyId", c.Id);

            j = new Job("MyId", EnvMode.LegacyJitX64, RunMode.Long); // id set explicitly
            Assert.Equal("MyId", j.Id);
            Assert.Equal("MyId", j.Env.Id);

            Job.EnvCharacteristic[j] = EnvMode.LegacyJitX86.UnfreezeCopy(); // id will not flow
            Assert.Equal("MyId", j.Id);
            Assert.Equal("MyId", j.Env.Id);

            j = j.With(Jit.RyuJit);  // custom id will flow
            Assert.Equal("MyId", j.Id);
        }

        [Fact]
        public static void CustomJobIdIsPreserved()
        {
            const string id = "theId";

            var jobWithId = Job.Default.WithId(id);

            Assert.Equal(id, jobWithId.Id);

            var shouldHaveSameId = jobWithId.AsBaseline();

            Assert.Equal(id, shouldHaveSameId.Id);
        }

        [Fact]
        public static void PredefinedJobIdIsNotPreserved()
        {
            var predefinedJob = Job.Default;

            var customJob = predefinedJob.AsBaseline();

            Assert.NotEqual(predefinedJob.Id, customJob.Id);
        }

        [Fact]
        public static void Test04Apply()
        {
            var j = new Job()
            {
                Run = { TargetCount = 1 }
            };

            AssertProperties(j, "TargetCount=1");

            j.Apply(
                new Job
                {
                    Env = { Platform = Platform.X64 },
                    Run = { TargetCount = 2 }
                });
            AssertProperties(j, "Platform=X64, TargetCount=2");

            // filter by properties
            j.Env.Apply(
                new Job()
                    .With(Jit.RyuJit)
                    .WithGcAllowVeryLargeObjects(true)
                    .WithTargetCount(3)
                    .WithLaunchCount(22));
            AssertProperties(j, "Jit=RyuJit, Platform=X64, AllowVeryLargeObjects=True, TargetCount=2");

            // apply subnode
            j.Apply(
                new GcMode()
                {
                    AllowVeryLargeObjects = false
                });
            AssertProperties(j, "Jit=RyuJit, Platform=X64, AllowVeryLargeObjects=False, TargetCount=2");

            // Apply empty
            j.Apply(Job.Default); // does nothing
            AssertProperties(j, "Jit=RyuJit, Platform=X64, AllowVeryLargeObjects=False, TargetCount=2");
        }

        [Fact]
        public static void Test05ApplyCharacteristicSet()
        {
            var set1 = new CharacteristicSet();
            var set2 = new CharacteristicSet();

            set1
                .Apply(
                    new EnvMode
                    {
                        Platform = Platform.X64
                    })
                .Apply(
                    new Job
                    {
                        Run =
                        {
                            LaunchCount = 2
                        },
                        Env =
                        {
                            Platform = Platform.X86
                        }
                    });
            AssertProperties(set1, "LaunchCount=2, Platform=X86");
            Assert.Equal(Platform.X86, Job.EnvCharacteristic[set1].Platform);
            Assert.True(set1.HasValue(Job.EnvCharacteristic));
            Assert.Equal(Platform.X86, EnvMode.PlatformCharacteristic[set1]);

            set2.Apply(EnvMode.RyuJitX64).Apply(new GcMode { Concurrent = true });
            Assert.Null(Job.RunCharacteristic[set2]);
            Assert.False(set2.HasValue(Job.RunCharacteristic));
            AssertProperties(set2, "Concurrent=True, Jit=RyuJit, Platform=X64");

            var temp = set1.UnfreezeCopy();
            set1.Apply(set2);
            set2.Apply(temp);
            AssertProperties(set1, "Concurrent=True, Jit=RyuJit, LaunchCount=2, Platform=X64");
            AssertProperties(set2, "Concurrent=True, Jit=RyuJit, LaunchCount=2, Platform=X86");

            var j = new Job();
            AssertProperties(j, "Default");

            j.Env.Gc.Apply(set1);
            AssertProperties(j, "Concurrent=True");

            j.Run.Apply(set1);
            AssertProperties(j, "Concurrent=True, LaunchCount=2");

            j.Env.Apply(set1);
            AssertProperties(j, "Jit=RyuJit, Platform=X64, Concurrent=True, LaunchCount=2");

            j.Apply(set1);
            AssertProperties(j, "Jit=RyuJit, Platform=X64, Concurrent=True, LaunchCount=2");
        }

        [Fact]
        public static void Test06CharacteristicHacks()
        {
            var j = new Job();
            Assert.Equal(0, j.Run.TargetCount);

            RunMode.TargetCountCharacteristic[j] = 123;
            Assert.Equal(123, j.Run.TargetCount);

            var old = j.Run;
            Job.RunCharacteristic[j] = new RunMode();
            Assert.Equal(0, j.Run.TargetCount);

            Job.RunCharacteristic[j] = old;
            old.TargetCount = 234;
            Assert.Equal(234, j.Run.TargetCount);
            Assert.Equal(234, RunMode.TargetCountCharacteristic[j]);

            Characteristic a = Job.RunCharacteristic;
            // will not throw:
            a[j] = new RunMode();
            Assert.Throws<ArgumentNullException>(() => a[j] = null); // nulls for job nodes are not allowed;
            Assert.Throws<ArgumentNullException>(() => a[j] = Characteristic.EmptyValue);
            Assert.Throws<ArgumentException>(() => a[j] = new EnvMode()); // not assignable;
            Assert.Throws<ArgumentException>(() => a[j] = new CharacteristicSet()); // not assignable;
            Assert.Throws<ArgumentException>(() => a[j] = 123); // not assignable;

            a = InfrastructureMode.ToolchainCharacteristic;
            // will not throw:
            a[j] = CsProjClassicNetToolchain.Net46;
            a[j] = null;
            a[j] = Characteristic.EmptyValue;
            Assert.Throws<ArgumentException>(() => a[j] = new EnvMode()); // not assignable;
            Assert.Throws<ArgumentException>(() => a[j] = new CharacteristicSet()); // not assignable;
            Assert.Throws<ArgumentException>(() => a[j] = 123); // not assignable;
        }

        [Fact]
        public static void Test07GetCharacteristics()
        {
            // Update expected values only if Job properties were changed.
            // Otherwise, there's a bug.
            var a = CharacteristicHelper
                .GetThisTypeCharacteristics(typeof(Job))
                .Select(c => c.Id);
            Assert.Equal("Id;Accuracy;Env;Infrastructure;Meta;Run", string.Join(";", a));
            a = CharacteristicHelper
                .GetAllCharacteristics(typeof(Job))
                .Select(c => c.Id);
            Assert.Equal(string.Join(";", a), "Id;Accuracy;AnalyzeLaunchVariance;EvaluateOverhead;" +
                "MaxAbsoluteError;MaxRelativeError;MinInvokeCount;MinIterationTime;RemoveOutliers;Env;Affinity;" +
                "Jit;Platform;Runtime;Gc;AllowVeryLargeObjects;Concurrent;CpuGroups;Force;HeapAffinitizeMask;HeapCount;NoAffinitize;" +
                "RetainVm;Server;Infrastructure;Arguments;BuildConfiguration;Clock;EngineFactory;EnvironmentVariables;Toolchain;Meta;IsBaseline;Run;InvocationCount;IterationTime;" +
                "LaunchCount;RunStrategy;TargetCount;UnrollFactor;WarmupCount");
        }
    }
}