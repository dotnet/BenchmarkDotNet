using System;
using System.Collections.Generic;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Running;
using Xunit;

namespace BenchmarkDotNet.IntegrationTests
{
    // Delibrately made the Property "static" to ensure that Params also work okay in this scenario
    [Config(typeof(SingleRunFastConfig))]
    public class ParamsTestStaticProperty
    {
        [Params(1, 2)]
        public static int StaticParamProperty { get; set; }

        private static HashSet<int> collectedParams = new HashSet<int>();

        [Fact]
        public void Test()
        {
            var logger = new AccumulationLogger();
            var config = DefaultConfig.Instance.With(logger);
            BenchmarkTestExecutor.CanExecute<ParamsTestStaticProperty>(config);

            foreach (var param in new[] { 1, 2 })
                Assert.Contains($"// ### New Parameter {param} ###" + Environment.NewLine, logger.GetLog());
            Assert.DoesNotContain($"// ### New Parameter {default(int)} ###" + Environment.NewLine, logger.GetLog());
        }

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

    // Delibrately made the Property "static" to ensure that Params also work okay in this scenario
    public class ParamsTestStaticPrivatePropertyError
    {
        [Params(1, 2)]
        public static int StaticParamProperty { get; private set; }

        private static HashSet<int> collectedParams = new HashSet<int>();

        [Fact]
        public void Test()
        {
            // System.InvalidOperationException : Property "StaticParamProperty" must be public and writable if it has the [Params(..)] attribute applied to it
            Assert.Throws<InvalidOperationException>(() => BenchmarkTestExecutor.CanExecute<ParamsTestStaticPrivatePropertyError>());
        }

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

    // Delibrately made everything "static" (as well as using a Field) to ensure that Params also work okay in this scenario
    [Config(typeof(SingleRunFastConfig))]
    public class ParamsTestStaticField
    {
        [Params(1, 2)]
        public static int StaticParamField = 0;

        private static HashSet<int> collectedParams = new HashSet<int>();

        [Fact]
        public static void Test()
        {
            var logger = new AccumulationLogger();
            var config = DefaultConfig.Instance.With(logger);
            BenchmarkTestExecutor.CanExecute<ParamsTestStaticField>(config);
            foreach (var param in new[] { 1, 2 })
                Assert.Contains($"// ### New Parameter {param} ###" + Environment.NewLine, logger.GetLog());
            Assert.DoesNotContain($"// ### New Parameter 0 ###" + Environment.NewLine, logger.GetLog());
        }

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

    // Delibrately made everything "static" (as well as using a Field) to ensure that Params also work okay in this scenario
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
            Assert.Throws<InvalidOperationException>(() => BenchmarkTestExecutor.CanExecute<ParamsTestStaticPrivateFieldError>());
        }

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
