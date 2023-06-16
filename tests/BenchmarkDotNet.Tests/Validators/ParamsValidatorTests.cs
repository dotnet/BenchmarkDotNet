using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Validators;
using Xunit;
using Xunit.Abstractions;
#pragma warning disable CS0414

namespace BenchmarkDotNet.Tests.Validators
{
    [SuppressMessage("ReSharper", "ClassNeverInstantiated.Global")]
    [SuppressMessage("ReSharper", "MemberCanBePrivate.Global")]
    [SuppressMessage("ReSharper", "UnusedAutoPropertyAccessor.Local")]
    public class ParamsValidatorTests
    {
        private readonly ITestOutputHelper output;

        public ParamsValidatorTests(ITestOutputHelper output)
        {
            this.output = output;
        }

        private void Check<T>(params string[] messageParts)
        {
            var typeToBenchmarks = BenchmarkConverter.TypeToBenchmarks(typeof(T));
            Assert.NotEmpty(typeToBenchmarks.BenchmarksCases);

            var validationErrors = ParamsValidator.FailOnError.Validate(typeToBenchmarks).ToList();
            output.WriteLine("Number of validation errors: " + validationErrors.Count);
            foreach (var error in validationErrors)
                output.WriteLine("* " + error.Message);

            Assert.Single(validationErrors);
            foreach (string messagePart in messageParts)
                Assert.Contains(messagePart, validationErrors.Single().Message);
        }

        private const string P = "[Params]";
        private const string Pa = "[ParamsAllValues]";
        private const string Ps = "[ParamsSource]";

        [Fact] public void Const1Test() => Check<Const1>(nameof(Const1.Input), "constant", P);
        [Fact] public void Const2Test() => Check<Const2>(nameof(Const2.Input), "constant", Pa);
        [Fact] public void Const3Test() => Check<Const3>(nameof(Const3.Input), "constant", Ps);
        [Fact] public void StaticReadonly1Test() => Check<StaticReadonly1>(nameof(StaticReadonly1.Input), "readonly", P);
        [Fact] public void StaticReadonly2Test() => Check<StaticReadonly2>(nameof(StaticReadonly2.Input), "readonly", Pa);
        [Fact] public void StaticReadonly3Test() => Check<StaticReadonly3>(nameof(StaticReadonly3.Input), "readonly", Ps);
        [Fact] public void NonStaticReadonly1Test() => Check<NonStaticReadonly1>(nameof(NonStaticReadonly1.Input), "readonly", P);
        [Fact] public void NonStaticReadonly2Test() => Check<NonStaticReadonly2>(nameof(NonStaticReadonly2.Input), "readonly", Pa);
        [Fact] public void NonStaticReadonly3Test() => Check<NonStaticReadonly3>(nameof(NonStaticReadonly3.Input), "readonly", Ps);
        [Fact] public void FieldMultiple1Test() => Check<FieldMultiple1>(nameof(FieldMultiple1.Input), "single attribute", P, Pa);
        [Fact] public void FieldMultiple2Test() => Check<FieldMultiple2>(nameof(FieldMultiple2.Input), "single attribute", P, Ps);
        [Fact] public void FieldMultiple3Test() => Check<FieldMultiple3>(nameof(FieldMultiple3.Input), "single attribute", Pa, Ps);
        [Fact] public void FieldMultiple4Test() => Check<FieldMultiple4>(nameof(FieldMultiple4.Input), "single attribute", P, Pa, Ps);
        [Fact] public void PropMultiple1Test() => Check<PropMultiple1>(nameof(PropMultiple1.Input), "single attribute", P, Pa);
        [Fact] public void PropMultiple2Test() => Check<PropMultiple2>(nameof(PropMultiple2.Input), "single attribute", P, Ps);
        [Fact] public void PropMultiple3Test() => Check<PropMultiple3>(nameof(PropMultiple3.Input), "single attribute", Pa, Ps);
        [Fact] public void PropMultiple4Test() => Check<PropMultiple4>(nameof(PropMultiple4.Input), "single attribute", P, Pa, Ps);
        [Fact] public void PrivateSetter1Test() => Check<PrivateSetter1>(nameof(PrivateSetter1.Input), "setter is not public", P);
        [Fact] public void PrivateSetter2Test() => Check<PrivateSetter2>(nameof(PrivateSetter2.Input), "setter is not public", Pa);
        [Fact] public void PrivateSetter3Test() => Check<PrivateSetter3>(nameof(PrivateSetter3.Input), "setter is not public", Ps);
        [Fact] public void NoSetter1Test() => Check<NoSetter1>(nameof(NoSetter1.Input), "no setter", P);
        [Fact] public void NoSetter2Test() => Check<NoSetter2>(nameof(NoSetter2.Input), "no setter", Pa);
        [Fact] public void NoSetter3Test() => Check<NoSetter3>(nameof(NoSetter3.Input), "no setter", Ps);
        [Fact] public void InternalField1Test() => Check<InternalField1>(nameof(InternalField1.Input), "it's not public", P);
        [Fact] public void InternalField2Test() => Check<InternalField2>(nameof(InternalField2.Input), "it's not public", Pa);
        [Fact] public void InternalField3Test() => Check<InternalField3>(nameof(InternalField3.Input), "it's not public", Ps);
        [Fact] public void InternalProp1Test() => Check<InternalProp1>(nameof(InternalProp1.Input), "setter is not public", P);
        [Fact] public void InternalProp2Test() => Check<InternalProp2>(nameof(InternalProp2.Input), "setter is not public", Pa);
        [Fact] public void InternalProp3Test() => Check<InternalProp3>(nameof(InternalProp3.Input), "setter is not public", Ps);

        public class Base
        {
            [Benchmark]
            public void Foo() { }

            public static IEnumerable<bool> Source() => new[] { false, true };
        }

        public class Const1 : Base
        {
            [Params(false, true)]
            public const bool Input = false;
        }

        public class Const2 : Base
        {
            [ParamsAllValues]
            public const bool Input = false;
        }

        public class Const3 : Base
        {
            [ParamsSource(nameof(Source))]
            public const bool Input = false;
        }

        public class StaticReadonly1 : Base
        {
            [Params(false, true)]
            public static readonly bool Input = false;
        }

        public class StaticReadonly2 : Base
        {
            [ParamsAllValues]
            public static readonly bool Input = false;
        }

        public class StaticReadonly3 : Base
        {
            [ParamsSource(nameof(Source))]
            public static readonly bool Input = false;
        }

        public class NonStaticReadonly1 : Base
        {
            [Params(false, true)]
            public readonly bool Input = false;
        }

        public class NonStaticReadonly2 : Base
        {
            [ParamsAllValues]
            public readonly bool Input = false;
        }

        public class NonStaticReadonly3 : Base
        {
            [ParamsSource(nameof(Source))]
            public readonly bool Input = false;
        }

        public class PrivateSetter1 : Base
        {
            [Params(false, true)]
            public bool Input { get; private set; }
        }

        public class PrivateSetter2 : Base
        {
            [ParamsAllValues]
            public bool Input { get; private set; }
        }

        public class PrivateSetter3 : Base
        {
            [ParamsSource(nameof(Source))]
            public bool Input { get; private set; }
        }

        public class NoSetter1 : Base
        {
            [Params(false, true)]
            public bool Input { get; } = false;
        }

        public class NoSetter2 : Base
        {
            [ParamsAllValues]
            public bool Input { get; } = false;
        }

        public class NoSetter3 : Base
        {
            [ParamsSource(nameof(Source))]
            public bool Input { get; } = false;
        }

        public class InternalField1 : Base
        {
            [Params(false, true)]
            internal bool Input = false;
        }

        public class InternalField2 : Base
        {
            [ParamsAllValues]
            internal bool Input = false;
        }

        public class InternalField3 : Base
        {
            [ParamsSource(nameof(Source))]
            internal bool Input = false;
        }

        public class InternalProp1 : Base
        {
            [Params(false, true)]
            internal bool Input { get; set; }
        }

        public class InternalProp2 : Base
        {
            [ParamsAllValues]
            internal bool Input { get; set; }
        }

        public class InternalProp3 : Base
        {
            [ParamsSource(nameof(Source))]
            internal bool Input { get; set; }
        }

        public class FieldMultiple1 : Base
        {
            [Params(false, true)]
            [ParamsAllValues]
            public bool Input = false;
        }

        public class FieldMultiple2 : Base
        {
            [Params(false, true)]
            [ParamsSource(nameof(Source))]
            public bool Input = false;
        }

        public class FieldMultiple3 : Base
        {
            [ParamsAllValues]
            [ParamsSource(nameof(Source))]
            public bool Input = false;
        }

        public class FieldMultiple4 : Base
        {
            [Params(false, true)]
            [ParamsAllValues]
            [ParamsSource(nameof(Source))]
            public bool Input = false;
        }

        public class PropMultiple1 : Base
        {
            [Params(false, true)]
            [ParamsAllValues]
            public bool Input { get; set; }
        }

        public class PropMultiple2 : Base
        {
            [Params(false, true)]
            [ParamsSource(nameof(Source))]
            public bool Input { get; set; }
        }

        public class PropMultiple3 : Base
        {
            [ParamsAllValues]
            [ParamsSource(nameof(Source))]
            public bool Input { get; set; }
        }

        public class PropMultiple4 : Base
        {
            [Params(false, true)]
            [ParamsAllValues]
            [ParamsSource(nameof(Source))]
            public bool Input { get; set; }
        }

#if NET5_0_OR_GREATER

        [Fact] public void InitOnly1Test() => Check<InitOnly1>(nameof(InitOnly1.Input), "init-only", P);
        [Fact] public void InitOnly2Test() => Check<InitOnly2>(nameof(InitOnly2.Input), "init-only", Pa);
        [Fact] public void InitOnly3Test() => Check<InitOnly3>(nameof(InitOnly3.Input), "init-only", Ps);

        public class InitOnly1 : Base
        {
            [Params(false, true)]
            public bool Input { get; init; }
        }

        public class InitOnly2 : Base
        {
            [ParamsAllValues]
            public bool Input { get; init; }
        }

        public class InitOnly3 : Base
        {
            [ParamsSource(nameof(Source))]
            public bool Input { get; init; }
        }

#endif
    }
}