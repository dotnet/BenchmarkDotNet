using System;
using System.Collections.Generic;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Running;
using Xunit;

namespace BenchmarkDotNet.IntegrationTests
{
    [Config(typeof(SingleRunFastConfig))]
    public class ParamsTestProperty
    {
        [Params(1, 2)]
        public int ParamProperty { get; set; }

        private HashSet<int> collectedParams = new HashSet<int>();

        [Fact]
        public void Test()
        {
            var logger = new AccumulationLogger();
            var config = DefaultConfig.Instance.With(logger);
            BenchmarkTestExecutor.CanExecute<ParamsTestProperty>(config);
            foreach (var param in new[] { 1, 2 })
                Assert.Contains($"// ### New Parameter {param} ###" + Environment.NewLine, logger.GetLog());
            Assert.DoesNotContain($"// ### New Parameter {default(int)} ###" + Environment.NewLine, logger.GetLog());
        }

        [Benchmark]
        public void Benchmark()
        {
            if (collectedParams.Contains(ParamProperty) == false)
            {
                Console.WriteLine($"// ### New Parameter {ParamProperty} ###");
                collectedParams.Add(ParamProperty);
            }
        }
    }

    public class ParamsTestPrivatePropertyError
    {
        [Params(1, 2)]
        public int ParamProperty { get; private set; }

        private HashSet<int> collectedParams = new HashSet<int>();

        [Fact]
        public void Test()
        {
            // System.InvalidOperationException : Property "ParamProperty" must be public and writable if it has the [Params(..)] attribute applied to it
            Assert.Throws<InvalidOperationException>(() => BenchmarkTestExecutor.CanExecute<ParamsTestPrivatePropertyError>());
        }

        [Benchmark]
        public void Benchmark()
        {
            if (collectedParams.Contains(ParamProperty) == false)
            {
                Console.WriteLine($"// ### New Parameter {ParamProperty} ###");
                collectedParams.Add(ParamProperty);
            }
        }
    }

    [Config(typeof(SingleRunFastConfig))]
    public class ParamsTestField
    {
        [Params(1, 2)]
        public int ParamField = 0;

        private HashSet<int> collectedParams = new HashSet<int>();

        [Fact]
        public void Test()
        {
            var logger = new AccumulationLogger();
            var config = DefaultConfig.Instance.With(logger);
            BenchmarkTestExecutor.CanExecute<ParamsTestField>(config);
            foreach (var param in new[] { 1, 2 })
                Assert.Contains($"// ### New Parameter {param} ###" + Environment.NewLine, logger.GetLog());
            Assert.DoesNotContain($"// ### New Parameter 0 ###" + Environment.NewLine, logger.GetLog());
        }

        [Benchmark]
        public void Benchmark()
        {
            if (collectedParams.Contains(ParamField) == false)
            {
                Console.WriteLine($"// ### New Parameter {ParamField} ###");
                collectedParams.Add(ParamField);
            }
        }
    }

    public class ParamsTestPrivateFieldError
    {
        [Params(1, 2)]
        private int ParamField = 0;

        private HashSet<int> collectedParams = new HashSet<int>();

        [Fact]
        public void Test()
        {
            // System.InvalidOperationException : Field "ParamField" must be public if it has the [Params(..)] attribute applied to it
            Assert.Throws<InvalidOperationException>(() => BenchmarkTestExecutor.CanExecute<ParamsTestPrivateFieldError>());
        }

        [Benchmark]
        public void Benchmark()
        {
            if (collectedParams.Contains(ParamField) == false)
            {
                Console.WriteLine($"// ### New Parameter {ParamField} ###");
                collectedParams.Add(ParamField);
            }
        }
    }

    public class NestedEnumsAsParams
    {
        public enum NestedOne
        {
            SampleValue = 1234
        }

        [Params(NestedOne.SampleValue)]
        public NestedOne Field;

        [Fact]
        public void AreSupported()
        {
            BenchmarkTestExecutor.CanExecute<NestedEnumsAsParams>();
        }

        [Benchmark]
        public NestedOne Benchmark()
        {
            return Field;
        }
    }

    public class CharactersAsParams
    {
        [Params('*')]
        public char Field;

        [Fact]
        public void AreSupported()
        {
            BenchmarkTestExecutor.CanExecute<CharactersAsParams>();
        }

        [Benchmark]
        public char Benchmark()
        {
            return Field;
        }
    }
}
