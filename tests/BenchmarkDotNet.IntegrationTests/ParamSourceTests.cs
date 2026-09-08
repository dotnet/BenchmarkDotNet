using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Code;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Reports;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Toolchains;
using BenchmarkDotNet.Toolchains.InProcess.Emit;
using BenchmarkDotNet.Toolchains.InProcess.NoEmit;

namespace BenchmarkDotNet.IntegrationTests
{
    public class ParamSourceTests : BenchmarkTestExecutor
    {
        public ParamSourceTests(ITestOutputHelper output) : base(output) { }

        public static IEnumerable<object[]> GetToolchains()
        {
            yield return [InProcessEmitToolchain.Default];

            if (ContinuousIntegration.IsGitHubDraftPR())
                yield break;

            yield return [Job.Default.GetToolchain()];
        }

        // InProcessNoEmit doesn't support arguments (#687), so only parameter tests can use it.
        public static IEnumerable<object[]> GetParamsToolchains()
        {
            yield return [InProcessNoEmitToolchain.Default];
            foreach (var toolchain in GetToolchains())
                yield return toolchain;
        }

        [Fact]
        public void ParamSourceCanHandleStringWithSurrogates()
        {
            CanExecute<ParamSourceIsStringWithSurrogates>(CreateSimpleConfig());
        }

        // Not a compilation-time constant, so the generated code re-obtains it from the source rather than
        // embedding a literal - which is the path these tests are about.
        public class Box
        {
            public int Value { get; set; }
        }

        [Theory, MemberData(nameof(GetParamsToolchains), DisableDiscoveryEnumeration = true)]
        public void StaticParamCanUseInstanceSource(IToolchain toolchain)
            => CanExecuteWithExtraInfo(typeof(StaticParamFromInstanceSource), toolchain);

        public class StaticParamFromInstanceSource
        {
            public IEnumerable<object> Values()
            {
                yield return new Box { Value = 42 };
            }

            [ParamsSource(nameof(Values))]
            public static Box Value { get; set; } = null!;

            [Benchmark]
            public int Run()
                => Value.Value == 42 ? Value.Value : throw new InvalidOperationException($"Wrong parameter: {Value.Value}.");
        }

        // The runnable assigns parameters through an object initializer, which can set an init-only setter; the
        // in-process toolchains reach the same setter reflectively.
        [Theory, MemberData(nameof(GetParamsToolchains), DisableDiscoveryEnumeration = true)]
        public void InitOnlyParamIsSupported(IToolchain toolchain)
            => CanExecuteWithExtraInfo(typeof(InitOnlyParam), toolchain);

        public class InitOnlyParam
        {
            [Params(42)]
            public int Value { get; init; }

            [Benchmark]
            public int Run()
                => Value == 42 ? Value : throw new InvalidOperationException($"The init-only parameter was not set (Value = {Value}).");
        }

        public interface IBox
        {
            int Value { get; }
        }

        public class BoxImpl : IBox
        {
            public int Value { get; set; }
        }

        // The counterpart shape: the same single-argument benchmark fed by a source that yields the argument
        // itself rather than a one-element argument list. Both are supported - the generated code indexes where
        // the source is declared to yield object[], which this one is not.
        [Theory, MemberData(nameof(GetToolchains), DisableDiscoveryEnumeration = true)]
        public void SingleArgumentSourceYieldingTheValueDirectlyIsNotIndexed(IToolchain toolchain)
            => CanExecuteWithExtraInfo(typeof(SingleBaseTypedArgumentFromObject), toolchain);

        public class SingleBaseTypedArgumentFromObject
        {
            public IEnumerable<object> Data()
            {
                yield return new BoxImpl { Value = 7 };
            }

            [Benchmark]
            [ArgumentsSource(nameof(Data))]
            public int Run(IBox box)
                => box.Value == 7 ? box.Value : throw new InvalidOperationException($"Wrong argument: {box}.");
        }

        // One read serves the whole row, so a sequence that can only be enumerated once is enough - and before
        // the row became the unit, each argument got its own invocation and this guard could never fire.
        [Theory, MemberData(nameof(GetToolchains), DisableDiscoveryEnumeration = true)]
        public void InstanceArgumentsSourceIsInvokedOncePerCase(IToolchain toolchain)
            => CanExecuteWithExtraInfo(typeof(InstanceSingleEnumerationArgumentsSource), toolchain);

        public class InstanceSingleEnumerationArgumentsSource
        {
            public IEnumerable<object[]> Data() => new SingleEnumeration();

            [Benchmark]
            [ArgumentsSource(nameof(Data))]
            public int Sum(Box a, Box b) => a.Value + b.Value;

            private sealed class SingleEnumeration : IEnumerable<object[]>
            {
                private bool enumerated;

                public IEnumerator<object[]> GetEnumerator()
                {
                    if (enumerated)
                        throw new InvalidOperationException("The source sequence was enumerated more than once.");
                    enumerated = true;
                    yield return new object[] { new Box { Value = 1 }, new Box { Value = 2 } };
                }

                System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => GetEnumerator();
            }
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
            public required string _ { get; set; }

            [Benchmark]
            public void Method() { }
        }

        private Summary CanExecuteWithExtraInfo(Type type, IToolchain toolchain)
        {
            IConfig config = CreateSimpleConfig(job: Job.Dry.WithToolchain(toolchain));
            if (!toolchain.IsInProcess)
            {
                // Show the relevant codegen excerpt in test results (the *.notcs is not part of the logs)
                Output.WriteLine("// Benchmarks and CodeGenerator.GetParamsContent()");
                BenchmarkRunInfo runInfo = BenchmarkConverter.TypeToBenchmarks(type, config);
                foreach (BenchmarkCase benchmarkCase in runInfo.BenchmarksCases)
                {
                    Output.WriteLine("//   " + benchmarkCase.DisplayInfo);
                    Output.WriteLine(CodeGenerator.GetParamsInitializer(benchmarkCase));
                }
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
            public static IEnumerable<ITargetInterface?> GetSource()
            {
                yield return null;
                yield return new NonPublicSource(1);
                yield return new NonPublicSource(2);
            }

            [ParamsSource(nameof(GetSource))]
            public required ITargetInterface? ParamsTarget { get; set; }

            [Benchmark]
            public int Benchmark() => ParamsTarget?.Data ?? 0;
        }

        [Theory, MemberData(nameof(GetParamsToolchains), DisableDiscoveryEnumeration = true)]
        public void PrivateClassWithPublicInterface_Succeeds(IToolchain toolchain) => CanExecuteWithExtraInfo(typeof(PrivateClassWithPublicInterface), toolchain);

        public class PrivateClassWithPublicInterface_Array
        {
            public IEnumerable<ITargetInterface?[]?> GetSource()
            {
                yield return null;
                yield return Array.Empty<NonPublicSource>();
                yield return new NonPublicSource?[] { null };
                yield return new[] { new NonPublicSource(1), new NonPublicSource(2) };
            }

            [ParamsSource(nameof(GetSource))]
            public required ITargetInterface?[]? ParamsTarget { get; set; }

            [Benchmark]
            public int Benchmark() => ParamsTarget?.Sum(p => p?.Data ?? 0) ?? 0;
        }

        [Theory, MemberData(nameof(GetParamsToolchains), DisableDiscoveryEnumeration = true)]
        public void PrivateClassWithPublicInterface_Array_Succeeds(IToolchain toolchain) => CanExecuteWithExtraInfo(typeof(PrivateClassWithPublicInterface_Array), toolchain);

        public class PrivateClassWithPublicInterface_Enumerable
        {
            public IEnumerable<IEnumerable<ITargetInterface?>?> GetSource()
            {
                static IEnumerable<ITargetInterface?> YieldNull() { yield return null; }
                yield return null;
                yield return Enumerable.Empty<NonPublicSource>();
                yield return YieldNull();
                yield return PrivateClassWithPublicInterface.GetSource();
            }

            [ParamsSource(nameof(GetSource))]
            public required IEnumerable<ITargetInterface?>? ParamsTarget { get; set; }

            [Benchmark]
            public int Benchmark() => ParamsTarget?.Sum(p => p?.Data ?? 0) ?? 0;
        }

        [Theory, MemberData(nameof(GetParamsToolchains), DisableDiscoveryEnumeration = true)]
        public void PrivateClassWithPublicInterface_Enumerable_Succeeds(IToolchain toolchain) => CanExecuteWithExtraInfo(typeof(PrivateClassWithPublicInterface_Enumerable), toolchain);

        public class PrivateClassWithPublicInterface_AsObject
        {
            public static IEnumerable<object?> GetSource()
            {
                yield return null;
                yield return new NonPublicSource(1);
                yield return new NonPublicSource(2);
            }

            [ParamsSource(nameof(GetSource))]
            public required ITargetInterface? ParamsTarget { get; set; }

            [Benchmark]
            public int Benchmark() => ParamsTarget?.Data ?? 0;
        }

        [Theory, MemberData(nameof(GetParamsToolchains), DisableDiscoveryEnumeration = true)]
        public void PrivateClassWithPublicInterface_AsObject_Succeeds(IToolchain toolchain) => CanExecuteWithExtraInfo(typeof(PrivateClassWithPublicInterface_AsObject), toolchain);

        public class PublicSource
        {
            public int Data { get; }
            public PublicSource(int data) => Data = data;
            // op_Implicit would be meaningless because codegen wouldn't have to do anything.
            public static explicit operator TargetType?(PublicSource @this) => @this != null ? new TargetType(@this.Data) : null;
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
            public static IEnumerable<PublicSource?> GetSource()
            {
                yield return null;
                yield return new PublicSource(1);
                yield return new PublicSource(2);
            }

            [ParamsSource(nameof(GetSource))]
            public required TargetType? ParamsTarget { get; set; }

            [Benchmark]
            public int Benchmark() => ParamsTarget?.Data ?? 0;
        }

        [Fact]
        public void SourceWithExplicitCastToTarget_DefaultToolchain_Succeeds() => CanExecuteWithExtraInfo(typeof(SourceWithExplicitCastToTarget), Job.Default.GetToolchain());

        [Fact]
        public void SourceWithExplicitCastToTarget_InProcessToolchain_Throws()
        {
            // op_Explicit is currently not supported by InProcessEmitToolchain
            // See TryChangeType() in Toolchains/InProcess.Emit.Implementation/Runnable/RunnableReflectionHelpers.cs
            // If that changes, this test and the one above should be merged into:
            //   [Theory, MemberData(nameof(GetToolchains))]
            //   public void SourceWithExplicitCastToTarget_Succeeds(IToolchain toolchain) => CanExecuteWithExtraInfo(typeof(SourceWithExplicitCastToTarget), toolchain);
            Assert.ThrowsAny<Exception>(() => CanExecuteWithExtraInfo(typeof(SourceWithExplicitCastToTarget), InProcessEmitToolchain.Default));
        }

        public abstract class OverridePropertyBase
        {
            public abstract int[] GetSourceProperty { get; }

            [ParamsSource(nameof(GetSourceProperty))]
            public int ParamsTarget { get; set; }
        }

        public class OverrideProperty : OverridePropertyBase
        {
            public override int[] GetSourceProperty => [1, 2, 3];

            [Benchmark]
            public int Benchmark() => ParamsTarget;
        }

        [Theory, MemberData(nameof(GetParamsToolchains), DisableDiscoveryEnumeration = true)]
        public void OverrideProperty_Succeeds(IToolchain toolchain) => CanExecuteWithExtraInfo(typeof(OverrideProperty), toolchain);

        public abstract class OverrideMethodBase
        {
            public abstract int[] GetSourceMethod();

            [ParamsSource(nameof(GetSourceMethod))]
            public int ParamsTarget { get; set; }
        }

        public class OverrideMethod : OverrideMethodBase
        {
            public override int[] GetSourceMethod() => [1, 2, 3];

            [Benchmark]
            public int Benchmark() => ParamsTarget;
        }

        [Theory, MemberData(nameof(GetParamsToolchains), DisableDiscoveryEnumeration = true)]
        public void OverrideMethod_Succeeds(IToolchain toolchain) => CanExecuteWithExtraInfo(typeof(OverrideMethod), toolchain);

        public class StaticParamsOnABase
        {
            [Params(1, 2)]
            public static int InheritedField;

            [ParamsSource(nameof(Values))]
            public static int InheritedFromSource { get; set; }

            public static IEnumerable<int> Values() { yield return 7; }
        }

        public class InheritsStaticParams : StaticParamsOnABase
        {
            // Asserted here rather than on the summary's standard output, which the in-process toolchains do not fill.
            [Benchmark]
            public int Benchmark()
                => InheritedFromSource == 7 && InheritedField is 1 or 2
                    ? InheritedField
                    : throw new InvalidOperationException($"Expected 1|2 from 7, got {InheritedField} from {InheritedFromSource}.");
        }

        // Reflection withholds a base type's statics unless FlattenHierarchy is asked for, so binding these at all
        // depends on discovery asking for it - and every toolchain has to reach them the same way afterwards: the
        // generated code through the benchmark type's name, the in-process ones by looking the member up again.
        [Theory, MemberData(nameof(GetParamsToolchains), DisableDiscoveryEnumeration = true)]
        public void ParamsOnABaseTypeStaticMemberAreAssigned(IToolchain toolchain)
            => CanExecuteWithExtraInfo(typeof(InheritsStaticParams), toolchain);

        public class HidesBaseMembers
        {
            protected int Hidden { get; set; }

            public static int HiddenStatic { get; set; }

            public string HiddenField = "";

            // Same name and same type as the member hiding it, so matching on the declared type leaves two
            // candidates and only the most derived one is the parameter.
            public static string HiddenStaticField = "";

            // Hidden by a member of the other kind: looking properties up before fields finds these instead of
            // the fields that carry the attribute.
            public string PropertyHiddenByField { get; set; } = "";

            public static string StaticPropertyHiddenByField { get; set; } = "";
        }

        // Looking a parameter up by name alone finds both the member and the one it hides, which reflection reports
        // as an ambiguous name rather than a choice.
        public class HidingParams : HidesBaseMembers
        {
            [Params("a")]
            public new string Hidden { get; set; } = null!;

            [Params("b")]
            public static new string HiddenStatic { get; set; } = null!;

            [Params("c")]
            public new string HiddenField = null!;

            [Params("d")]
            public static new string HiddenStaticField = null!;

            [Params("e")]
            public new string PropertyHiddenByField = null!;

            [Params("f")]
            public static new string StaticPropertyHiddenByField = null!;

            [Benchmark]
            public string Benchmark()
                => Hidden == "a" && HiddenStatic == "b" && HiddenField == "c" && HiddenStaticField == "d"
                    && PropertyHiddenByField == "e" && StaticPropertyHiddenByField == "f"
                    ? Hidden + HiddenStatic + HiddenField + HiddenStaticField + PropertyHiddenByField + StaticPropertyHiddenByField
                    : throw new InvalidOperationException(
                        $"Expected a/b/c/d/e/f, got {Hidden}/{HiddenStatic}/{HiddenField}/{HiddenStaticField}"
                        + $"/{PropertyHiddenByField}/{StaticPropertyHiddenByField}.");
        }

        [Theory, MemberData(nameof(GetParamsToolchains), DisableDiscoveryEnumeration = true)]
        public void ParamsHidingABaseMemberAreAssigned(IToolchain toolchain)
            => CanExecuteWithExtraInfo(typeof(HidingParams), toolchain);

        // The members above hide ones that carry no attribute, so only one declaration is ever a parameter.
        // Here both carry it, which is what makes the pair reach the parameter list: GetFields reports a hidden
        // base field alongside the `new` one hiding it, where GetProperties collapses the pair. Two parameters of
        // one name multiply the cases against each other and emit the name twice in the runnable's object
        // initializer (CS1912), so only the most derived declaration may survive.
        public class AttributedOnBothBase
        {
            [Params(1, 2)] public int SharedField;

            [Params(3, 4)] public int SharedProperty { get; set; }
        }

        public class ParamsOnBothDeclarations : AttributedOnBothBase
        {
            [Params(5)] public new int SharedField;

            [Params(6)] public new int SharedProperty { get; set; }

            [Benchmark]
            public int Benchmark()
                => SharedField == 5 && SharedProperty == 6
                    ? SharedField
                    : throw new InvalidOperationException($"Expected 5/6, got {SharedField}/{SharedProperty}.");
        }

        [Theory, MemberData(nameof(GetParamsToolchains), DisableDiscoveryEnumeration = true)]
        public void ParamsOnBothADerivedMemberAndTheOneItHidesUseTheDerivedOne(IToolchain toolchain)
            => CanExecuteWithExtraInfo(typeof(ParamsOnBothDeclarations), toolchain);
    }
}
