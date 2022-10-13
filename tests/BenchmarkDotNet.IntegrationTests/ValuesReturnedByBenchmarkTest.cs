using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Environments;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Toolchains;
using BenchmarkDotNet.Toolchains.InProcess.Emit;
using Xunit;
using Xunit.Abstractions;

namespace BenchmarkDotNet.IntegrationTests
{
    public class ValuesReturnedByBenchmarkTest : BenchmarkTestExecutor
    {
        public ValuesReturnedByBenchmarkTest(ITestOutputHelper output) : base(output) { }

        public static IEnumerable<object[]> GetToolchains() => new[]
        {
            new object[] { Job.Default.GetToolchain() },
            new object[] { InProcessEmitToolchain.Instance },
        };

        [Theory, MemberData(nameof(GetToolchains))]
        public void AnyValueCanBeReturned(IToolchain toolchain) => CanExecute<ValuesReturnedByBenchmark>(ManualConfig.CreateEmpty().AddJob(Job.Dry.WithToolchain(toolchain)));

        public class ValuesReturnedByBenchmark
        {
#if NETFRAMEWORK
            [Benchmark]
            public System.Windows.Point? TypeFromCustomFrameworkAssembly() => new System.Windows.Point();

            [Benchmark]
            public Diagnostics.Windows.InliningDiagnoser TypeFromCustomDependency() => new Diagnostics.Windows.InliningDiagnoser();
#endif

            [Benchmark]
            public object ReturnNullForReferenceType() => null;

            [Benchmark]
            public object ReturnNotNullForReferenceType() => new object();

            [Benchmark]
            public DateTime? ReturnNullForNullableType() => null;

            [Benchmark]
            public DateTime? ReturnNotNullForNullableType() => DateTime.UtcNow;

            [Benchmark]
            public DateTime ReturnDefaultValueForValueType() => default;

            [Benchmark]
            public DateTime ReturnNonDefaultValueForValueType() => DateTime.UtcNow;

            [Benchmark]
            public Result<DateTime> ReturnGenericValueType() => new Result<DateTime>();

            [Benchmark]
            public Jit ReturnEnum() => Jit.RyuJit;

            private int intergerField = 123;
            [Benchmark]
            public ref int ReturnByRef() => ref intergerField;

            [Benchmark]
            public ref readonly int ReturnByReadonlyRef() => ref intergerField;

            public readonly struct ReadOnlyStruct { }
            private ReadOnlyStruct readOnlyStructField;

            [Benchmark]
            public ReadOnlyStruct ReturnReadOnlyStruct() => new ReadOnlyStruct();

            [Benchmark]
            public ref ReadOnlyStruct ReturnReadOnlyStructByRef() => ref readOnlyStructField;

            [Benchmark]
            public ref readonly ReadOnlyStruct ReturnReadOnlyStructByReadonlyRef() => ref readOnlyStructField;

            [Benchmark]
            public Span<byte> ReturnStackOnlyType() => new Span<byte>(Array.Empty<byte>());

            [Benchmark]
            public ImmutableArray<int> TypeFromNetStandardNuGetPackage() => ImmutableArray<int>.Empty;

            [Benchmark]
            public ValueTuple<int> TypeInTwoDlls() => new ValueTuple<int>();

            public struct Result<T>
            {
                public T Field;

                public Result(T field)
                {
                    Field = field;
                }
            }

            [Benchmark]
            public Job TypeCalledJob() => new Job();

            public class Job { }

            [Benchmark]
            public NoNamespace TypeWithoutNamespace() => new NoNamespace();

            [Benchmark]
            public unsafe void* PointerToAnything() => System.IntPtr.Zero.ToPointer();

            [Benchmark]
            public unsafe int* PointerToUnmanagedType() => (int*)System.IntPtr.Zero.ToPointer();

            [Benchmark]
            public System.IntPtr IntPtr() => System.IntPtr.Zero;

            [Benchmark]
            public System.UIntPtr UIntPtr() => System.UIntPtr.Zero;

            [Benchmark]
            public nint NativeSizeInteger() => 0;

            [Benchmark]
            public nuint UnsignedNativeSizeInteger() => 0;

            [Benchmark]
            public Tuple<Outer, Outer.Inner> BenchmarkInnerClass() => Tuple.Create(new Outer(), new Outer.Inner());

            [Benchmark]
            public Tuple<Outer, Outer.InnerGeneric<string>> BenchmarkGenericInnerClass() => Tuple.Create(new Outer(), new Outer.InnerGeneric<string>());
        }

        public class Outer
        {
            public class Inner
            {
            }

            public class InnerGeneric<T>
            {
            }
        }
    }
}

public class NoNamespace
{
}