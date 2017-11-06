using System;
using System.Collections.Generic;
using BenchmarkDotNet.Attributes;
using Xunit;
using Xunit.Abstractions;

namespace BenchmarkDotNet.IntegrationTests
{
    public class ArgumentsAttributeTests : BenchmarkTestExecutor
    {
        public ArgumentsAttributeTests(ITestOutputHelper output) : base(output) { }

        [Fact]
        public void ArgumentsArePassedToBenchmarks() => CanExecute<WithArguments>();

        [Fact]
        public void ArgumentsFromSourceArePassedToBenchmarks() => CanExecute<WithArgumentsSource>();
    }

    public class WithArguments
    {
        [Benchmark]
        [Arguments(true, 1)]
        [Arguments(false, 2)]
        public void Simple(bool boolean, int number)
        {
            if(boolean && number != 1 || !boolean && number != 2)
                throw new InvalidOperationException("Incorrect values were passed");
        }
    }

    public class WithArgumentsSource
    {
        [Benchmark]
        [ArgumentsSource(nameof(ArgumentsProvider))]
        public void Simple(bool boolean, int number)
        {
            if (boolean && number != 1 || !boolean && number != 2)
                throw new InvalidOperationException("Incorrect values were passed");
        }

        public IEnumerable<object[]> ArgumentsProvider()
        {
            yield return new object[] { true, 1};
            yield return new object[] { false, 2 };
        }
    }

}