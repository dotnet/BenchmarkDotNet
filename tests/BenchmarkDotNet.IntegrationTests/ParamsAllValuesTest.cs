using System;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Tests.Loggers;
using Xunit;
using Xunit.Abstractions;

namespace BenchmarkDotNet.IntegrationTests
{
    public class ParamsAllValuesTestBoolTest : BenchmarkTestExecutor
    {
        public ParamsAllValuesTestBoolTest(ITestOutputHelper output) : base(output) { }

        [Fact]
        public void Test()
        {
            var logger = new OutputLogger(Output);
            var config = CreateSimpleConfig(logger);

            CanExecute<ParamsAllValuesTestBool>(config);
            foreach (var param in new[] { false, true })
                Assert.Contains($"// ### Parameter {param} ###" + Environment.NewLine, logger.GetLog());
        }

        public class ParamsAllValuesTestBool
        {
            [ParamsAllValues]
            public bool ParamProperty { get; set; }

            [Benchmark]
            public void Benchmark() => Console.WriteLine($"// ### Parameter {ParamProperty} ###");
        }
    }

    public class ParamsAllValuesTestEnumTest : BenchmarkTestExecutor
    {
        public ParamsAllValuesTestEnumTest(ITestOutputHelper output) : base(output) { }

        [Fact]
        public void Test()
        {
            var logger = new OutputLogger(Output);
            var config = CreateSimpleConfig(logger);

            CanExecute<ParamsAllValuesTestEnum>(config);
            foreach (var param in new[] { TestEnum.A, TestEnum.B, TestEnum.C })
                Assert.Contains($"// ### Parameter {param} ###" + Environment.NewLine, logger.GetLog());
            Assert.DoesNotContain($"// ### Parameter {default(TestEnum)} ###" + Environment.NewLine, logger.GetLog());
        }

        public class ParamsAllValuesTestEnum
        {
            [ParamsAllValues]
            public TestEnum ParamProperty { get; set; }

            [Benchmark]
            public void Benchmark() => Console.WriteLine($"// ### Parameter {ParamProperty} ###");
        }

        public enum TestEnum
        {
            A = 1, B, C
        }
    }

    public class ParamsAllValuesTestNullableBoolTest : BenchmarkTestExecutor
    {
        public ParamsAllValuesTestNullableBoolTest(ITestOutputHelper output) : base(output) { }

        [Fact]
        public void Test()
        {
            var logger = new OutputLogger(Output);
            var config = CreateSimpleConfig(logger);

            CanExecute<ParamsAllValuesTestNullableBool>(config);
            foreach (var param in new bool?[] { null, false, true })
                Assert.Contains($"// ### Parameter {param} ###" + Environment.NewLine, logger.GetLog());
        }

        public class ParamsAllValuesTestNullableBool
        {
            [ParamsAllValues]
            public bool? ParamProperty { get; set; }

            [Benchmark]
            public void Benchmark() => Console.WriteLine($"// ### Parameter {ParamProperty} ###");
        }
    }

    public class ParamsAllValuesTestNullableEnumTest : BenchmarkTestExecutor
    {
        public ParamsAllValuesTestNullableEnumTest(ITestOutputHelper output) : base(output) { }

        [Fact]
        public void Test()
        {
            var logger = new OutputLogger(Output);
            var config = CreateSimpleConfig(logger);

            CanExecute<ParamsAllValuesTestNullableEnum>(config);
            foreach (var param in new TestEnum?[] { null, TestEnum.A, TestEnum.B, TestEnum.C })
                Assert.Contains($"// ### Parameter {param} ###" + Environment.NewLine, logger.GetLog());
            Assert.DoesNotContain($"// ### Parameter {default(TestEnum)} ###" + Environment.NewLine, logger.GetLog());
        }

        public class ParamsAllValuesTestNullableEnum
        {
            [ParamsAllValues]
            public TestEnum? ParamProperty { get; set; }

            [Benchmark]
            public void Benchmark() => Console.WriteLine($"// ### Parameter {ParamProperty} ###");
        }

        public enum TestEnum
        {
            A = 1, B, C
        }
    }

    public class ParamsAllValuesTestNotAllowedTypeErrorTest : BenchmarkTestExecutor
    {
        public ParamsAllValuesTestNotAllowedTypeErrorTest(ITestOutputHelper output) : base(output) { }

        [Fact]
        public void Test()
        {
            // System.InvalidOperationException : Type Int32 cannot be used with [ParamsAllValues], allowed types are: bool, enum types and nullable type for another allowed type.
            Assert.Throws<InvalidOperationException>(() => CanExecute<ParamsAllValuesTestNotAllowedTypeError>());
        }

        public class ParamsAllValuesTestNotAllowedTypeError
        {
            [ParamsAllValues]
            public int ParamProperty { get; set; }

            [Benchmark]
            public void Benchmark() { }
        }
    }

    public class ParamsAllValuesTestNotAllowedNullableTypeErrorTest : BenchmarkTestExecutor
    {
        public ParamsAllValuesTestNotAllowedNullableTypeErrorTest(ITestOutputHelper output) : base(output) { }

        [Fact]
        public void Test()
        {
            // System.InvalidOperationException : Type Int32 cannot be used with [ParamsAllValues], allowed types are: bool, enum types and nullable type for another allowed type.
            Assert.Throws<InvalidOperationException>(() => CanExecute<ParamsAllValuesTestNotAllowedTypeError>());
        }

        public class ParamsAllValuesTestNotAllowedTypeError
        {
            [ParamsAllValues]
            public int? ParamProperty { get; set; }

            [Benchmark]
            public void Benchmark() { }
        }
    }

    public class ParamsAllValuesTestFlagsEnumErrorTest : BenchmarkTestExecutor
    {
        public ParamsAllValuesTestFlagsEnumErrorTest(ITestOutputHelper output) : base(output) { }

        [Fact]
        public void Test()
        {
            // System.InvalidOperationException : Unable to use TestFlagsEnum with [ParamsAllValues], because it's flags enum.
            Assert.Throws<InvalidOperationException>(() => CanExecute<ParamsAllValuesTestFlagsEnumError>());
        }

        public class ParamsAllValuesTestFlagsEnumError
        {
            [ParamsAllValues]
            public TestFlagsEnum ParamProperty { get; set; }

            [Benchmark]
            public void Benchmark() { }
        }

        [Flags]
        public enum TestFlagsEnum
        {
            A = 0b001,
            B = 0b010,
            C = 0b100
        }
    }
}