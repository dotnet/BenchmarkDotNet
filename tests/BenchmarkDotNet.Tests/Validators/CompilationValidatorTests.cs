using System;
using System.Linq;
using System.Reflection.Emit;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Running;
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

        public static Delegate BuildDummyMethod<T>(string name)
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
    }
}
