using System;
using System.Linq;
using System.Reflection.Emit;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Validators;
using Xunit;

namespace BenchmarkDotNet.Tests.Validators
{
    public class CompilationValidatorTests
    {
        [Fact]
        public void BenchmarkedMethodNameMustNotContainWhitespaces()
        {
            Delegate method = BuildDummyMethod<int>("Has Some Whitespaces");

            var parameters = new ValidationParameters(
                new[]
                {
                    new Benchmark(
                        new Target(
                            typeof(CompilationValidatorTests),
                            method.Method),
                        Job.Dry,
                        null)
                }, new ManualConfig());

            var errors = CompilationValidator.Default.Validate(parameters);

            Assert.Equal("Benchmarked method `Has Some Whitespaces` contains illegal character(s). Please use `[<Benchmark(Description = \"Custom name\")>]` to set custom display name.", 
                errors.Single().Message);
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
            var validationErrors = CompilationValidator.Default.Validate(BenchmarkConverter.TypeToBenchmarks(constructed))
                                                               .ToList();
            
            // Assert
            Assert.Equal(validationErrors.Any(), hasErrors);
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
        
    
}
