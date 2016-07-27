using System;
using System.Linq;
using System.Reflection;
using Xunit;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Code;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Running;

namespace BenchmarkDotNet.Tests
{
    public class CodeGeneratorTests
    {
        [Fact]
        public void AsyncVoidIsNotSupported()
        {
            var asyncVoidMethod =
                typeof(CodeGeneratorTests)
                    .GetTypeInfo()
                    .GetMethods()
                    .Single(method => method.Name == "AsyncVoidMethod");

            var target = new Target(typeof(CodeGeneratorTests), asyncVoidMethod);
            var benchmark = new Benchmark(target, Job.Default, null);

            Assert.Throws<NotSupportedException>(() => CodeGenerator.Generate(benchmark));
        }

        [Benchmark]
        public async void AsyncVoidMethod() { }
    }
}