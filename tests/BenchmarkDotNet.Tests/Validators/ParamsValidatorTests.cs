using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Validators;
using Xunit;
using Xunit.Abstractions;

namespace BenchmarkDotNet.Tests.Validators
{
    [SuppressMessage("ReSharper", "ClassNeverInstantiated.Global")]
    [SuppressMessage("ReSharper", "MemberCanBePrivate.Global")]
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
    }
}