using System;
using System.Linq;
using System.Reflection;
using Xunit;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Code;
using BenchmarkDotNet.Configs;
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
            var benchmark = BenchmarkCase.Create(target, Job.Default, null);

            Assert.Throws<NotSupportedException>(() => CodeGenerator.Generate(new BuildPartition(new[] { new BenchmarkBuildInfo(benchmark, ManualConfig.CreateEmpty().AsReadOnly(), 0) }, BenchmarkRunner.DefaultResolver)));
        }


#pragma warning disable CS1998
#pragma warning disable xUnit1013 // Public method should be marked as test
        [Benchmark]
        public async void AsyncVoidMethod() { }
#pragma warning restore xUnit1013 // Public method should be marked as test
    }
}