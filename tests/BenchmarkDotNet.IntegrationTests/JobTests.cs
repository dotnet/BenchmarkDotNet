using System;
using System.Linq;
using BenchmarkDotNet.Characteristics;
using BenchmarkDotNet.Engines;
using BenchmarkDotNet.Environments;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Toolchains.CsProj;
using Xunit;
using static Xunit.Assert;

namespace BenchmarkDotNet.IntegrationTests
{
    [Trait("Category", "JobTests")]
    public static class JobTests
    {
        private static void AssertProperties(CharacteristicObject obj, string properties) =>
            Equal(CharacteristicObject.IdCharacteristic.ResolveValueCore(obj, null), properties);

        [Fact]
        public static void Test01Create()
        {
            var j = new Job("CustomId");
            Equal(j.Frozen, false);
            Equal(j.Env.Frozen, false);
            Equal(j.Run.Frozen, false);
            Equal(j.Env.Gc.AllowVeryLargeObjects, false);
            Equal(j.Env.Platform, Platform.AnyCpu);
            Equal(j.Run.RunStrategy, RunStrategy.Throughput); // set by default
            Equal(j.Id, "CustomId");
            Equal(j.DisplayInfo, "CustomId");
            Equal(j.ResolvedId, "CustomId");
            Equal(j.ResolvedId, j.FolderInfo);
            Equal(j.Env.Id, "CustomId");

            // freeze
            var old = j;
            j = j.Freeze();
            Same(old, j);
            j = j.Freeze();
            Same(old, j);
            Equal(j.Frozen, true);
            Equal(j.Env.Frozen, true);
            Equal(j.Run.Frozen, true);
            Equal(j.Env.Gc.AllowVeryLargeObjects, false);
            Equal(j.Env.Platform, Platform.AnyCpu);
            Equal(j.Run.RunStrategy, RunStrategy.Throughput); // set by default
            Equal(j.Id, "CustomId");
            Equal(j.DisplayInfo, "CustomId");
            Equal(j.ResolvedId, "CustomId");
            Equal(j.ResolvedId, j.FolderInfo);
            Equal(j.Env.Id, "CustomId");

            // unfreeze
            old = j;
            j = j.UnfreezeCopy();
            NotSame(old, j);
            Equal(j.Frozen, false);
            Equal(j.Env.Frozen, false);
            Equal(j.Run.Frozen, false);
            Equal(j.Env.Gc.AllowVeryLargeObjects, false);
            Equal(j.Env.Platform, Platform.AnyCpu);
            Equal(j.Run.RunStrategy, RunStrategy.Throughput); // set by default
            Equal(j.Id, "Default"); // id reset
            True(j.DisplayInfo == "DefaultJob", "DisplayInfo = " + j.DisplayInfo);
            True(j.ResolvedId == "DefaultJob", "ResolvedId = " + j.ResolvedId);
            Equal(j.ResolvedId, j.FolderInfo);
            Equal(j.Env.Id, "Default");

            // new job
            j = new Job(j.Freeze());
            Equal(j.Frozen, false);
            Equal(j.Env.Frozen, false);
            Equal(j.Run.Frozen, false);
            Equal(j.Env.Gc.AllowVeryLargeObjects, false);
            Equal(j.Env.Platform, Platform.AnyCpu);
            Equal(j.Run.RunStrategy, RunStrategy.Throughput); // set by default
            Equal(j.Id, "Default"); // id reset
            True(j.DisplayInfo == "DefaultJob", "DisplayInfo = " + j.DisplayInfo);
            True(j.ResolvedId == "DefaultJob", "ResolvedId = " + j.ResolvedId);
            Equal(j.ResolvedId, j.FolderInfo);
            Equal(j.Env.Id, "Default");
        }

        [Fact]
        public static void Test02Modify()
        {
            var j = new Job("SomeId");

            Equal(j.Id, "SomeId");
            Equal(j.Env.Platform, Platform.AnyCpu);
            Equal(j.Run.LaunchCount, 0);

            False(j.HasValue(EnvMode.PlatformCharacteristic));
            False(j.Env.HasValue(EnvMode.PlatformCharacteristic));
            False(j.HasValue(RunMode.LaunchCountCharacteristic));
            False(j.Run.HasValue(RunMode.LaunchCountCharacteristic));

            AssertProperties(j, "Default");
            AssertProperties(j.Env, "Default");

            // 1. change values
            j.Env.Platform = Platform.X64;
            j.Run.LaunchCount = 1;

            Equal(j.Id, "SomeId");
            Equal(j.Env.Platform, Platform.X64);
            Equal(j.Run.LaunchCount, 1);

            True(j.HasValue(EnvMode.PlatformCharacteristic));
            True(j.Env.HasValue(EnvMode.PlatformCharacteristic));
            True(j.HasValue(RunMode.LaunchCountCharacteristic));
            True(j.Run.HasValue(RunMode.LaunchCountCharacteristic));

            AssertProperties(j, "Platform=X64, LaunchCount=1");
            AssertProperties(j.Env, "Platform=X64");
            AssertProperties(j.Run, "LaunchCount=1");

            // 2. reset Env mode (hack via Characteristic setting)
            var oldEnv = j.Env;
            Job.EnvCharacteristic[j] = new EnvMode();

            Equal(j.Id, "SomeId");
            Equal(j.Env.Platform, Platform.AnyCpu);
            Equal(j.Run.LaunchCount, 1);

            False(j.HasValue(EnvMode.PlatformCharacteristic));
            False(j.Env.HasValue(EnvMode.PlatformCharacteristic));
            True(j.HasValue(RunMode.LaunchCountCharacteristic));
            True(j.Run.HasValue(RunMode.LaunchCountCharacteristic));

            AssertProperties(j, "LaunchCount=1");
            AssertProperties(j.Env, "Default");
            AssertProperties(j.Run, "LaunchCount=1");

            // 2.1 proof that oldEnv was the same
            Equal(j.Id, "SomeId");
            Equal(oldEnv.Platform, Platform.X64);
            True(oldEnv.HasValue(EnvMode.PlatformCharacteristic));
            Equal(oldEnv.Id, "Platform=X64");

            // 3. update Env mode (hack via Characteristic setting)
            Job.EnvCharacteristic[j] = new EnvMode()
            {
                Platform = Platform.X86
            };

            Equal(j.Id, "SomeId");
            Equal(j.Env.Platform, Platform.X86);
            Equal(j.Run.LaunchCount, 1);

            True(j.HasValue(EnvMode.PlatformCharacteristic));
            True(j.Env.HasValue(EnvMode.PlatformCharacteristic));
            True(j.HasValue(RunMode.LaunchCountCharacteristic));
            True(j.Run.HasValue(RunMode.LaunchCountCharacteristic));

            AssertProperties(j, "Platform=X86, LaunchCount=1");
            AssertProperties(j.Env, "Platform=X86");
            AssertProperties(j.Run, "LaunchCount=1");

            // 4. Freeze-unfreeze:
            j = j.Freeze().UnfreezeCopy();

            Equal(j.Id, "Platform=X86, LaunchCount=1");
            Equal(j.Env.Platform, Platform.X86);
            Equal(j.Run.LaunchCount, 1);

            True(j.HasValue(EnvMode.PlatformCharacteristic));
            True(j.Env.HasValue(EnvMode.PlatformCharacteristic));
            True(j.HasValue(RunMode.LaunchCountCharacteristic));
            True(j.Run.HasValue(RunMode.LaunchCountCharacteristic));

            AssertProperties(j, "Platform=X86, LaunchCount=1");
            AssertProperties(j.Env, "Platform=X86");
            AssertProperties(j.Run, "LaunchCount=1");

            // 5. Test .With extensions
            j = j.Freeze()
                .WithId("NewId");
            Equal(j.Id, "NewId"); // id set

            j = j.Freeze()
                .With(Platform.X64)
                .WithLaunchCount(2);

            Equal(j.Id, "Platform=X64, LaunchCount=2"); // id lost
            Equal(j.Env.Platform, Platform.X64);
            Equal(j.Run.LaunchCount, 2);

            True(j.HasValue(EnvMode.PlatformCharacteristic));
            True(j.Env.HasValue(EnvMode.PlatformCharacteristic));
            True(j.HasValue(RunMode.LaunchCountCharacteristic));
            True(j.Run.HasValue(RunMode.LaunchCountCharacteristic));

            AssertProperties(j, "Platform=X64, LaunchCount=2");
            AssertProperties(j.Env, "Platform=X64");
            AssertProperties(j.Run, "LaunchCount=2");

        }


        [Fact]
        public static void Test03IdDoesNotFlow()
        {
            var j = new Job(EnvMode.LegacyJitX64, RunMode.Long); // id will not flow, new Job
            False(j.HasValue(CharacteristicObject.IdCharacteristic));
            False(j.Env.HasValue(CharacteristicObject.IdCharacteristic));

            Job.EnvCharacteristic[j] = EnvMode.LegacyJitX86.UnfreezeCopy(); // id will not flow
            False(j.HasValue(CharacteristicObject.IdCharacteristic));
            False(j.Env.HasValue(CharacteristicObject.IdCharacteristic));

            var c = new CharacteristicSet(EnvMode.LegacyJitX64, RunMode.Long); // id will not flow, new CharacteristicSet
            False(c.HasValue(CharacteristicObject.IdCharacteristic));

            Job.EnvCharacteristic[c] = EnvMode.LegacyJitX86.UnfreezeCopy(); // id will not flow
            False(c.HasValue(CharacteristicObject.IdCharacteristic));

            CharacteristicObject.IdCharacteristic[c] = "MyId"; // id set explicitly
            Equal(c.Id, "MyId");

            j = new Job("MyId", EnvMode.LegacyJitX64, RunMode.Long); // id set explicitly
            Equal(j.Id, "MyId");
            Equal(j.Env.Id, "MyId");

            Job.EnvCharacteristic[j] = EnvMode.LegacyJitX86.UnfreezeCopy(); // id will not flow
            Equal(j.Id, "MyId");
            Equal(j.Env.Id, "MyId");

            j = j.With(Jit.RyuJit);  // id will not flow
            False(j.HasValue(CharacteristicObject.IdCharacteristic));
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
            Equal(Job.EnvCharacteristic[set1].Platform, Platform.X86);
            Equal(set1.HasValue(Job.EnvCharacteristic), true);
            Equal(EnvMode.PlatformCharacteristic[set1], Platform.X86);

            set2.Apply(EnvMode.RyuJitX64).Apply(new GcMode { Concurrent = true });
            Equal(Job.RunCharacteristic[set2], null);
            Equal(set2.HasValue(Job.RunCharacteristic), false);
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
            Equal(j.Run.TargetCount, 0);

            RunMode.TargetCountCharacteristic[j] = 123;
            Equal(j.Run.TargetCount, 123);

            var old = j.Run;
            Job.RunCharacteristic[j] = new RunMode();
            Equal(j.Run.TargetCount, 0);

            Job.RunCharacteristic[j] = old;
            old.TargetCount = 234;
            Equal(j.Run.TargetCount, 234);
            Equal(RunMode.TargetCountCharacteristic[j], 234);

            Characteristic a = Job.RunCharacteristic;
            // will not throw:
            a[j] = new RunMode();
            Throws<ArgumentNullException>(() => a[j] = null); // nulls for job nodes are not allowed;
            Throws<ArgumentNullException>(() => a[j] = Characteristic.EmptyValue);
            Throws<ArgumentException>(() => a[j] = new EnvMode()); // not assignable;
            Throws<ArgumentException>(() => a[j] = new CharacteristicSet()); // not assignable;
            Throws<ArgumentException>(() => a[j] = 123); // not assignable;

            a = InfrastructureMode.ToolchainCharacteristic;
            // will not throw:
            a[j] = CsProjClassicNetToolchain.Net46;
            a[j] = null;
            a[j] = Characteristic.EmptyValue;
            Throws<ArgumentException>(() => a[j] = new EnvMode()); // not assignable;
            Throws<ArgumentException>(() => a[j] = new CharacteristicSet()); // not assignable;
            Throws<ArgumentException>(() => a[j] = 123); // not assignable;
        }

        [Fact]
        public static void Test07GetCharacteristics()
        {
            // Update expected values only if Job properties were changed.
            // Otherwise, there's a bug.
            var a = CharacteristicHelper
                .GetThisTypeCharacteristics(typeof(Job))
                .Select(c => c.Id);
            Equal(string.Join(";", a), "Id;Accuracy;Env;Infrastructure;Run");
            a = CharacteristicHelper
                .GetAllCharacteristics(typeof(Job))
                .Select(c => c.Id);
            Equal(string.Join(";", a), "Id;Accuracy;AnalyzeLaunchVariance;EvaluateOverhead;" +
                "MaxAbsoluteError;MaxRelativeError;MinInvokeCount;MinIterationTime;RemoveOutliers;Env;Affinity;" +
                "Jit;Platform;Runtime;Gc;AllowVeryLargeObjects;Concurrent;CpuGroups;Force;" +
                "RetainVm;Server;Infrastructure;Arguments;BuildConfiguration;Clock;EngineFactory;EnvironmentVariables;Toolchain;Run;InvocationCount;IterationTime;" +
                "LaunchCount;RunStrategy;TargetCount;UnrollFactor;WarmupCount");
        }
    }
}