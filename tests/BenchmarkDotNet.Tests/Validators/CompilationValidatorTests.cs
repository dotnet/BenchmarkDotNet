using System;
using System.Linq;
using System.Reflection.Emit;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Validators;
using Xunit;
using System.Reflection;

namespace BenchmarkDotNet.Tests.Validators
{
    public class CompilationValidatorTests
    {
        [Fact]
        public void BenchmarkedMethodNameMustNotContainWhitespaces()
        {
            Delegate method = BuildDummyMethod<int>("Has Some Whitespaces");

            var config = new ManualConfig().CreateImmutableConfig();
            var parameters = new ValidationParameters(
                new[]
                {
                    BenchmarkCase.Create(
                        new Descriptor(typeof(CompilationValidatorTests), method.Method),
                        Job.Dry,
                        null,
                        config
                        )
                }, config);

            var errors = CompilationValidator.FailOnError.Validate(parameters).Select(e => e.Message);

            Assert.Contains(errors,
                s => s.Equals(
                    "Benchmarked method `Has Some Whitespaces` contains illegal character(s) or uses C# keyword. Please use `[<Benchmark(Description = \"Custom name\")>]` to set custom display name."));
        }

        [Fact]
        public void BenchmarkedMethodNameMustNotUseCsharpKeywords()
        {
            Delegate method = BuildDummyMethod<int>("typeof");

            var config = ManualConfig.CreateEmpty().CreateImmutableConfig();
            var parameters = new ValidationParameters(
                new[]
                {
                    BenchmarkCase.Create(
                        new Descriptor(typeof(CompilationValidatorTests), method.Method),
                        Job.Dry,
                        null,
                        config)
                }, config);

            var errors = CompilationValidator.FailOnError.Validate(parameters).Select(e => e.Message);

            Assert.Contains(errors,
                s => s.Equals(
                    "Benchmarked method `typeof` contains illegal character(s) or uses C# keyword. Please use `[<Benchmark(Description = \"Custom name\")>]` to set custom display name."));
        }

        [Theory]
        /* BenchmarkDotNet can only benchmark public unsealed classes*/
        [InlineData(typeof(BenchMarkPublicClass), false)]
        [InlineData(typeof(BenchMarkPublicClass.PublicNestedClass), false)]
        [InlineData(typeof(SealedClass.PublicNestedClass), false)]
        [InlineData(typeof(OuterClass.PublicNestedClass), true)]
        [InlineData(typeof(SealedClass), true)]
        [InlineData(typeof(MyPrivateClass), true)]
        [InlineData(typeof(MyPublicProtectedClass), true)]
        [InlineData(typeof(MyPrivateProtectedClass), true)]
        [InlineData(typeof(MyProtectedInternalClass), true)]
        [InlineData(typeof(MyInternalClass), true)]
        [InlineData(typeof(OuterClass), true)]
        [InlineData(typeof(OuterClass.InternalNestedClass), true)]
        [InlineData(typeof(BenchMarkPublicClass.InternalNestedClass), true)]
        /* Generics Remaining */
        public void Benchmark_Class_Modifers_Must_Be_Public(Type type, bool hasErrors)
        {
            var validationErrors = CompilationValidator.FailOnError.Validate(BenchmarkConverter.TypeToBenchmarks(type));

            Assert.Equal(hasErrors, validationErrors.Any());
        }

        [Theory]
        [InlineData(typeof(BenchmarkClassWithStaticMethod), true)]
        [InlineData(typeof(BenchmarkClass<PublicClass>), false)]
        public void Benchmark_Class_Methods_Must_Be_Non_Static(Type type, bool hasErrors)
        {
            var validationErrors = CompilationValidator.FailOnError.Validate(BenchmarkConverter.TypeToBenchmarks(type));

            Assert.Equal(hasErrors, validationErrors.Any());
        }

        [Theory]
        [InlineData(typeof(PublicClass), false)]
        [InlineData(typeof(PublicClass.PublicNestedClass), false)]
        [InlineData(typeof(PrivateClass), true)]
        [InlineData(typeof(PrivateNestedClass), true)]
        [InlineData(typeof(InternalClass), true)]
        [InlineData(typeof(InternalClass.InternalNestedClass), true)]
        [InlineData(typeof(PrivateProtectedClass), true)]
        [InlineData(typeof(PrivateProtectedNestedClass), true)]
        [InlineData(typeof(ProtectedInternalClass), true)]
        [InlineData(typeof(ProtectedInternalClass.ProtectedInternalNestedClass), true)]
        public void Benchmark_Class_Generic_Argument_Must_Be_Public(Type type, bool hasErrors)
        {
            // Arrange
            var constructed = typeof(BenchmarkClass<>).MakeGenericType(type);

            // Act
            var validationErrors = CompilationValidator.FailOnError.Validate(BenchmarkConverter.TypeToBenchmarks(constructed))
                                                               .ToList();

            // Assert
            Assert.Equal(hasErrors, validationErrors.Any());
        }

        private static Delegate BuildDummyMethod<T>(string name)
        {
            var dynamicMethod = new DynamicMethod(
                name,
                returnType: typeof(T),
                parameterTypes: new[] { typeof(T) });

            var cilGenerator = dynamicMethod.GetILGenerator();
            cilGenerator.Emit(OpCodes.Ldarg_0); // load the argument
            cilGenerator.Emit(OpCodes.Ret); // return whatever it is

            return dynamicMethod.CreateDelegate(typeof(Func<T, T>));
        }

        private class PrivateNestedClass { }
        private protected class PrivateProtectedNestedClass { }
        private class PrivateClass { }
        private protected class PrivateProtectedClass { }
        protected internal class ProtectedInternalClass
        {
            protected internal class ProtectedInternalNestedClass { }
        }

        private class MyPrivateClass{ [Benchmark] public void PublicMethod(){} }

        protected class MyPublicProtectedClass{ [Benchmark] public void PublicMethod(){} }

        private protected class MyPrivateProtectedClass{ [Benchmark] public void PublicMethod(){} }

        internal class MyInternalClass{ [Benchmark] public void PublicMethod(){} }

        protected internal class MyProtectedInternalClass{ [Benchmark] public void PublicMethod() { } }
        }

    public class BenchmarkClassWithStaticMethod
    {
        [Benchmark]
        public static void StaticMethod() { }
    }

    public class BenchmarkClass<T> where T : new()
    {
        [Benchmark]
        public T New() => new T();
    }

    public class PublicClass
    {
        public class PublicNestedClass { }
    }

    internal class InternalClass
    {
        internal class InternalNestedClass { }
    }

    public sealed class SealedClass
    {
        [Benchmark] public void PublicMethod() { }

        public class PublicNestedClass { [Benchmark] public void PublicMethod() { } }
    }

    internal class OuterClass
    {
        [Benchmark] public void PublicMethod(){}

        internal class InternalNestedClass { [Benchmark] public void PublicMethod() { } }

        public class PublicNestedClass { [Benchmark] public void PublicMethod() { } }
    }

    public class BenchMarkPublicClass
    {
        [Benchmark] public void PublicMethod(){}

        public class PublicNestedClass { [Benchmark] public void PublicMethod() { } }

        internal class InternalNestedClass { [Benchmark] public void PublicMethod() { } }
    }
}
