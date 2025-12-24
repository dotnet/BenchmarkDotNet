using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Toolchains;
using BenchmarkDotNet.Toolchains.InProcess.Emit;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace BenchmarkDotNet.IntegrationTests;

public class CustomTaskAndAwaitableTests(ITestOutputHelper output) : BenchmarkTestExecutor(output)
{
    public static TheoryData<IToolchain> GetToolchains() => new(
    [
        Job.Default.GetToolchain(),
        InProcessEmitToolchain.Default
    ]);

    public class BenchmarkCustomAwaitableSimple
    {
        [GlobalSetup]
        public CustomAwaitable GlobalSetup() => new();

        [GlobalCleanup]
        public CustomAwaitable GlobalCleanup() => new();

        [IterationSetup]
        public CustomAwaitable IterationSetup() => new();

        [IterationCleanup]
        public CustomAwaitable IterationCleanup() => new();

        [Benchmark]
        public CustomAwaitable Benchmark() => new();
    }

    [Theory]
    [MemberData(nameof(GetToolchains), DisableDiscoveryEnumeration = true)]
    public void ReturnCustomAwaitableType(IToolchain toolchain)
    {
        var config = CreateSimpleConfig(job: Job.Dry.WithToolchain(toolchain));
        CanExecute<BenchmarkCustomAwaitableSimple>(config);
    }

    public class BenchmarkCustomAwaitableOverrideCaller
    {
        [GlobalSetup]
        public CustomAwaitable GlobalSetup()
        {
            Assert.Equal(0, AsyncCustomTaskMethodBuilder.InUseCounter);
            return new();
        }

        [GlobalCleanup]
        public CustomAwaitable GlobalCleanup()
        {
            Assert.Equal(0, AsyncCustomTaskMethodBuilder.InUseCounter);
            return new();
        }

        [IterationSetup]
        public CustomAwaitable IterationSetup()
        {
            Assert.Equal(0, AsyncCustomTaskMethodBuilder.InUseCounter);
            return new();
        }

        [IterationCleanup]
        public CustomAwaitable IterationCleanup()
        {
            Assert.Equal(1, AsyncCustomTaskMethodBuilder.InUseCounter);
            return new();
        }

        [Benchmark]
        [AsyncCallerType(typeof(CustomTask))]
        public CustomAwaitable Benchmark()
        {
            Assert.Equal(1, AsyncCustomTaskMethodBuilder.InUseCounter);
            return new();
        }
    }

    [Theory]
    [MemberData(nameof(GetToolchains), DisableDiscoveryEnumeration = true)]
    public void ReturnCustomAwaitableTypeAndOverrideCaller(IToolchain toolchain)
    {
        var config = CreateSimpleConfig(job: Job.Dry.WithToolchain(toolchain));
        CanExecute<BenchmarkCustomAwaitableOverrideCaller>(config);
    }

    public class BenchmarkCustomTask
    {
        [GlobalSetup]
        public CustomTask GlobalSetup()
        {
            Assert.Equal(0, AsyncCustomTaskMethodBuilder.InUseCounter);
            return new();
        }

        [GlobalCleanup]
        public CustomTask GlobalCleanup()
        {
            Assert.Equal(0, AsyncCustomTaskMethodBuilder.InUseCounter);
            return new();
        }

        [IterationSetup]
        public CustomTask IterationSetup()
        {
            Assert.Equal(0, AsyncCustomTaskMethodBuilder.InUseCounter);
            return new();
        }

        [IterationCleanup]
        public CustomTask IterationCleanup()
        {
            Assert.Equal(1, AsyncCustomTaskMethodBuilder.InUseCounter);
            return new();
        }

        [Benchmark]
        public async CustomTask Benchmark()
        {
            Assert.Equal(2, AsyncCustomTaskMethodBuilder.InUseCounter);
            await new ValueTask();
        }
    }

    [Theory]
    [MemberData(nameof(GetToolchains), DisableDiscoveryEnumeration = true)]
    public void ReturnCustomTask(IToolchain toolchain)
    {
        var config = CreateSimpleConfig(job: Job.Dry.WithToolchain(toolchain));
        CanExecute<BenchmarkCustomTask>(config);
    }

    public class BenchmarkCustomTaskOverrideCaller
    {
        [GlobalSetup]
        public CustomTask GlobalSetup()
        {
            Assert.Equal(0, AsyncCustomTaskMethodBuilder.InUseCounter);
            return new();
        }

        [GlobalCleanup]
        public CustomTask GlobalCleanup()
        {
            Assert.Equal(0, AsyncCustomTaskMethodBuilder.InUseCounter);
            return new();
        }

        [IterationSetup]
        public CustomTask IterationSetup()
        {
            Assert.Equal(0, AsyncCustomTaskMethodBuilder.InUseCounter);
            return new();
        }

        [IterationCleanup]
        public CustomTask IterationCleanup()
        {
            Assert.Equal(0, AsyncCustomTaskMethodBuilder.InUseCounter);
            return new();
        }

        [Benchmark]
        [AsyncCallerType(typeof(ValueTask))]
        public async CustomTask Benchmark()
        {
            Assert.Equal(1, AsyncCustomTaskMethodBuilder.InUseCounter);
            await new ValueTask();
        }
    }

    [Theory]
    [MemberData(nameof(GetToolchains), DisableDiscoveryEnumeration = true)]
    public void ReturnCustomTaskAndOverrideCaller(IToolchain toolchain)
    {
        var config = CreateSimpleConfig(job: Job.Dry.WithToolchain(toolchain));
        CanExecute<BenchmarkCustomTaskOverrideCaller>(config);
    }

    // [AsyncMethodBuilder] on methods is C# 10 feature, but it also requires the attribute be allowed on methods, which only works in .Net 5+.
    // It can technically be poly-filled to work, but it's not worth the extra complexity to make it work for the generated project.
#if NET5_0_OR_GREATER
    public class BenchmarkCustomBuilder
    {
        [GlobalSetup]
        public void GlobalSetup()
        {
            Assert.Equal(0, AsyncWrapperTaskMethodBuilder.InUseCounter);
        }

        [GlobalCleanup]
        public void GlobalCleanup()
        {
            Assert.Equal(0, AsyncWrapperTaskMethodBuilder.InUseCounter);
        }

        [IterationSetup]
        public void IterationSetup()
        {
            Assert.Equal(0, AsyncWrapperTaskMethodBuilder.InUseCounter);
        }

        [IterationCleanup]
        public void IterationCleanup()
        {
            Assert.Equal(1, AsyncWrapperTaskMethodBuilder.InUseCounter);
        }

        [Benchmark]
        [AsyncMethodBuilder(typeof(AsyncWrapperTaskMethodBuilder))]
        public async Task Benchmark()
        {
            Assert.Equal(2, AsyncWrapperTaskMethodBuilder.InUseCounter);
            await new ValueTask();
        }
    }

    [Theory]
    [MemberData(nameof(GetToolchains), DisableDiscoveryEnumeration = true)]
    public void ReturnCustomBuilder(IToolchain toolchain)
    {
        var config = CreateSimpleConfig(job: Job.Dry.WithToolchain(toolchain));
        CanExecute<BenchmarkCustomBuilder>(config);
    }
#endif
}
