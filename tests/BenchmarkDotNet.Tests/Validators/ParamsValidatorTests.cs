using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;
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

        private async ValueTask Check<T>(params string[] messageParts)
        {
            var typeToBenchmarks = BenchmarkConverter.TypeToBenchmarks(typeof(T));
            Assert.NotEmpty(typeToBenchmarks.BenchmarksCases);

            var validationErrors = await ParamsValidator.FailOnError.ValidateAsync(typeToBenchmarks).ToArrayAsync();
            output.WriteLine("Number of validation errors: " + validationErrors.Length);
            foreach (var error in validationErrors)
                output.WriteLine("* " + error.Message);

            Assert.Single(validationErrors);
            foreach (string messagePart in messageParts)
                Assert.Contains(messagePart, validationErrors.Single().Message);
        }

        private const string P = "[Params]";
        private const string Pa = "[ParamsAllValues]";
        private const string Ps = "[ParamsSource]";

        [Fact] public async Task Const1Test() => await Check<Const1>(nameof(Const1.Input), "constant", P);
        [Fact] public async Task Const2Test() => await Check<Const2>(nameof(Const2.Input), "constant", Pa);
        [Fact] public async Task Const3Test() => await Check<Const3>(nameof(Const3.Input), "constant", Ps);
        [Fact] public async Task StaticReadonly1Test() => await Check<StaticReadonly1>(nameof(StaticReadonly1.Input), "readonly", P);
        [Fact] public async Task StaticReadonly2Test() => await Check<StaticReadonly2>(nameof(StaticReadonly2.Input), "readonly", Pa);
        [Fact] public async Task StaticReadonly3Test() => await Check<StaticReadonly3>(nameof(StaticReadonly3.Input), "readonly", Ps);
        [Fact] public async Task NonStaticReadonly1Test() => await Check<NonStaticReadonly1>(nameof(NonStaticReadonly1.Input), "readonly", P);
        [Fact] public async Task NonStaticReadonly2Test() => await Check<NonStaticReadonly2>(nameof(NonStaticReadonly2.Input), "readonly", Pa);
        [Fact] public async Task NonStaticReadonly3Test() => await Check<NonStaticReadonly3>(nameof(NonStaticReadonly3.Input), "readonly", Ps);
        [Fact] public async Task FieldMultiple1Test() => await Check<FieldMultiple1>(nameof(FieldMultiple1.Input), "single attribute", P, Pa);
        [Fact] public async Task FieldMultiple2Test() => await Check<FieldMultiple2>(nameof(FieldMultiple2.Input), "single attribute", P, Ps);
        [Fact] public async Task FieldMultiple3Test() => await Check<FieldMultiple3>(nameof(FieldMultiple3.Input), "single attribute", Pa, Ps);
        [Fact] public async Task FieldMultiple4Test() => await Check<FieldMultiple4>(nameof(FieldMultiple4.Input), "single attribute", P, Pa, Ps);
        [Fact] public async Task PropMultiple1Test() => await Check<PropMultiple1>(nameof(PropMultiple1.Input), "single attribute", P, Pa);
        [Fact] public async Task PropMultiple2Test() => await Check<PropMultiple2>(nameof(PropMultiple2.Input), "single attribute", P, Ps);
        [Fact] public async Task PropMultiple3Test() => await Check<PropMultiple3>(nameof(PropMultiple3.Input), "single attribute", Pa, Ps);
        [Fact] public async Task PropMultiple4Test() => await Check<PropMultiple4>(nameof(PropMultiple4.Input), "single attribute", P, Pa, Ps);
        [Fact] public async Task PrivateSetter1Test() => await Check<PrivateSetter1>(nameof(PrivateSetter1.Input), "setter is not public", P);
        [Fact] public async Task PrivateSetter2Test() => await Check<PrivateSetter2>(nameof(PrivateSetter2.Input), "setter is not public", Pa);
        [Fact] public async Task PrivateSetter3Test() => await Check<PrivateSetter3>(nameof(PrivateSetter3.Input), "setter is not public", Ps);
        [Fact] public async Task NoSetter1Test() => await Check<NoSetter1>(nameof(NoSetter1.Input), "no setter", P);
        [Fact] public async Task NoSetter2Test() => await Check<NoSetter2>(nameof(NoSetter2.Input), "no setter", Pa);
        [Fact] public async Task NoSetter3Test() => await Check<NoSetter3>(nameof(NoSetter3.Input), "no setter", Ps);
        [Fact] public async Task InternalField1Test() => await Check<InternalField1>(nameof(InternalField1.Input), "it's not public", P);
        [Fact] public async Task InternalField2Test() => await Check<InternalField2>(nameof(InternalField2.Input), "it's not public", Pa);
        [Fact] public async Task InternalField3Test() => await Check<InternalField3>(nameof(InternalField3.Input), "it's not public", Ps);
        [Fact] public async Task InternalProp1Test() => await Check<InternalProp1>(nameof(InternalProp1.Input), "setter is not public", P);
        [Fact] public async Task InternalProp2Test() => await Check<InternalProp2>(nameof(InternalProp2.Input), "setter is not public", Pa);
        [Fact] public async Task InternalProp3Test() => await Check<InternalProp3>(nameof(InternalProp3.Input), "setter is not public", Ps);

        public class Base
        {
            [Benchmark]
            public void Foo() { }

            public static IEnumerable<bool> Source() => new[] { false, true };
        }

#pragma warning disable BDN1205
        public class Const1 : Base
        {
            [Params(false, true)]
            public const bool Input = false;
        }
#pragma warning restore BDN1205

#pragma warning disable BDN1205
        public class Const2 : Base
        {
            [ParamsAllValues]
            public const bool Input = false;
        }
#pragma warning restore BDN1205

#pragma warning disable BDN1205
        public class Const3 : Base
        {
            [ParamsSource(nameof(Source))]
            public const bool Input = false;
        }
#pragma warning restore BDN1205

#pragma warning disable BDN1204
        public class StaticReadonly1 : Base
        {
            [Params(false, true)]
            public static readonly bool Input = false;
        }
#pragma warning restore BDN1204

#pragma warning disable BDN1204
        public class StaticReadonly2 : Base
        {
            [ParamsAllValues]
            public static readonly bool Input = false;
        }
#pragma warning restore BDN1204

#pragma warning disable BDN1204
        public class StaticReadonly3 : Base
        {
            [ParamsSource(nameof(Source))]
            public static readonly bool Input = false;
        }
#pragma warning restore BDN1204

#pragma warning disable BDN1204
        public class NonStaticReadonly1 : Base
        {
            [Params(false, true)]
            public readonly bool Input = false;
        }
#pragma warning restore BDN1204

#pragma warning disable BDN1204
        public class NonStaticReadonly2 : Base
        {
            [ParamsAllValues]
            public readonly bool Input = false;
        }
#pragma warning restore BDN1204

#pragma warning disable BDN1204
        public class NonStaticReadonly3 : Base
        {
            [ParamsSource(nameof(Source))]
            public readonly bool Input = false;
        }
#pragma warning restore BDN1204

#pragma warning disable BDN1207
        public class PrivateSetter1 : Base
        {
            [Params(false, true)]
            public bool Input { get; private set; }
        }
#pragma warning restore BDN1207

#pragma warning disable BDN1207
        public class PrivateSetter2 : Base
        {
            [ParamsAllValues]
            public bool Input { get; private set; }
        }
#pragma warning restore BDN1207

#pragma warning disable BDN1207
        public class PrivateSetter3 : Base
        {
            [ParamsSource(nameof(Source))]
            public bool Input { get; private set; }
        }
#pragma warning restore BDN1207

#pragma warning disable BDN1207
        public class NoSetter1 : Base
        {
            [Params(false, true)]
            public bool Input { get; } = false;
        }
#pragma warning restore BDN1207

#pragma warning disable BDN1207
        public class NoSetter2 : Base
        {
            [ParamsAllValues]
            public bool Input { get; } = false;
        }
#pragma warning restore BDN1207

#pragma warning disable BDN1207
        public class NoSetter3 : Base
        {
            [ParamsSource(nameof(Source))]
            public bool Input { get; } = false;
        }
#pragma warning restore BDN1207

#pragma warning disable BDN1202
        public class InternalField1 : Base
        {
            [Params(false, true)]
            internal bool Input = false;
        }
#pragma warning restore BDN1202

#pragma warning disable BDN1202
        public class InternalField2 : Base
        {
            [ParamsAllValues]
            internal bool Input = false;
        }
#pragma warning restore BDN1202

#pragma warning disable BDN1202
        public class InternalField3 : Base
        {
            [ParamsSource(nameof(Source))]
            internal bool Input = false;
        }
#pragma warning restore BDN1202

#pragma warning disable BDN1203
        public class InternalProp1 : Base
        {
            [Params(false, true)]
            internal bool Input { get; set; }
        }
#pragma warning restore BDN1203

#pragma warning disable BDN1203
        public class InternalProp2 : Base
        {
            [ParamsAllValues]
            internal bool Input { get; set; }
        }
#pragma warning restore BDN1203

#pragma warning disable BDN1203
        public class InternalProp3 : Base
        {
            [ParamsSource(nameof(Source))]
            internal bool Input { get; set; }
        }
#pragma warning restore BDN1203

#pragma warning disable BDN1200
        public class FieldMultiple1 : Base
        {
            [Params(false, true)]
            [ParamsAllValues]
            public bool Input = false;
        }
#pragma warning restore BDN1200

#pragma warning disable BDN1200
        public class FieldMultiple2 : Base
        {
            [Params(false, true)]
            [ParamsSource(nameof(Source))]
            public bool Input = false;
        }
#pragma warning restore BDN1200

#pragma warning disable BDN1200
        public class FieldMultiple3 : Base
        {
            [ParamsAllValues]
            [ParamsSource(nameof(Source))]
            public bool Input = false;
        }
#pragma warning restore BDN1200

#pragma warning disable BDN1200
        public class FieldMultiple4 : Base
        {
            [Params(false, true)]
            [ParamsAllValues]
            [ParamsSource(nameof(Source))]
            public bool Input = false;
        }
#pragma warning restore BDN1200

#pragma warning disable BDN1201
        public class PropMultiple1 : Base
        {
            [Params(false, true)]
            [ParamsAllValues]
            public bool Input { get; set; }
        }
#pragma warning restore BDN1201

#pragma warning disable BDN1201
        public class PropMultiple2 : Base
        {
            [Params(false, true)]
            [ParamsSource(nameof(Source))]
            public bool Input { get; set; }
        }
#pragma warning restore BDN1201

#pragma warning disable BDN1201
        public class PropMultiple3 : Base
        {
            [ParamsAllValues]
            [ParamsSource(nameof(Source))]
            public bool Input { get; set; }
        }
#pragma warning restore BDN1201

#pragma warning disable BDN1201
        public class PropMultiple4 : Base
        {
            [Params(false, true)]
            [ParamsAllValues]
            [ParamsSource(nameof(Source))]
            public bool Input { get; set; }
        }
#pragma warning restore BDN1201

#if NET5_0_OR_GREATER

        [Fact] public async Task InitOnly1Test() => await Check<InitOnly1>(nameof(InitOnly1.Input), "init-only", P);
        [Fact] public async Task InitOnly2Test() => await Check<InitOnly2>(nameof(InitOnly2.Input), "init-only", Pa);
        [Fact] public async Task InitOnly3Test() => await Check<InitOnly3>(nameof(InitOnly3.Input), "init-only", Ps);

#pragma warning disable BDN1206
        public class InitOnly1 : Base
        {
            [Params(false, true)]
            public bool Input { get; init; }
        }
#pragma warning restore BDN1206

#pragma warning disable BDN1206
        public class InitOnly2 : Base
        {
            [ParamsAllValues]
            public bool Input { get; init; }
        }
#pragma warning restore BDN1206

#pragma warning disable BDN1206
        public class InitOnly3 : Base
        {
            [ParamsSource(nameof(Source))]
            public bool Input { get; init; }
        }
#pragma warning restore BDN1206

#endif
    }
}