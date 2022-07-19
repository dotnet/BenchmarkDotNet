using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Reports;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Toolchains;
using BenchmarkDotNet.Toolchains.InProcess.Emit;
using Xunit;
using Xunit.Abstractions;

namespace BenchmarkDotNet.IntegrationTests
{
    public class ParamSourceTests: BenchmarkTestExecutor
    {
        public ParamSourceTests(ITestOutputHelper output) : base(output) { }

        public static IEnumerable<object[]> GetToolchains()
            => new[]
                {
                    new object[] { Job.Default.GetToolchain() },
                    new object[] { InProcessEmitToolchain.Instance },
                };

        [Fact]
        public void ParamSourceCanHandleStringWithSurrogates()
        {
            CanExecute<ParamSourceIsStringWithSurrogates>(CreateSimpleConfig());
        }

        public class ParamSourceIsStringWithSurrogates
        {
            public IEnumerable<string> StringValues
            {
                get
                {
                    yield return "a" + string.Join("", Enumerable.Repeat("😀", 40)) + "a";
                    yield return "a" + string.Join("", Enumerable.Repeat("😀", 40));
                    yield return string.Join("", Enumerable.Repeat("😀", 40)) + "a";
                    yield return string.Join("", Enumerable.Repeat("😀", 40));
                }
            }

            [ParamsSource(nameof(StringValues))]
            public string _ { get; set; }

            [Benchmark]
            public void Method() { }
        }

        private Summary CanExecuteWithExtraInfo(Type type, IToolchain toolchain)
        {
            IConfig config = CreateSimpleConfig(job: Job.Dry.WithToolchain(toolchain));
            // Show the relevant codegen excerpt in test results (the *.notcs is not part of the logs)
            // This uses the private static CodeGenerator.GetParamsContent() method.
            // Method changes there must be applied here - or consider making the method internal.
            // TODO Should we omit the extra info if (toolchain.IsInProcess)? Doesn't use GetParamsContent().
            Output.WriteLine("// Benchmarks and CodeGenerator.GetParamsContent()");
            BenchmarkRunInfo runInfo = BenchmarkConverter.TypeToBenchmarks(type, config);
            MethodInfo getParamsContent = typeof(Code.CodeGenerator).GetMethod("GetParamsContent", BindingFlags.Static | BindingFlags.NonPublic, null, new[] { typeof(BenchmarkCase) }, null);
            Assert.NotNull(getParamsContent);
            foreach (BenchmarkCase benchmarkCase in runInfo.BenchmarksCases)
            {
                Output.WriteLine("//   " + benchmarkCase.DisplayInfo);
                Output.WriteLine((string)getParamsContent.Invoke(null, new object[] { benchmarkCase }));
            }
            return CanExecute(type, config);
        }

        public interface ITargetInterface
        {
            int Data { get; }
        }

        private class NonPublicSource : ITargetInterface
        {
            public int Data { get; }
            public NonPublicSource(int data) => Data = data;
            public override string ToString() => "src " + Data.ToString();
        }

        public class PrivateClassWithPublicInterface
        {
            public static IEnumerable<ITargetInterface> GetSource()
            {
                yield return null;
                yield return new NonPublicSource(1);
                yield return new NonPublicSource(2);
            }

            [ParamsSource(nameof(GetSource))]
            public ITargetInterface ParamsTarget { get; set; }

            [Benchmark]
            public int Benchmark() => ParamsTarget?.Data ?? 0;
        }

        [Theory, MemberData(nameof(GetToolchains))]
        public void PrivateClassWithPublicInterface_Succeeds(IToolchain toolchain) => CanExecuteWithExtraInfo(typeof(PrivateClassWithPublicInterface), toolchain);

        public class PrivateClassWithPublicInterface_Array
        {
            public IEnumerable<ITargetInterface[]> GetSource()
            {
                yield return null;
                yield return Array.Empty<NonPublicSource>();
                yield return new NonPublicSource[] { null };
                yield return new[] { new NonPublicSource(1), new NonPublicSource(2) };
            }

            [ParamsSource(nameof(GetSource))]
            public ITargetInterface[] ParamsTarget { get; set; }

            [Benchmark]
            public int Benchmark() => ParamsTarget?.Sum(p => p?.Data ?? 0) ?? 0;
        }

        [Theory, MemberData(nameof(GetToolchains))]
        public void PrivateClassWithPublicInterface_Array_Succeeds(IToolchain toolchain) => CanExecuteWithExtraInfo(typeof(PrivateClassWithPublicInterface_Array), toolchain);

        public class PrivateClassWithPublicInterface_Enumerable
        {
            public IEnumerable<IEnumerable<ITargetInterface>> GetSource()
            {
                IEnumerable<ITargetInterface> YieldNull() { yield return null; }
                yield return null;
                yield return Enumerable.Empty<NonPublicSource>();
                yield return YieldNull();
                yield return PrivateClassWithPublicInterface.GetSource();
            }

            [ParamsSource(nameof(GetSource))]
            public IEnumerable<ITargetInterface> ParamsTarget { get; set; }

            [Benchmark]
            public int Benchmark() => ParamsTarget?.Sum(p => p?.Data ?? 0) ?? 0;
        }

        [Theory, MemberData(nameof(GetToolchains))]
        public void PrivateClassWithPublicInterface_Enumerable_Succeeds(IToolchain toolchain) => CanExecuteWithExtraInfo(typeof(PrivateClassWithPublicInterface_Enumerable), toolchain);

        public class PrivateClassWithPublicInterface_AsObject
        {
            public static IEnumerable<object> GetSource()
            {
                yield return null;
                yield return new NonPublicSource(1);
                yield return new NonPublicSource(2);
            }

            [ParamsSource(nameof(GetSource))]
            public ITargetInterface ParamsTarget { get; set; }

            [Benchmark]
            public int Benchmark() => ParamsTarget?.Data ?? 0;
        }

        [Theory, MemberData(nameof(GetToolchains))]
        public void PrivateClassWithPublicInterface_AsObject_Succeeds(IToolchain toolchain) => CanExecuteWithExtraInfo(typeof(PrivateClassWithPublicInterface_AsObject), toolchain);

        public class PublicSource
        {
            public int Data { get; }
            public PublicSource(int data) => Data = data;
            // Notes:
            // - op_Implicit would be meaningless since codegen wouldn't have to do anything.
            // - TODO op_Explicit is currently not supported by InProcessEmitToolchain (See TryChangeType() in Toolchains/InProcess.Emit.Implementation/Runnable/RunnableReflectionHelpers.cs)
            public static explicit operator TargetType(PublicSource @this) => @this != null ? new TargetType(@this.Data) : null;
            public override string ToString() => "src " + Data.ToString();
        }

        public class TargetType
        {
            public int Data { get; }
            public TargetType(int data) => Data = data;
            public override string ToString() => "target " + Data.ToString();
        }

        public class SourceWithExplicitCastToTarget
        {
            public static IEnumerable<PublicSource> GetSource()
            {
                yield return null;
                yield return new PublicSource(1);
                yield return new PublicSource(2);
            }

            [ParamsSource(nameof(GetSource))]
            public TargetType ParamsTarget { get; set; }

            [Benchmark]
            public int Benchmark() => ParamsTarget?.Data ?? 0;
        }

        [Fact]
        public void SourceWithExplicitCastToTarget_DefaultToolchain_Succeeds() => CanExecuteWithExtraInfo(typeof(SourceWithExplicitCastToTarget), Job.Default.GetToolchain());

        [Fact]
        public void SourceWithExplicitCastToTarget_InProcessToolchain_Throws()
        {
            // See notes on op_Explicit above.
            Assert.ThrowsAny<Exception>(() => CanExecuteWithExtraInfo(typeof(SourceWithExplicitCastToTarget), InProcessEmitToolchain.Instance));
        }
    }
}
