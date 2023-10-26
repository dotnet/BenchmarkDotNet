using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Tests.XUnit;
using BenchmarkDotNet.Toolchains;
using BenchmarkDotNet.Toolchains.InProcess.Emit;
using Xunit;
using Xunit.Abstractions;

namespace BenchmarkDotNet.IntegrationTests
{
    public class ArgumentsTests : BenchmarkTestExecutor
    {
        public static IEnumerable<object[]> GetToolchains()
            => new[]
                {
                    new object[] { Job.Default.GetToolchain() },
                    new object[] { InProcessEmitToolchain.Instance },
                };

        public ArgumentsTests(ITestOutputHelper output) : base(output) { }

        [Theory, MemberData(nameof(GetToolchains))]
        public void ArgumentsArePassedToBenchmarks(IToolchain toolchain) => CanExecute<WithArguments>(toolchain);

        public class WithArguments
        {
            [Benchmark]
            [Arguments(true, 1)]
            [Arguments(false, 2)]
            public void Simple(bool boolean, int number)
            {
                if (boolean && number != 1 || !boolean && number != 2)
                    throw new InvalidOperationException("Incorrect values were passed");
            }

            [Benchmark]
            [Arguments(true, 1)]
            [Arguments(false, 2)]
            public Task SimpleAsync(bool boolean, int number)
            {
                if (boolean && number != 1 || !boolean && number != 2)
                    throw new InvalidOperationException("Incorrect values were passed");

                return Task.CompletedTask;
            }

            [Benchmark]
            [Arguments(true, 1)]
            [Arguments(false, 2)]
            public ValueTask<int> SimpleValueTaskAsync(bool boolean, int number)
            {
                if (boolean && number != 1 || !boolean && number != 2)
                    throw new InvalidOperationException("Incorrect values were passed");

                return new ValueTask<int>(0);
            }
        }

        [Theory, MemberData(nameof(GetToolchains))]
        public void ArgumentsFromSourceArePassedToBenchmarks(IToolchain toolchain) => CanExecute<WithArgumentsSource>(toolchain);

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
                yield return new object[] { true, 1 };
                yield return new object[] { false, 2 };
            }
        }

        [Theory, MemberData(nameof(GetToolchains))]
        public void ArgumentsCanBePassedByReferenceToBenchmark(IToolchain toolchain) => CanExecute<WithRefArguments>(toolchain);

        public class WithRefArguments
        {
            [Benchmark]
            [Arguments(true, 1)]
            [Arguments(false, 2)]
            public void Simple(ref bool boolean, ref int number)
            {
                if (boolean && number != 1 || !boolean && number != 2)
                    throw new InvalidOperationException("Incorrect values were passed");
            }
        }

        [Theory, MemberData(nameof(GetToolchains))]
        public void ArgumentsCanBePassedByReadonlyReferenceToBenchmark(IToolchain toolchain) => CanExecute<WithInArguments>(toolchain);

        public class WithInArguments
        {
            [Benchmark]
            [Arguments(true, 1)]
            [Arguments(false, 2)]
            public void Simple(in bool boolean, in int number)
            {
                if (boolean && number != 1 || !boolean && number != 2)
                    throw new InvalidOperationException("Incorrect values were passed");
            }
        }

        [Theory, MemberData(nameof(GetToolchains))]
        public void NonCompileTimeConstantsCanBeReturnedFromSource(IToolchain toolchain) => CanExecute<WithComplexTypesReturnedFromSources>(toolchain);

        public class WithComplexTypesReturnedFromSources
        {
            [ParamsSource(nameof(DictionaryAsParam))]
            public Dictionary<int, string> DictionaryParamInstance;

            [ParamsSource(nameof(SameButStatic))]
            public Dictionary<int, string> DictionaryParamStatic;

            [Benchmark]
            [ArgumentsSource(nameof(NonPrimitive))]
            public void Simple(SomeClass someClass, SomeStruct someStruct)
            {
                if (DictionaryParamInstance[1234] != "it's an instance getter")
                    throw new InvalidOperationException("Incorrect dictionary (instance");

                if (DictionaryParamStatic[1234] != "it's a static getter")
                    throw new InvalidOperationException("Incorrect dictionary (static)");

                if (!(someStruct.RangeEnd == 100 || someStruct.RangeEnd == 1000))
                    throw new InvalidOperationException("Incorrect struct values were passed");

                if (someStruct.RangeEnd != someClass.Values.Length)
                    throw new InvalidOperationException("Incorrect length");

                for (int i = 0; i < someStruct.RangeEnd; i++)
                    if (someClass.Values[i] != i * 2)
                        throw new InvalidOperationException("Incorrect array values were passed");
            }

            public IEnumerable<object[]> NonPrimitive()
            {
                yield return new object[] { new SomeClass(Enumerable.Range(0, 100).ToArray()), new SomeStruct(100) };
                yield return new object[] { new SomeClass(Enumerable.Range(0, 1000).ToArray()), new SomeStruct(1000) };
            }

            public IEnumerable<object> DictionaryAsParam => new object[] { new Dictionary<int, string>() { { 1234, "it's an instance getter" } } };

            public static IEnumerable<object> SameButStatic => new object[] { new Dictionary<int, string>() { { 1234, "it's a static getter" } } };

            public class SomeClass
            {
                public SomeClass(int[] initialValues) => Values = initialValues.Select(val => val * 2).ToArray();

                public int[] Values { get; }
            }

            public struct SomeStruct
            {
                public SomeStruct(int rangeEnd) => RangeEnd = rangeEnd;

                public int RangeEnd { get; }
            }
        }

        [Theory, MemberData(nameof(GetToolchains))]
        public void ArrayCanBeUsedAsArgument(IToolchain toolchain) => CanExecute<WithArray>(toolchain);

        public class WithArray
        {
            [Benchmark]
            [Arguments(new[] { 0, 1, 2 })]
            public void AcceptingArray(int[] array)
            {
                if (array.Length != 3)
                    throw new InvalidOperationException("Incorrect array length");

                for (int i = 0; i < 3; i++)
                    if (array[i] != i)
                        throw new InvalidOperationException($"Incorrect array element at index {i}, was {array[i]} instead of {i}");
            }
        }

        [Theory, MemberData(nameof(GetToolchains))]
        public void IEnumerableCanBeUsedAsArgument(IToolchain toolchain) => CanExecute<WithIEnumerable>(toolchain);

        public class WithIEnumerable
        {
            private static IEnumerable<int> Iterator() { yield return 1; }

            public IEnumerable<object[]> Sources()
            {
                yield return new object[] { "Empty", Enumerable.Empty<int>() };
                yield return new object[] { "Range", Enumerable.Range(0, 10) };
                yield return new object[] { "List", new List<int>() { 1, 2, 3 } };
                yield return new object[] { "int[]", new int[] { 1, 2, 3 } };
                yield return new object[] { "int[].Select", new int[] { 1, 2, 3 }.Select(i => i) };
                yield return new object[] { "int[].Select.Where", new int[] { 1, 2, 3 }.Select(i => i).Where(i => i % 2 == 0) };
                yield return new object[] { "Iterator", Iterator() };
                yield return new object[] { "Iterator.Select", Iterator().Select(i => i) };
                yield return new object[] { "Iterator.Select.Where", Iterator().Select(i => i).Where(i => i % 2 == 0) };
            }

            [Benchmark]
            [ArgumentsSource(nameof(Sources))]
            public void Any(string name, IEnumerable<int> source) => source.Any();
        }

        [Theory, MemberData(nameof(GetToolchains))]
        public void JaggedArrayCanBeUsedAsArgument(IToolchain toolchain) => CanExecute<WithJaggedArray>(toolchain);

        public class WithJaggedArray
        {
            [Benchmark]
            [ArgumentsSource(nameof(CreateMatrix))]
            public void Test(int[][] array)
            {
                if (array == null)
                    throw new ArgumentNullException(nameof(array));

                for (int i = 0; i < 10; i++)
                for (int j = 0; j < i; j++)
                    if (array[i][j] != i)
                        throw new ArgumentException("Invalid value");
            }

            public IEnumerable<object> CreateMatrix()
            {
                int[][] jagged = new int[10][];

                for (int i = 0; i < jagged.Length; i++)
                {
                    int[] row = new int[i];

                    for (int j = 0; j < i; j++)
                        row[j] = i;

                    jagged[i] = row;
                }

                yield return jagged;
            }
        }

        [Theory, MemberData(nameof(GetToolchains))]
        public void GenericTypeCanBePassedByRefAsArgument(IToolchain toolchain) => CanExecute<WithGenericByRef>(toolchain);

        public class WithGenericByRef
        {
            public class Generic<T1, T2>
            {
                public T1 Item1;
                public T2 Item2;

                public Generic(T1 item1, T2 item2)
                {
                    Item1 = item1;
                    Item2 = item2;
                }
            }

            [Benchmark]
            [ArgumentsSource(nameof(GetInputData))]
            public bool ValueTupleCompareNoOpt(ref Generic<int, string> byRef)
            {
                if (byRef == null)
                    throw new ArgumentNullException(nameof(byRef));

                if (byRef.Item1 != 3 || byRef.Item2 != "red")
                    throw new ArgumentException("Wrong values");

                return true;
            }

            public IEnumerable<object> GetInputData()
            {
                yield return new Generic<int, string>(3, "red");
            }
        }

        [Theory, MemberData(nameof(GetToolchains))]
        public void AnArrayOfTypeWithNoParameterlessCtorCanBePassedAsArgument(IToolchain toolchain) => CanExecute<WithArrayOfStringAsArgument>(toolchain);

        public class WithArrayOfStringAsArgument
        {
            [Benchmark]
            [Arguments(new object[1] { new string[0] })]
            // arguments accept "params object[]", when we pass just a string[] it's recognized as an array of params
            public void TypeReflectionArrayGetType(object anArray)
            {
                string[] strings = (string[])anArray;

                if (strings.Length != 0)
                    throw new ArgumentException("The array should be empty");
            }
        }

        [Theory, MemberData(nameof(GetToolchains))]
        public void AnArrayCanBePassedToBenchmarkAsSpan(IToolchain toolchain) => CanExecute<WithArrayToSpan>(toolchain);

        public class WithArrayToSpan
        {
            [Benchmark]
            [Arguments(new[] { 0, 1, 2 })]
            public void AcceptsSpan(Span<int> span)
            {
                if (span.Length != 3)
                    throw new ArgumentException("Invalid length");

                for (int i = 0; i < 3; i++)
                    if (span[i] != i)
                        throw new ArgumentException("Invalid value");
            }
        }

        [TheoryEnvSpecific("The implicit cast operator is available only in .NET Core 2.1+ (See https://github.com/dotnet/corefx/issues/30121 for more)",
            EnvRequirement.DotNetCoreOnly)]
        [MemberData(nameof(GetToolchains))]
        public void StringCanBePassedToBenchmarkAsReadOnlySpan(IToolchain toolchain) => CanExecute<WithStringToReadOnlySpan>(toolchain);

        public class WithStringToReadOnlySpan
        {
            private const string expectedString = "very nice string";

            [Benchmark]
            [Arguments(expectedString)]
            public void AcceptsReadOnlySpan(ReadOnlySpan<char> notString)
            {
                string aString = notString.ToString();

                if (aString != expectedString)
                    throw new ArgumentException("Invalid value");
            }
        }

        [Theory, MemberData(nameof(GetToolchains))]
        public void AnArrayOfStringsCanBeUsedAsArgument(IToolchain toolchain) =>
            CanExecute<WithArrayOfStringFromArgumentSource>(toolchain);

        public class WithArrayOfStringFromArgumentSource
        {
            public IEnumerable<object> GetArrayOfString()
            {
                yield return new string[123];
            }

            [Benchmark]
            [ArgumentsSource(nameof(GetArrayOfString))]
            public void TypeReflectionArrayGetType(string[] array)
            {
                if (array.Length != 123)
                    throw new ArgumentException("The array was empty");
            }
        }

        [Theory, MemberData(nameof(GetToolchains))] // make sure BDN mimics xunit's MemberData behaviour
        public void AnIEnumerableOfArrayOfObjectsCanBeUsedAsArgumentForBenchmarkAcceptingSingleArgument(IToolchain toolchain)
            => CanExecute<WithIEnumerableOfArrayOfObjectsFromArgumentSource>(toolchain);

        public class WithIEnumerableOfArrayOfObjectsFromArgumentSource
        {
            public IEnumerable<object[]> GetArguments()
            {
                yield return new object[] { true };
            }

            [Benchmark]
            [ArgumentsSource(nameof(GetArguments))]
            public void SingleArgument(bool boolean)
            {
                if (boolean != true)
                    throw new ArgumentException("The value of boolean was incorrect");
            }
        }

        [Theory, MemberData(nameof(GetToolchains))]
        public void BenchmarkCanAcceptFewArrays(IToolchain toolchain) => CanExecute<FewArrays>(toolchain);

        public class FewArrays
        {
            public IEnumerable<object[]> GetArrays()
            {
                yield return new object[2]
                {
                    new int[] { 0, 2, 4 },
                    new int[] { 1, 3, 5 },
                };
            }

            [Benchmark]
            [ArgumentsSource(nameof(GetArrays))]
            public void AcceptsArrays(int[] even, int[] notEven)
            {
                if (even.Length != 3 || notEven.Length != 3)
                    throw new ArgumentException("Incorrect length");

                if (!even.All(n => n % 2 == 0))
                    throw new ArgumentException("Not even");

                if (!notEven.All(n => n % 2 != 0))
                    throw new ArgumentException("Even");
            }
        }

        [Theory, MemberData(nameof(GetToolchains))]
        public void VeryBigIntegersAreSupported(IToolchain toolchain) => CanExecute<WithVeryBigInteger>(toolchain);

        public class WithVeryBigInteger
        {
            public IEnumerable<object> GetVeryBigInteger()
            {
                yield return BigInteger.Parse(new string(Enumerable.Repeat('1', 1000).ToArray()));
            }

            [Benchmark]
            [ArgumentsSource(nameof(GetVeryBigInteger))]
            public void Method(BigInteger passed)
            {
                BigInteger expected = GetVeryBigInteger().OfType<BigInteger>().Single();

                if (expected != passed)
                    throw new ArgumentException("The BigInteger has wrong value!");
            }
        }

        [Theory, MemberData(nameof(GetToolchains))]
        public void SpecialDoubleValuesAreSupported(IToolchain toolchain) => CanExecute<WithSpecialDoubleValues>(toolchain);

        public class WithSpecialDoubleValues
        {
            public IEnumerable<object[]> GetSpecialDoubleValues()
            {
                yield return new object[] { double.Epsilon, nameof(double.Epsilon) };
                yield return new object[] { double.MinValue, nameof(double.MinValue) };
                yield return new object[] { double.MaxValue, nameof(double.MaxValue) };
                yield return new object[] { double.NaN, nameof(double.NaN) };
                yield return new object[] { double.NegativeInfinity, nameof(double.NegativeInfinity) };
                yield return new object[] { double.PositiveInfinity, nameof(double.PositiveInfinity) };
            }

            [Benchmark]
            [ArgumentsSource(nameof(GetSpecialDoubleValues))]
            public void Method(double passed, string name)
            {
                switch (name)
                {
                    case nameof(double.Epsilon):
                        if (passed != double.Epsilon) throw new InvalidOperationException($"Unable to pass {nameof(double.Epsilon)}");
                        break;
                    case nameof(double.MaxValue):
                        if (passed != double.MaxValue) throw new InvalidOperationException($"Unable to pass {nameof(double.MaxValue)}");
                        break;
                    case nameof(double.MinValue):
                        if (passed != double.MinValue) throw new InvalidOperationException($"Unable to pass {nameof(double.MinValue)}");
                        break;
                    case nameof(double.NaN):
                        if (!double.IsNaN(passed)) throw new InvalidOperationException($"Unable to pass {nameof(double.NaN)}");
                        break;
                    case nameof(double.PositiveInfinity):
                        if (!double.IsPositiveInfinity(passed)) throw new InvalidOperationException($"Unable to pass {nameof(double.PositiveInfinity)}");
                        break;
                    case nameof(double.NegativeInfinity):
                        if (!double.IsNegativeInfinity(passed)) throw new InvalidOperationException($"Unable to pass {nameof(double.NegativeInfinity)}");
                        break;
                    default:
                        throw new InvalidOperationException($"Unknown case! {name}");
                }
            }
        }

        [Theory, MemberData(nameof(GetToolchains))]
        public void SpecialFloatValuesAreSupported(IToolchain toolchain) => CanExecute<WithSpecialFloatValues>(toolchain);

        public class WithSpecialFloatValues
        {
            public IEnumerable<object[]> GetSpecialFloatValues()
            {
                yield return new object[] { float.Epsilon, nameof(float.Epsilon) };
                yield return new object[] { float.MinValue, nameof(float.MinValue) };
                yield return new object[] { float.MaxValue, nameof(float.MaxValue) };
                yield return new object[] { float.NaN, nameof(float.NaN) };
                yield return new object[] { float.NegativeInfinity, nameof(float.NegativeInfinity) };
                yield return new object[] { float.PositiveInfinity, nameof(float.PositiveInfinity) };
            }

            [Benchmark]
            [ArgumentsSource(nameof(GetSpecialFloatValues))]
            public void Method(float passed, string name)
            {
                switch (name)
                {
                    case nameof(float.Epsilon):
                        if (passed != float.Epsilon) throw new InvalidOperationException($"Unable to pass {nameof(float.Epsilon)}");
                        break;
                    case nameof(float.MaxValue):
                        if (passed != float.MaxValue) throw new InvalidOperationException($"Unable to pass {nameof(float.MaxValue)}");
                        break;
                    case nameof(float.MinValue):
                        if (passed != float.MinValue) throw new InvalidOperationException($"Unable to pass {nameof(float.MinValue)}");
                        break;
                    case nameof(float.NaN):
                        if (!float.IsNaN(passed)) throw new InvalidOperationException($"Unable to pass {nameof(float.NaN)}");
                        break;
                    case nameof(float.PositiveInfinity):
                        if (!float.IsPositiveInfinity(passed)) throw new InvalidOperationException($"Unable to pass {nameof(float.PositiveInfinity)}");
                        break;
                    case nameof(float.NegativeInfinity):
                        if (!float.IsNegativeInfinity(passed)) throw new InvalidOperationException($"Unable to pass {nameof(float.NegativeInfinity)}");
                        break;
                    default:
                        throw new InvalidOperationException($"Unknown case! {name}");
                }
            }
        }

        [Theory, MemberData(nameof(GetToolchains))]
        public void SpecialDecimalValuesAreSupported(IToolchain toolchain) => CanExecute<WithSpecialDecimalValues>(toolchain);

        public class WithSpecialDecimalValues
        {
            public IEnumerable<object[]> GetSpecialDecimalValues()
            {
                yield return new object[] { decimal.MaxValue, nameof(decimal.MaxValue) };
                yield return new object[] { decimal.MinValue, nameof(decimal.MinValue) };
            }

            [Benchmark]
            [ArgumentsSource(nameof(GetSpecialDecimalValues))]
            public void Method(decimal passed, string name)
            {
                switch (name)
                {
                    case nameof(decimal.MaxValue):
                        if (passed != decimal.MaxValue) throw new InvalidOperationException($"Unable to pass {nameof(decimal.MaxValue)}");
                        break;
                    case nameof(decimal.MinValue):
                        if (passed != decimal.MinValue) throw new InvalidOperationException($"Unable to pass {nameof(decimal.MinValue)}");
                        break;
                    default:
                        throw new InvalidOperationException($"Unknown case! {name}");
                }
            }
        }

        [Theory, MemberData(nameof(GetToolchains))]
        public void DateTimeCanBeUsedAsArgument(IToolchain toolchain) => CanExecute<WithDateTime>(toolchain);

        public class WithDateTime
        {
            public IEnumerable<object> DateTimeValues()
            {
                yield return new DateTime(2018, 8, 15);
            }

            [Benchmark]
            [ArgumentsSource(nameof(DateTimeValues))]
            public void Test(DateTime passed)
            {
                DateTime expected = DateTimeValues().OfType<DateTime>().Single();

                if (expected != passed)
                    throw new ArgumentException("The DateTime has wrong value!");
            }
        }

        [Theory, MemberData(nameof(GetToolchains))]
        public void CustomTypeThatAlsoExistsInTheSystemNamespaceAsArgument(IToolchain toolchain) => CanExecute<CustomTypeThatAlsoExistsInTheSystemNamespace>(toolchain);

        public class CustomTypeThatAlsoExistsInTheSystemNamespace
        {
            public enum Action
            {
                It, Is, A, Duplicate, Of, System, Dot, Action
            }

            [Benchmark]
            [Arguments(Action.System)]
            public void Test(Action passed)
            {
                Action expected = Action.System;

                if (expected != passed)
                    throw new ArgumentException("The passed enum has wrong value!");
            }
        }

        [Theory, MemberData(nameof(GetToolchains))]
        public void EnumFlagsAreSupported(IToolchain toolchain) => CanExecute<WithEnumFlags>(toolchain);

        public class WithEnumFlags
        {
            [Flags]
            public enum LongFlagEnum : long
            {
                None = 0,
                First = 1 << 0,
                Second = 1 << 1,
                Third = 1 << 2,
                Fourth = 1 << 3
            }

            [Flags]
            public enum ByteFlagEnum : byte
            {
                None = 0,
                First = 1 << 0,
                Second = 1 << 1,
                Third = 1 << 2,
                Fourth = 1 << 3
            }

            [Benchmark]
            [Arguments(LongFlagEnum.First | LongFlagEnum.Second, ByteFlagEnum.Third | ByteFlagEnum.Fourth)]
            public void Test(LongFlagEnum passedLongFlagEnum, ByteFlagEnum passedByteFlagEnum)
            {
                if ((LongFlagEnum.First | LongFlagEnum.Second) != passedLongFlagEnum)
                    throw new ArgumentException("The passed long flag enum has wrong value!");

                if ((ByteFlagEnum.Third | ByteFlagEnum.Fourth) != passedByteFlagEnum)
                    throw new ArgumentException("The passed byte flag enum has wrong value!");
            }
        }

        [Theory, MemberData(nameof(GetToolchains))]
        public void UndefinedEnumValuesAreSupported(IToolchain toolchain) => CanExecute<WithUndefinedEnumValue>(toolchain);

        public class WithUndefinedEnumValue
        {
            [Flags]
            public enum SomeEnum : long
            {
                First = 0, Last = 1
            }

            [Benchmark]
            [Arguments(SomeEnum.First, (SomeEnum)100, (SomeEnum)(-100))]
            public void Test(SomeEnum defined, SomeEnum undefined, SomeEnum undefinedNegative)
            {
                if (SomeEnum.First != defined)
                    throw new ArgumentException("The passed defined enum has wrong value!");

                if ((SomeEnum)100 != undefined)
                    throw new ArgumentException("The passed undefined enum has wrong value!");

                if ((SomeEnum)(-100) != undefinedNegative)
                    throw new ArgumentException("The passed undefined negative enum has wrong value!");
            }
        }

        [Theory, MemberData(nameof(GetToolchains))]
        public void StaticMethodsAndPropertiesCanBeUsedAsSources_EnumerableOfObjects(IToolchain toolchain)
            => CanExecute<WithStaticSources_EnumerableOfObjects>(toolchain);

        public class WithStaticSources_EnumerableOfObjects
        {
            public static IEnumerable<object> StaticMethod() { yield return 1; }

            public static IEnumerable<object> StaticProperty
            {
                get
                {
                    yield return 2;
                    yield return 3;
                }
            }

            [ParamsSource(nameof(StaticMethod))]
            public int ParamOne { get; set; }

            [ParamsSource(nameof(StaticProperty))]
            public int ParamTwo { get; set; }

            [Benchmark]
            [ArgumentsSource(nameof(StaticMethod))]
            public void TestMethod(int argument)
            {
                if (argument != 1)
                    throw new ArgumentException("The argument value is incorrect!");
                if (ParamOne != 1)
                    throw new ArgumentException("The ParamOne value is incorrect!");
                if (ParamTwo != 2 && ParamTwo != 3)
                    throw new ArgumentException("The ParamTwo value is incorrect!");
            }

            [Benchmark]
            [ArgumentsSource(nameof(StaticProperty))]
            public void TestProperty(int argument)
            {
                if (argument != 2 && argument != 3)
                    throw new ArgumentException("The argument value is incorrect!");
                if (ParamOne != 1)
                    throw new ArgumentException("The ParamOne value is incorrect!");
                if (ParamTwo != 2 && ParamTwo != 3)
                    throw new ArgumentException("The ParamTwo value is incorrect!");
            }
        }

        [Theory, MemberData(nameof(GetToolchains))]
        public void StaticMethodsAndPropertiesCanBeUsedAsSources_EnumerableOfArrayOfObjects(IToolchain toolchain)
            => CanExecute<WithStaticSources_EnumerableOfArrayOfObjects>(toolchain);

        public class WithStaticSources_EnumerableOfArrayOfObjects
        {
            public static IEnumerable<object[]> StaticMethod() { yield return new object[] { 1 }; }
            public static IEnumerable<object[]> StaticProperty
            {
                get
                {
                    yield return new object[] { 2 };
                    yield return new object[] { 3 };
                }
            }

            [ParamsSource(nameof(StaticMethod))]
            public int ParamOne { get; set; }

            [ParamsSource(nameof(StaticProperty))]
            public int ParamTwo { get; set; }

            [Benchmark]
            [ArgumentsSource(nameof(StaticMethod))]
            public void TestMethod(int argument)
            {
                if (argument != 1)
                    throw new ArgumentException("The argument value is incorrect!");
                if (ParamOne != 1)
                    throw new ArgumentException("The ParamOne value is incorrect!");
                if (ParamTwo != 2 && ParamTwo != 3)
                    throw new ArgumentException("The ParamTwo value is incorrect!");
            }

            [Benchmark]
            [ArgumentsSource(nameof(StaticProperty))]
            public void TestProperty(int argument)
            {
                if (argument != 2 && argument != 3)
                    throw new ArgumentException("The argument value is incorrect!");
                if (ParamOne != 1)
                    throw new ArgumentException("The ParamOne value is incorrect!");
                if (ParamTwo != 2 && ParamTwo != 3)
                    throw new ArgumentException("The ParamTwo value is incorrect!");
            }
        }

        [Theory, MemberData(nameof(GetToolchains))]
        public void VeryLongStringsAreSupported(IToolchain toolchain) => CanExecute<WithVeryLongString>(toolchain);

        public class WithVeryLongString
        {
            private readonly string LongString = new string('a', 200_000);
            private readonly string LongString2 = new string('a', 200_000 - 1) + "b";

            public IEnumerable<object[]> Arguments()
            {
                yield return new object[] { LongString, LongString2 };
            }

            [Benchmark]
            [ArgumentsSource(nameof(Arguments))]
            public void Test(string first, string second)
            {
                if (first != LongString)
                    throw new ArgumentException($"{nameof(first)} passed string has wrong value!");
                if (second != LongString2)
                    throw new ArgumentException($"{nameof(second)} passed string has wrong value!");
            }
        }

        [Theory, MemberData(nameof(GetToolchains))]
        public void ComplexStringPattersAreSupported(IToolchain toolchain) => CanExecute<Perf_Regex_Industry_RustLang_Sherlock>(toolchain);

        public class Perf_Regex_Industry_RustLang_Sherlock
        {
            [Params(@"[""'][^""']{0,30}[?!.][""']")]
            public string Pattern { get; set; }

            [Benchmark]
            public int Consume() => Pattern.Length;
        }

        [Fact]
        public void UnusedDisposableParamsAreDisposed() => CanExecute<WithDisposableArguments>(Job.Default.GetToolchain());

        public class WithDisposableArguments
        {
            public IEnumerable<Disposable> GetDisposables()
            {
                yield return new Disposable(0);
                yield return new Disposable(1);
            }

            [ParamsSource(nameof(GetDisposables))]
            public Disposable used;

            [Benchmark]
            public void CheckDisposed()
            {
                if (used.Id == 0)
                {
                    if (Disposable.Created != 1)
                        throw new ArgumentException("Only one instance should be created so far!");
                    if (Disposable.Disposed != 0)
                        throw new ArgumentException("None should be disposed as only one was created and is still in use");
                }
                if (used.Id == 1)
                {
                    if (Disposable.Created != 2)
                        throw new ArgumentException("Two instances should be created so far!");
                    if (Disposable.Disposed != 1)
                        throw new ArgumentException("The first one should be disposed as it's not used");
                }
            }

            public class Disposable : IDisposable
            {
                public static int Created = 0;
                public static int Disposed = 0;

                public int Id { get; private set; }

                public Disposable(int id)
                {
                    Id = id;
                    ++Created;
                }

                public void Dispose() => ++Disposed;
            }
        }

        private void CanExecute<T>(IToolchain toolchain) => CanExecute<T>(CreateSimpleConfig(job: Job.Dry.WithToolchain(toolchain)));
    }
}