using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Toolchains;

namespace BenchmarkDotNet.IntegrationTests
{
    public class ParamsTests : BenchmarkTestExecutor
    {
        public ParamsTests(ITestOutputHelper output) : base(output) { }

        private IConfig? GetConfig()
            => new SingleRunInProcessConfig(Output); // Use `null` when running test with out-of-process toolchain.

        private IReadOnlyList<string> Execute<T>()
        {
            var config = GetConfig();

            bool isInProcess = config == null
                ? false
                : config.GetJobs().Single().GetToolchain().IsInProcess;

            if (isInProcess)
            {
                // When using inprocess toolchain, Report don't contains StandardOutput.
                using var captureConsoleHelper = new CaptureConsoleHelper();
                CanExecute<T>(config);
                return captureConsoleHelper.CapturedLines; // CapturedLines contains benchmark execution logs also.
            }
            else
            {
                // Use following code when running test with out-of-process.
                var summary = CanExecute<T>();
                return GetCombinedStandardOutput(summary);
            }
        }

        [Fact]
        public void ParamsSupportPropertyWithPublicSetter()
        {
            var standardOutput = Execute<ParamsTestProperty>();

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
            var standardOutput = Execute<ParamsTestField>();

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
        public void NestedEnumsAsParamsAreSupported()
        {
            Execute<NestedEnumsAsParams>();
        }

        public class NestedEnumsAsParams
        {
            [Params(NestedOne.SampleValue)]
            public NestedOne Field;

            [Benchmark]
            public NestedOne Benchmark() => Field;
        }

        [Fact]
        public void CharactersAsParamsAreSupported()
            => Execute<CharactersAsParams>();

        public class CharactersAsParams
        {
            [Params('*')]
            public char Field;

            [Benchmark]
            public char Benchmark() => Field;
        }

        [Fact]
        public void NullableTypesAsParamsAreSupported()
            => Execute<NullableTypesAsParams>();

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
        public void InvalidFileNamesInParamsAreSupported()
            => Execute<InvalidFileNamesInParams>();

        public class InvalidFileNamesInParams
        {
            [Params("/\\@#$%")]
            public required string Field;

            [Benchmark]
            public void Benchmark() => Console.WriteLine("// " + Field);
        }

        [Fact]
        public void SpecialCharactersInStringAreSupported()
            => Execute<CompileSpecialCharactersInString>();

        public class CompileSpecialCharactersInString
        {
            [Params("\0")] public required string Null;
            [Params("\t")] public required string Tab;
            [Params("\n")] public required string NewLine;
            [Params("\\")] public required string Slash;
            [Params("\"")] public required string Quote;
            [Params("\u0061")] public required string Unicode;
            [Params("{")] public required string Bracket;

            [Params("\n \0 \n")] public required string Combo;

            [Params("C:\\file1.txt")] public required string Path1;
            [Params(@"C:\file2.txt")] public required string Path2;

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
        public void SpecialCharactersInCharAreSupported()
            => Execute<CompileSpecialCharactersInChar>();

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
        public void ParamsMustBeEscapedProperly()
            => Execute<NeedEscaping>();

        public class NeedEscaping
        {
            private const string Json = "{ \"message\": \"Hello, World!\" }";

            [Params(Json)]
            public required string Field;

            [Benchmark]
            [Arguments(Json)]
            public void Benchmark(string argument)
            {
                if (Field != Json || argument != Json)
                    throw new InvalidOperationException("Wrong character escaping!");
            }
        }

        [Fact]
        public void ArrayCanBeUsedAsParameter()
            => Execute<WithArray>();

        public class WithArray
        {
            [Params(new[] { 0, 1, 2 })]
            public required int[] Array;

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
        public void StaticFieldsAndPropertiesCanBeParams()
            => Execute<WithStaticParams>();

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

#if NET8_0_OR_GREATER
        [Fact]
        public void ParamsSupportRequiredProperty()
        {
            var standardOutput = Execute<ParamsTestRequiredProperty>();

            foreach (var param in new[] { "a", "b" })
            {
                Assert.Contains($"// ### New Parameter {param} ###", standardOutput);
            }
        }

        public class ParamsTestRequiredProperty
        {
            [Params("a", "b")]
            public required string ParamProperty { get; set; }

            [Benchmark]
            public void Benchmark() => Console.WriteLine($"// ### New Parameter {ParamProperty} ###");
        }
#endif
    }
}
