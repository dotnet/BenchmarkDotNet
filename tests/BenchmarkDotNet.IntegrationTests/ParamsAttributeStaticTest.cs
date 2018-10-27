using System;
using System.Collections.Generic;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Tests.Loggers;
using Xunit;
using Xunit.Abstractions;

namespace BenchmarkDotNet.IntegrationTests
{
    public class ParamsTestStaticPropertyTest : BenchmarkTestExecutor
    {
        public ParamsTestStaticPropertyTest(ITestOutputHelper output) : base(output) { }

        [Fact]
        public void Test()
        {
            var logger = new OutputLogger(Output);
            var config = CreateSimpleConfig(logger);

            CanExecute<ParamsTestStaticProperty>(config);

            foreach (var param in new[] { 1, 2 })
                Assert.Contains($"// ### New Parameter {param} ###" + Environment.NewLine, logger.GetLog());
            Assert.DoesNotContain($"// ### New Parameter {default(int)} ###" + Environment.NewLine, logger.GetLog());
        }

        public class ParamsTestStaticProperty
        {
            /// <summary>
            /// Deliberately made the Property "static" to ensure that Params also work okay in this scenario 
            /// </summary>
            [Params(1, 2)]
            public static int StaticParamProperty { get; set; }

            private static HashSet<int> collectedParams = new HashSet<int>();

            [Benchmark]
            public void Benchmark()
            {
                if (collectedParams.Contains(StaticParamProperty) == false)
                {
                    Console.WriteLine($"// ### New Parameter {StaticParamProperty} ###");
                    collectedParams.Add(StaticParamProperty);
                }
            }
        }
    }
    
    public class ParamsTestStaticPrivatePropertyError : BenchmarkTestExecutor
    {
        public ParamsTestStaticPrivatePropertyError(ITestOutputHelper output) : base(output) { }

        /// <summary>
        /// Deliberately made the Property "static" to ensure that Params also work okay in this scenario
        /// </summary>
        [Params(1, 2)]
        public static int StaticParamProperty { get; private set; }

        private static HashSet<int> collectedParams = new HashSet<int>();

        [Fact]
        public void Test()
        {
            // System.InvalidOperationException : Property "StaticParamProperty" must be public and writable if it has the [Params(..)] attribute applied to it
            Assert.Throws<InvalidOperationException>(() => CanExecute<ParamsTestStaticPrivatePropertyError>());
        }

#pragma warning disable xUnit1013 // Public method should be marked as test
        [Benchmark]
        public void Benchmark()
        {
            if (collectedParams.Contains(StaticParamProperty) == false)
            {
                Console.WriteLine($"// ### New Parameter {StaticParamProperty} ###");
                collectedParams.Add(StaticParamProperty);
            }
        }
#pragma warning restore xUnit1013 // Public method should be marked as test
    }

    // Deliberately made everything "static" (as well as using a Field) to ensure that Params also work okay in this scenario
    public class ParamsTestStaticFieldTest : BenchmarkTestExecutor
    {
        public ParamsTestStaticFieldTest(ITestOutputHelper output) : base(output)
        {
        }

        [Fact]
        public void Test()
        {
            var logger = new OutputLogger(Output);
            var config = CreateSimpleConfig(logger);

            CanExecute<ParamsTestStaticField>(config);

            foreach (var param in new[] { 1, 2 })
                Assert.Contains($"// ### New Parameter {param} ###" + Environment.NewLine, logger.GetLog());
            Assert.DoesNotContain($"// ### New Parameter 0 ###" + Environment.NewLine, logger.GetLog());
        }

        public class ParamsTestStaticField
        {
            [Params(1, 2)]
            public static int StaticParamField = 0;

            private static HashSet<int> collectedParams = new HashSet<int>();

            [Benchmark]
            public static void Benchmark()
            {
                if (collectedParams.Contains(StaticParamField) == false)
                {
                    Console.WriteLine($"// ### New Parameter {StaticParamField} ###");
                    collectedParams.Add(StaticParamField);
                }
            }
        }
    }

    // Deliberately made everything "static" (as well as using a Field) to ensure that Params also work okay in this scenario
    [Config(typeof(SingleRunFastConfig))]
    public class ParamsTestStaticPrivateFieldError
    {
        [Params(1, 2)]
        private static int StaticParamField = 0;

        private static HashSet<int> collectedParams = new HashSet<int>();

        [Fact]
        public static void Test()
        {
            // System.InvalidOperationException : Field "StaticParamField" must be public if it has the [Params(..)] attribute applied to it
            Assert.Throws<InvalidOperationException>(() => new BenchmarkTestExecutor().CanExecute<ParamsTestStaticPrivateFieldError>());
        }

#pragma warning disable xUnit1013 // Public method should be marked as test
        [Benchmark]
        public static void Benchmark()
        {
            if (collectedParams.Contains(StaticParamField) == false)
            {
                Console.WriteLine($"// ### New Parameter {StaticParamField} ###");
                collectedParams.Add(StaticParamField);
            }
        }
#pragma warning restore xUnit1013 // Public method should be marked as test
    }
}
