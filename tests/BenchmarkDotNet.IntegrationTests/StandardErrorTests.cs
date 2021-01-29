using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Tests.Loggers;
using System;
using Xunit;
using Xunit.Abstractions;

namespace BenchmarkDotNet.IntegrationTests
{
    public class StandardErrorTests : BenchmarkTestExecutor
    {
        private const string ErrorMessage = "ErrorMessage";

        public StandardErrorTests(ITestOutputHelper output) : base(output)
        {
        }

        [Fact]
        public void BenchmarkCanWriteToStandardError() => CanExecute<WritingToStandardError>();

        public class WritingToStandardError
        {
            [Benchmark]
            public void Write() => Console.Error.Write(new string('a', 10_000)); // the text needs to be big enough to hit the deadlock
        }

        [Fact]
        public void ExceptionMessageIsNotLost()
        {
            var logger = new OutputLogger(Output);
            var config = CreateSimpleConfig(logger);

            CanExecute<ThrowingException>(config, fullValidation: false);

            Assert.Contains(ErrorMessage, logger.GetLog());
        }

        public class ThrowingException
        {
            [Benchmark]
            public void Write() => throw new Exception(ErrorMessage);
        }
    }
}
