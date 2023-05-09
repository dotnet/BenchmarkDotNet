using System;
using BenchmarkDotNet.Attributes;
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
            var summary = CanExecute<ParamsTestProperty>();
            var standardOutput = GetCombinedStandardOutput(summary);

            foreach (var param in new[] { 1, 2 })
                Assert.Contains($"// ### New Parameter {param} ###", standardOutput);

            Assert.DoesNotContain($"// ### New Parameter {default(int)} ###", standardOutput);
        }

        public class ParamsTestProperty
        {
            [Params(1, 2)]
            public int ParamProperty { get; set; }

            [Benchmark]
            public void Benchmark() => Console.WriteLine($"// ### New Parameter {ParamProperty} ###");
        }

        [Fact]
        public void ParamsSupportPublicFields()
        {
            var summary = CanExecute<ParamsTestField>();
            var standardOutput = GetCombinedStandardOutput(summary);

            foreach (var param in new[] { 1, 2 })
                Assert.Contains($"// ### New Parameter {param} ###", standardOutput);

            Assert.DoesNotContain($"// ### New Parameter 0 ###", standardOutput);
        }

        public class ParamsTestField
        {
            [Params(1, 2)]
            public int ParamField = 0;

            [Benchmark]
            public void Benchmark() => Console.WriteLine($"// ### New Parameter {ParamField} ###");
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
                if (Field != null) { throw new Exception("Field should be initialized in ctor with 1 and then set to null by Engine"); }
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
        public void SpecialCharactersInStringAreSupported() => CanExecute<CompileSpecialCharactersInString>();

        public class CompileSpecialCharactersInString
        {
            [Params("\0")] public string Null;
            [Params("\t")] public string Tab;
            [Params("\n")] public string NewLine;
            [Params("\\")] public string Slash;
            [Params("\"")] public string Quote;
            [Params("\u0061")] public string Unicode;
            [Params("{")] public string Bracket;

            [Params("\n \0 \n")] public string Combo;

            [Params("C:\\file1.txt")] public string Path1;
            [Params(@"C:\file2.txt")] public string Path2;

            [Benchmark]
            public void Benchmark()
            {
                var isPassedAsSingleCharacter =
                    Null.Length == 1 &&
                    Tab.Length == 1 &&
                    NewLine.Length == 1 &&
                    Slash.Length == 1 &&
                    Quote.Length == 1 &&
                    Unicode.Length == 1 &&
                    Bracket.Length == 1;

                if (!isPassedAsSingleCharacter)
                    throw new InvalidOperationException("Some Param has an invalid escaped string");
            }
        }

        [Fact]
        public void SpecialCharactersInCharAreSupported() => CanExecute<CompileSpecialCharactersInChar>();

        public class CompileSpecialCharactersInChar
        {
            [Params('\0')] public char Null;
            [Params('\t')] public char Tab;
            [Params('\n')] public char NewLine;
            [Params('\\')] public char Slash;
            [Params('\"')] public char Quote;
            [Params('\u0061')] public char Unicode;

            [Benchmark]
            public void Benchmark() { }
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
                    if (Array[i] != i)
                        throw new InvalidOperationException($"Incorrect array element at index {i}, was {Array[i]} instead of {i}");
            }
        }

        [Fact]
        public void StaticFieldsAndPropertiesCanBeParams() => CanExecute<WithStaticParams>();

        public class WithStaticParams
        {
            [Params(1)]
            public static int StaticParamField = 0;

            [Params(2)]
            public static int StaticParamProperty { get; set; } = 0;

            [Benchmark]
            public void Test()
            {
                if (StaticParamField != 1)
                    throw new ArgumentException($"{nameof(StaticParamField)} has wrong value: {StaticParamField}!");
                if (StaticParamProperty != 2)
                    throw new ArgumentException($"{nameof(StaticParamProperty)} has wrong value: {StaticParamProperty}!");
            }
        }
    }
}