using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Tests.XUnit;
using BenchmarkDotNet.Toolchains;
using BenchmarkDotNet.Toolchains.InProcess.Emit;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace BenchmarkDotNet.IntegrationTests;

public class CodeGenTests(ITestOutputHelper output) : BenchmarkTestExecutor(output)
{
    public static IEnumerable<object[]> GetToolchains() =>
    [
        [Job.Default.GetToolchain()],
        [InProcessEmitToolchain.Default]
    ];

    [TheoryEnvSpecific(".Net Framework JIT is not tiered", EnvRequirement.DotNetCoreOnly)]
    [MemberData(nameof(GetToolchains), DisableDiscoveryEnumeration = true)]
    public void GeneratedBenchmarkTypeMethodsAreAggressivelyOptimized(IToolchain toolchain)
    {
        var config = CreateSimpleConfig(job: Job.Dry.WithToolchain(toolchain));
        CanExecute<BenchmarkManyTypes>(config);
    }

#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
    public class BenchmarkManyTypes
    {
        private static void AssertAggressiveOptimization()
        {
            var runnableType = GetRunnableType();
            AssertMethodsAggressivelyOptimized(runnableType);

            static Type GetRunnableType()
            {
                var stacktrace = new StackTrace(false);
                for (int i = 0; i < stacktrace.FrameCount; i++)
                {
                    var benchmarkType = stacktrace.GetFrame(i).GetMethod().DeclaringType;
                    do
                    {
                        if (benchmarkType.Name.StartsWith("Runnable_"))
                        {
                            return benchmarkType;
                        }
                        benchmarkType = benchmarkType.DeclaringType;
                    }
                    while (benchmarkType != null);
                }
                Assert.Fail("Runnable type not found");
                return null; // unreachable
            }

            static void AssertMethodsAggressivelyOptimized(Type type)
            {
                foreach (var method in type.GetMethods(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly))
                {
                    if (method.MethodImplementationFlags.HasFlag(MethodImplAttributes.NoOptimization))
                    {
                        Assert.False(
                            method.MethodImplementationFlags.HasFlag(Portability.CodeGenHelper.AggressiveOptimizationOptionForEmit),
                            $"Method is aggressively optimized: {method}"
                        );
                    }
                    else
                    {
                        Assert.True(
                            method.MethodImplementationFlags.HasFlag(Portability.CodeGenHelper.AggressiveOptimizationOptionForEmit),
                            $"Method is not aggressively optimized: {method}"
                        );
                    }
                }

                foreach (var nestedType in type.GetNestedTypes())
                {
                    AssertMethodsAggressivelyOptimized(nestedType);
                }
            }
        }

        [Benchmark]
        public void ReturnVoid() => AssertAggressiveOptimization();

        [Benchmark]
        public async Task ReturnTaskAsync() => AssertAggressiveOptimization();

        [Benchmark]
        public async ValueTask ReturnValueTaskAsync() => AssertAggressiveOptimization();

        [Benchmark]
        public string ReturnRefType()
        {
            AssertAggressiveOptimization();
            return default;
        }

        [Benchmark]
        public decimal ReturnValueType()
        {
            AssertAggressiveOptimization();
            return default;
        }

        [Benchmark]
        public async Task<string> ReturnTaskOfTAsync()
        {
            AssertAggressiveOptimization();
            return default;
        }

        [Benchmark]
        public ValueTask<decimal> ReturnValueTaskOfT()
        {
            AssertAggressiveOptimization();
            return default;
        }

        private int intField;

        [Benchmark]
        public ref int ReturnByRefType()
        {
            AssertAggressiveOptimization();
            return ref intField;
        }

        [Benchmark]
        public ref readonly int ReturnByRefReadonlyType()
        {
            AssertAggressiveOptimization();
            return ref intField;
        }

        [Benchmark]
        public unsafe void* ReturnVoidPointerType()
        {
            AssertAggressiveOptimization();
            return default;
        }
    }
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
}