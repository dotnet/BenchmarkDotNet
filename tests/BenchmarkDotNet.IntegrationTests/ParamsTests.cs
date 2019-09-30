using System;
using System.Collections.Generic;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Tests.Loggers;
using Xunit;
using Xunit.Abstractions;

namespace BenchmarkDotNet.IntegrationTests
{
    public class ParamsTests : BenchmarkTestExecutor
    {
        public ParamsTests(ITestOutputHelper output) : base(output) { }

        [Fact]
        public void ParamsSupportPropertyWithPublicSetter()
        {
            var logger = new OutputLogger(Output);
            var config = CreateSimpleConfig(logger);

            CanExecute<ParamsTestProperty>(config);
            foreach (var param in new[] { 1, 2 })
                Assert.Contains($"// ### New Parameter {param} ###" + Environment.NewLine, logger.GetLog());
            Assert.DoesNotContain($"// ### New Parameter {default(int)} ###" + Environment.NewLine, logger.GetLog());
        }

        public class ParamsTestProperty
        {
            [Params(1, 2)]
            public int ParamProperty { get; set; }

            private HashSet<int> collectedParams = new HashSet<int>();

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

        [Fact]
        public void ParamsDoesNotSupportPropertyWithoutPublicSetter()
        {
            // System.InvalidOperationException : Property "ParamProperty" must be public and writable if it has the [Params(..)] attribute applied to it
            Assert.Throws<InvalidOperationException>(() => CanExecute<ParamsTestPrivatePropertyError>());
        }

        public class ParamsTestPrivatePropertyError
        {
            [Params(1, 2)]
            public int ParamProperty { get; private set; }

            private HashSet<int> collectedParams = new HashSet<int>();

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

        [Fact]
        public void ParamsSupportPublicFields()
        {
            var logger = new OutputLogger(Output);
            var config = CreateSimpleConfig(logger);

            CanExecute<ParamsTestField>(config);

            foreach (var param in new[] { 1, 2 })
                Assert.Contains($"// ### New Parameter {param} ###" + Environment.NewLine, logger.GetLog());
            Assert.DoesNotContain($"// ### New Parameter 0 ###" + Environment.NewLine, logger.GetLog());
        }

        public class ParamsTestField
        {
            [Params(1, 2)]
            public int ParamField = 0;

            private HashSet<int> collectedParams = new HashSet<int>();

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

        [Fact]
        public void ParamsDoesNotSupportPrivateFields()
        {
            // System.InvalidOperationException : Field "ParamField" must be public if it has the [Params(..)] attribute applied to it
            Assert.Throws<InvalidOperationException>(() => CanExecute<ParamsTestPrivateFieldError>());
        }

        public class ParamsTestPrivateFieldError
        {
            [Params(1, 2)]
            private int ParamField = 0;

            private HashSet<int> collectedParams = new HashSet<int>();

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

        public enum NestedOne
        {
            SampleValue = 1234
        }

        [Fact]
        public void NestedEnumsAsParamsAreSupported() => CanExecute<NestedEnumsAsParams>();

        public class NestedEnumsAsParams
        {
            [Params(NestedOne.SampleValue)]
            public NestedOne Field;

            [Benchmark]
            public NestedOne Benchmark() => Field;
        }

        [Fact]
        public void CharactersAsParamsAreSupported() => CanExecute<CharactersAsParams>();

        public class CharactersAsParams
        {
            [Params('*')]
            public char Field;

            [Benchmark]
            public char Benchmark() => Field;
        }

        [Fact]
        public void NullableTypesAsParamsAreSupported() => CanExecute<NullableTypesAsParams>();

        public class NullableTypesAsParams
        {
            [Params(null)]
            public int? Field = 1;

            [Benchmark]
            public void Benchmark()
            {
                if(Field != null) { throw new Exception("Field should be initialized in ctor with 1 and then set to null by Engine"); }
            }
        }

        [Fact]
        public void InvalidFileNamesInParamsAreSupported() => CanExecute<InvalidFileNamesInParams>();

        public class InvalidFileNamesInParams
        {
            [Params("/\\@#$%")]
            public string Field;

            [Benchmark]
            public void Benchmark() => Console.WriteLine("// " + Field);
        }

        [Fact]
        public void ParamsMustBeEscapedProperly() => CanExecute<NeedEscaping>();

        public class NeedEscaping
        {
            private const string Json = "{ \"message\": \"Hello, World!\" }";

            [Params(Json)]
            public string Field;

            [Benchmark]
            [Arguments(Json)]
            public void Benchmark(string argument)
            {
                if (Field != Json || argument != Json)
                    throw new InvalidOperationException("Wrong character escaping!");
            }
        }

        [Fact]
        public void ArrayCanBeUsedAsParameter() => CanExecute<WithArray>();

        public class WithArray
        {
            [Params(new[] { 0, 1, 2 })]
            public int[] Array;

            [Benchmark]
            public void AcceptingArray()
            {
                if (Array.Length != 3)
                    throw new InvalidOperationException("Incorrect array length");

                for (int i = 0; i < 3; i++)
                    if(Array[i] != i)
                        throw new InvalidOperationException($"Incorrect array element at index {i}, was {Array[i]} instead of {i}");
            }
        }
    }
}