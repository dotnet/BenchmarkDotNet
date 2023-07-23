using System;
using System.Linq;
using System.Reflection;
using BenchmarkDotNet.Characteristics;
using BenchmarkDotNet.Engines;
using BenchmarkDotNet.Environments;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Toolchains.CsProj;
using Xunit;

namespace BenchmarkDotNet.Tests.Configs
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
            Assert.False(j.Environment.Frozen);
            Assert.False(j.Run.Frozen);
            Assert.False(j.Environment.Gc.AllowVeryLargeObjects);
            Assert.Equal(Platform.AnyCpu, j.Environment.Platform);
            Assert.Equal(RunStrategy.Throughput, j.Run.RunStrategy); // set by default
            Assert.Equal("CustomId", j.Id);
            Assert.Equal("CustomId", j.DisplayInfo);
            Assert.Equal("CustomId", j.ResolvedId);
            Assert.Equal(j.ResolvedId, j.FolderInfo);
            Assert.Equal("CustomId", j.Environment.Id);

            // freeze
            var old = j;
            j = j.Freeze();
            Assert.Same(old, j);
            j = j.Freeze();
            Assert.Same(old, j);
            Assert.True(j.Frozen);
            Assert.True(j.Environment.Frozen);
            Assert.True(j.Run.Frozen);
            Assert.False(j.Environment.Gc.AllowVeryLargeObjects);
            Assert.Equal(Platform.AnyCpu, j.Environment.Platform);
            Assert.Equal(RunStrategy.Throughput, j.Run.RunStrategy); // set by default
            Assert.Equal("CustomId", j.Id);
            Assert.Equal("CustomId", j.DisplayInfo);
            Assert.Equal("CustomId", j.ResolvedId);
            Assert.Equal(j.ResolvedId, j.FolderInfo);
            Assert.Equal("CustomId", j.Environment.Id);

            // unfreeze
            old = j;
            j = j.UnfreezeCopy();
            Assert.NotSame(old, j);
            Assert.False(j.Frozen);
            Assert.False(j.Environment.Frozen);
            Assert.False(j.Run.Frozen);
            Assert.False(j.Environment.Gc.AllowVeryLargeObjects);
            Assert.Equal(Platform.AnyCpu, j.Environment.Platform);
            Assert.Equal(RunStrategy.Throughput, j.Run.RunStrategy); // set by default
            Assert.Equal("Default", j.Id); // id reset
            Assert.True(j.DisplayInfo == "DefaultJob", "DisplayInfo = " + j.DisplayInfo);
            Assert.True(j.ResolvedId == "DefaultJob", "ResolvedId = " + j.ResolvedId);
            Assert.Equal(j.ResolvedId, j.FolderInfo);
            Assert.Equal("Default", j.Environment.Id);

            // new job
            j = new Job(j.Freeze());
            Assert.False(j.Frozen);
            Assert.False(j.Environment.Frozen);
            Assert.False(j.Run.Frozen);
            Assert.False(j.Environment.Gc.AllowVeryLargeObjects);
            Assert.Equal(Platform.AnyCpu, j.Environment.Platform);
            Assert.Equal(RunStrategy.Throughput, j.Run.RunStrategy); // set by default
            Assert.Equal("Default", j.Id); // id reset
            Assert.True(j.DisplayInfo == "DefaultJob", "DisplayInfo = " + j.DisplayInfo);
            Assert.True(j.ResolvedId == "DefaultJob", "ResolvedId = " + j.ResolvedId);
            Assert.Equal(j.ResolvedId, j.FolderInfo);
            Assert.Equal("Default", j.Environment.Id);
        }

        [Fact]
        public static void Test02Modify()
        {
            var j = new Job("SomeId");

            Assert.Equal("SomeId", j.Id);
            Assert.Equal(Platform.AnyCpu, j.Environment.Platform);
            Assert.Equal(0, j.Run.LaunchCount);

            Assert.False(j.HasValue(EnvironmentMode.PlatformCharacteristic));
            Assert.False(j.Environment.HasValue(EnvironmentMode.PlatformCharacteristic));
            Assert.False(j.HasValue(RunMode.LaunchCountCharacteristic));
            Assert.False(j.Run.HasValue(RunMode.LaunchCountCharacteristic));

            AssertProperties(j, "Default");
            AssertProperties(j.Environment, "Default");

            // 1. change values
            j.Environment.Platform = Platform.X64;
            j.Run.LaunchCount = 1;

            Assert.Equal("SomeId", j.Id);
            Assert.Equal(Platform.X64, j.Environment.Platform);
            Assert.Equal(1, j.Run.LaunchCount);

            Assert.True(j.HasValue(EnvironmentMode.PlatformCharacteristic));
            Assert.True(j.Environment.HasValue(EnvironmentMode.PlatformCharacteristic));
            Assert.True(j.HasValue(RunMode.LaunchCountCharacteristic));
            Assert.True(j.Run.HasValue(RunMode.LaunchCountCharacteristic));

            AssertProperties(j, "Platform=X64, LaunchCount=1");
            AssertProperties(j.Environment, "Platform=X64");
            AssertProperties(j.Run, "LaunchCount=1");

            // 2. reset Env mode (hack via Characteristic setting)
            var oldEnv = j.Environment;
            Job.EnvironmentCharacteristic[j] = new EnvironmentMode();

            Assert.Equal("SomeId", j.Id);
            Assert.Equal(Platform.AnyCpu, j.Environment.Platform);
            Assert.Equal(1, j.Run.LaunchCount);

            Assert.False(j.HasValue(EnvironmentMode.PlatformCharacteristic));
            Assert.False(j.Environment.HasValue(EnvironmentMode.PlatformCharacteristic));
            Assert.True(j.HasValue(RunMode.LaunchCountCharacteristic));
            Assert.True(j.Run.HasValue(RunMode.LaunchCountCharacteristic));

            AssertProperties(j, "LaunchCount=1");
            AssertProperties(j.Environment, "Default");
            AssertProperties(j.Run, "LaunchCount=1");

            // 2.1 proof that oldEnv was the same
            Assert.Equal("SomeId", j.Id);
            Assert.Equal(Platform.X64, oldEnv.Platform);
            Assert.True(oldEnv.HasValue(EnvironmentMode.PlatformCharacteristic));
            Assert.Equal("Platform=X64", oldEnv.Id);

            // 3. update Env mode (hack via Characteristic setting)
            Job.EnvironmentCharacteristic[j] = new EnvironmentMode()
            {
                Platform = Platform.X86
            };

            Assert.Equal("SomeId", j.Id);
            Assert.Equal(Platform.X86, j.Environment.Platform);
            Assert.Equal(1, j.Run.LaunchCount);

            Assert.True(j.HasValue(EnvironmentMode.PlatformCharacteristic));
            Assert.True(j.Environment.HasValue(EnvironmentMode.PlatformCharacteristic));
            Assert.True(j.HasValue(RunMode.LaunchCountCharacteristic));
            Assert.True(j.Run.HasValue(RunMode.LaunchCountCharacteristic));

            AssertProperties(j, "Platform=X86, LaunchCount=1");
            AssertProperties(j.Environment, "Platform=X86");
            AssertProperties(j.Run, "LaunchCount=1");

            // 4. Freeze-unfreeze:
            j = j.Freeze().UnfreezeCopy();

            Assert.Equal("Platform=X86, LaunchCount=1", j.Id);
            Assert.Equal(Platform.X86, j.Environment.Platform);
            Assert.Equal(1, j.Run.LaunchCount);

            Assert.True(j.HasValue(EnvironmentMode.PlatformCharacteristic));
            Assert.True(j.Environment.HasValue(EnvironmentMode.PlatformCharacteristic));
            Assert.True(j.HasValue(RunMode.LaunchCountCharacteristic));
            Assert.True(j.Run.HasValue(RunMode.LaunchCountCharacteristic));

            AssertProperties(j, "Platform=X86, LaunchCount=1");
            AssertProperties(j.Environment, "Platform=X86");
            AssertProperties(j.Run, "LaunchCount=1");

            // 5. Test .With extensions
            j = j.Freeze()
                .WithId("NewId");
            Assert.Equal("NewId", j.Id); // id set

            j = j.Freeze()
                .WithPlatform(Platform.X64)
                .WithLaunchCount(2);

            Assert.Equal("NewId", j.Id); // id not lost
            Assert.Equal("NewId(Platform=X64, LaunchCount=2)", j.DisplayInfo);
            Assert.Equal(Platform.X64, j.Environment.Platform);
            Assert.Equal(2, j.Run.LaunchCount);

            Assert.True(j.HasValue(EnvironmentMode.PlatformCharacteristic));
            Assert.True(j.Environment.HasValue(EnvironmentMode.PlatformCharacteristic));
            Assert.True(j.HasValue(RunMode.LaunchCountCharacteristic));
            Assert.True(j.Run.HasValue(RunMode.LaunchCountCharacteristic));

            AssertProperties(j, "Platform=X64, LaunchCount=2");
            AssertProperties(j.Environment, "Platform=X64");
            AssertProperties(j.Run, "LaunchCount=2");
        }

        [Fact]
        public static void Test03IdDoesNotFlow()
        {
            var j = new Job(EnvironmentMode.LegacyJitX64, RunMode.Long); // id will not flow, new Job
            Assert.False(j.HasValue(CharacteristicObject.IdCharacteristic));
            Assert.False(j.Environment.HasValue(CharacteristicObject.IdCharacteristic));

            Job.EnvironmentCharacteristic[j] = EnvironmentMode.LegacyJitX86.UnfreezeCopy(); // id will not flow
            Assert.False(j.HasValue(CharacteristicObject.IdCharacteristic));
            Assert.False(j.Environment.HasValue(CharacteristicObject.IdCharacteristic));

            var c = new CharacteristicSet(EnvironmentMode.LegacyJitX64, RunMode.Long); // id will not flow, new CharacteristicSet
            Assert.False(c.HasValue(CharacteristicObject.IdCharacteristic));

            Job.EnvironmentCharacteristic[c] = EnvironmentMode.LegacyJitX86.UnfreezeCopy(); // id will not flow
            Assert.False(c.HasValue(CharacteristicObject.IdCharacteristic));

            CharacteristicObject.IdCharacteristic[c] = "MyId"; // id set explicitly
            Assert.Equal("MyId", c.Id);

            j = new Job("MyId", EnvironmentMode.LegacyJitX64, RunMode.Long); // id set explicitly
            Assert.Equal("MyId", j.Id);
            Assert.Equal("MyId", j.Environment.Id);

            Job.EnvironmentCharacteristic[j] = EnvironmentMode.LegacyJitX86.UnfreezeCopy(); // id will not flow
            Assert.Equal("MyId", j.Id);
            Assert.Equal("MyId", j.Environment.Id);

            j = j.WithJit(Jit.RyuJit);  // custom id will flow
            Assert.Equal("MyId", j.Id);
        }

        [Fact]
        public static void CustomJobIdIsPreserved()
        {
            const string id = "theId";

            var jobWithId = Job.Default.WithId(id);

            Assert.Equal(id, jobWithId.Id);

            var shouldHaveSameId = jobWithId.WithJit(Jit.RyuJit);

            Assert.Equal(id, shouldHaveSameId.Id);
        }

        [Fact]
        public static void PredefinedJobIdIsNotPreserved()
        {
            var predefinedJob = Job.Default;

            var customJob = predefinedJob.WithJit(Jit.RyuJit);

            Assert.NotEqual(predefinedJob.Id, customJob.Id);
        }

        [Fact]
        public static void BaselineDoesntChangeId()
        {
            const string id = "theId";

            var predefinedJob = Job.Default;
            var customJob = predefinedJob.AsBaseline();
            Assert.Equal(predefinedJob.Id, customJob.Id);

            var jobWithId = predefinedJob.WithId(id);
            var customJob2 = jobWithId.AsBaseline();
            Assert.Equal(jobWithId.Id, customJob2.Id);
        }

        [Fact]
        public static void Test04Apply()
        {
            var j = new Job()
            {
                Run = { IterationCount = 1 }
            };

            AssertProperties(j, "IterationCount=1");

            j.Apply(
                new Job
                {
                    Environment = { Platform = Platform.X64 },
                    Run = { IterationCount = 2 }
                });
            AssertProperties(j, "Platform=X64, IterationCount=2");

            // filter by properties
            j.Environment.Apply(
                new Job()
                    .WithJit(Jit.RyuJit)
                    .WithGcAllowVeryLargeObjects(true)
                    .WithIterationCount(3)
                    .WithLaunchCount(22));
            AssertProperties(j, "Jit=RyuJit, Platform=X64, AllowVeryLargeObjects=True, IterationCount=2");

            // apply subnode
            j.Apply(
                new GcMode()
                {
                    AllowVeryLargeObjects = false
                });
            AssertProperties(j, "Jit=RyuJit, Platform=X64, AllowVeryLargeObjects=False, IterationCount=2");

            // Apply empty
            j.Apply(Job.Default); // does nothing
            AssertProperties(j, "Jit=RyuJit, Platform=X64, AllowVeryLargeObjects=False, IterationCount=2");
        }

        [Fact]
        public static void Test05ApplyCharacteristicSet()
        {
            var set1 = new CharacteristicSet();
            var set2 = new CharacteristicSet();

            set1
                .Apply(
                    new EnvironmentMode
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
                        Environment =
                        {
                            Platform = Platform.X86
                        }
                    });
            AssertProperties(set1, "LaunchCount=2, Platform=X86");
            Assert.Equal(Platform.X86, Job.EnvironmentCharacteristic[set1].Platform);
            Assert.True(set1.HasValue(Job.EnvironmentCharacteristic));
            Assert.Equal(Platform.X86, EnvironmentMode.PlatformCharacteristic[set1]);

            set2.Apply(EnvironmentMode.RyuJitX64).Apply(new GcMode { Concurrent = true });
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

            j.Environment.Gc.Apply(set1);
            AssertProperties(j, "Concurrent=True");

            j.Run.Apply(set1);
            AssertProperties(j, "Concurrent=True, LaunchCount=2");

            j.Environment.Apply(set1);
            AssertProperties(j, "Jit=RyuJit, Platform=X64, Concurrent=True, LaunchCount=2");

            j.Apply(set1);
            AssertProperties(j, "Jit=RyuJit, Platform=X64, Concurrent=True, LaunchCount=2");
        }

        [Fact]
        public static void Test06CharacteristicHacks()
        {
            var j = new Job();
            Assert.Equal(0, j.Run.IterationCount);

            RunMode.IterationCountCharacteristic[j] = 123;
            Assert.Equal(123, j.Run.IterationCount);

            var old = j.Run;
            Job.RunCharacteristic[j] = new RunMode();
            Assert.Equal(0, j.Run.IterationCount);

            Job.RunCharacteristic[j] = old;
            old.IterationCount = 234;
            Assert.Equal(234, j.Run.IterationCount);
            Assert.Equal(234, RunMode.IterationCountCharacteristic[j]);

            Characteristic a = Job.RunCharacteristic;
            // will not throw:
            a[j] = new RunMode();
            Assert.Throws<ArgumentNullException>(() => a[j] = null); // nulls for job nodes are not allowed;
            Assert.Throws<ArgumentNullException>(() => a[j] = Characteristic.EmptyValue);
            Assert.Throws<ArgumentException>(() => a[j] = new EnvironmentMode()); // not assignable;
            Assert.Throws<ArgumentException>(() => a[j] = new CharacteristicSet()); // not assignable;
            Assert.Throws<ArgumentException>(() => a[j] = 123); // not assignable;

            a = InfrastructureMode.ToolchainCharacteristic;
            // will not throw:
            a[j] = CsProjClassicNetToolchain.Net462;
            a[j] = null;
            a[j] = Characteristic.EmptyValue;
            Assert.Throws<ArgumentException>(() => a[j] = new EnvironmentMode()); // not assignable;
            Assert.Throws<ArgumentException>(() => a[j] = new CharacteristicSet()); // not assignable;
            Assert.Throws<ArgumentException>(() => a[j] = 123); // not assignable;
        }

        [Fact]
        public static void MutatorAppliedToOtherJobOverwritesOnlyTheConfiguredSettings()
        {
            var jobBefore = Job.Default.WithRuntime(CoreRuntime.Core30); // this is a default job with Runtime set to Core
            var copy = jobBefore.UnfreezeCopy();

            Assert.False(copy.HasValue(RunMode.MaxIterationCountCharacteristic));

            var mutator = Job.Default.WithMaxIterationCount(20);

            copy.Apply(mutator);

            Assert.True(copy.HasValue(RunMode.MaxIterationCountCharacteristic));
            Assert.Equal(20, copy.Run.MaxIterationCount);
            Assert.False(jobBefore.HasValue(RunMode.MaxIterationCountCharacteristic));
            Assert.True(copy.Environment.Runtime is CoreRuntime);
            Assert.False(copy.Meta.IsMutator); // the job does not became a mutator itself, this config should not be copied
        }

        [Fact]
        public static void AllJobModesPropertyNamesMatchCharacteristicNames() // it's mandatory to generate the right c# code
        {
            var jobModes = typeof(JobMode<>)
                .Assembly
                .GetExportedTypes()
                .Where(type => type.IsSubclassOf(typeof(CharacteristicObject)) && IsSubclassOfobModeOfItself(type))
                .ToArray();

            foreach (var jobMode in jobModes)
            {
                var properties = jobMode.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly).Where(property => property.CanRead && property.CanWrite);

                foreach (var property in properties)
                {
                    string expectedPropertyName = $"{property.Name}Characteristic";
                    Assert.True(null != jobMode.GetField(expectedPropertyName, BindingFlags.Static | BindingFlags.Public), $"{expectedPropertyName} in {jobMode.Name} does not exist");
                }
            }
        }

        [Fact]
        public static void WithNuGet()
        {
            var j = new Job("SomeId");

            //.WithNuGet extensions

            j = j.Freeze().WithNuGet("Newtonsoft.Json");
            Assert.Single(j.Infrastructure.NuGetReferences);

            j = j.WithNuGet("AutoMapper", "7.0.1");
            Assert.Collection(j.Infrastructure.NuGetReferences,
                reference => Assert.Equal(new NuGetReference("AutoMapper", "7.0.1"), reference),
                reference => Assert.Equal(new NuGetReference("Newtonsoft.Json", ""), reference));

            Assert.Throws<ArgumentException>(() => j = j.WithNuGet("AutoMapper")); //adding is an error, since it's the same package
            Assert.Throws<ArgumentException>(() => j = j.WithNuGet("AutoMapper", "7.0.0-alpha-0001")); //adding is an error, since it's the same package

            j = j.WithNuGet("NLog", "4.5.10"); // ensure we can add at the end of a non-empty list
            Assert.Collection(j.Infrastructure.NuGetReferences,
                reference => Assert.Equal(new NuGetReference("AutoMapper", "7.0.1"), reference),
                reference => Assert.Equal(new NuGetReference("Newtonsoft.Json", ""), reference),
                reference => Assert.Equal(new NuGetReference("NLog", "4.5.10"), reference));

            var expected = new NuGetReferenceList(Array.Empty<NuGetReference>())
            {
                new NuGetReference("AutoMapper", "7.0.1"),
                new NuGetReference("Newtonsoft.Json", ""),
                new NuGetReference("NLog", "4.5.10"),
            };

            Assert.Equal(expected, j.Infrastructure.NuGetReferences); // ensure that the list's equality operator returns true when the contents are the same
        }

        private static bool IsSubclassOfobModeOfItself(Type type)
        {
            Type jobModeOfT;

            try
            {
                jobModeOfT = typeof(JobMode<>).MakeGenericType(type);
            }
            catch (ArgumentException) //  violates the constraint of type parameter 'T'.
            {
                return false;
            }

            return type.IsSubclassOf(jobModeOfT);
        }
    }
}