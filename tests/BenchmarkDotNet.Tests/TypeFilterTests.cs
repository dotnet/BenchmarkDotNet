using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.ConsoleArguments;
using BenchmarkDotNet.Extensions;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Tests;
using BenchmarkDotNet.Tests.Loggers;
using JetBrains.Annotations;
using TypeFilter = BenchmarkDotNet.Running.TypeFilter;

namespace BenchmarkDotNet.Tests
{
    public class TypeFilterTests
    {
        public ITestOutputHelper Output { get; }

        public TypeFilterTests(ITestOutputHelper output) => Output = output;

        [Fact]
        public void ReturnsNoBenchmarksForInvalidTypes()
        {
            var benchmarks = Filter([typeof(ClassC)], ["--filter", "*"]);

            Assert.Empty(benchmarks);
        }

        [Fact]
        public void CanFilterAllBenchmark()
        {
            var benchmarks = Filter([typeof(ClassA), typeof(ClassB)], ["--filter", "*"]);

            Assert.Equal(5, benchmarks.Count);
            Assert.Contains("ClassA.Method1", benchmarks);
            Assert.Contains("ClassA.Method2", benchmarks);
            Assert.Contains("ClassB.Method1", benchmarks);
            Assert.Contains("ClassB.Method2", benchmarks);
            Assert.Contains("ClassB.Method3", benchmarks);
        }

        [Fact]
        public void CanFilterAllBenchmarksDuringListAllBenchmarkCase()
        {
            var benchmarks = Filter([typeof(ClassA), typeof(ClassB)], ["--list", "flat"]);

            Assert.Equal(5, benchmarks.Count);
            Assert.Contains("ClassA.Method1", benchmarks);
            Assert.Contains("ClassA.Method2", benchmarks);
            Assert.Contains("ClassB.Method1", benchmarks);
            Assert.Contains("ClassB.Method2", benchmarks);
            Assert.Contains("ClassB.Method3", benchmarks);
        }

        [Fact]
        public void CanFilterBenchmarksDuringListAllBenchmarkCase()
        {
            var benchmarks = Filter([typeof(ClassA), typeof(ClassB)], ["--list", "flat", "--filter", "*ClassB*"]);

            Assert.Equal(3, benchmarks.Count);
            Assert.Contains("ClassB.Method1", benchmarks);
            Assert.Contains("ClassB.Method2", benchmarks);
            Assert.Contains("ClassB.Method3", benchmarks);
        }

        [Fact]
        public void CanSelectMethods()
        {
            var benchmarks = Filter([typeof(ClassA), typeof(ClassB)], ["--filter", "*Method2", "*Method3"]);

            Assert.Equal(3, benchmarks.Count);
            Assert.Contains("ClassA.Method2", benchmarks);
            Assert.Contains("ClassB.Method2", benchmarks);
            Assert.Contains("ClassB.Method3", benchmarks);
        }

        [Fact]
        public void CanSelectMethodsWithFullName()
        {
            var benchmarks = Filter(
                [typeof(ClassA), typeof(ClassB)],
                ["--filter", "BenchmarkDotNet.Tests.ClassA.Method2", "BenchmarkDotNet.Tests.ClassB.Method3"]);

            Assert.Equal(2, benchmarks.Count);
            Assert.Contains("ClassA.Method2", benchmarks);
            Assert.Contains("ClassB.Method3", benchmarks);
        }

        [Fact]
        public void CanSelectClassesUsingPatters()
        {
            var benchmarks = Filter(
                [typeof(ClassA), typeof(ClassB)],
                ["--filter", "*ClassC*", "*ClassA*"]);

            // ClassC not matched as it has NO methods with the [Benchmark] attribute
            Assert.Equal(2, benchmarks.Count);
            Assert.Contains("ClassA.Method1", benchmarks);
            Assert.Contains("ClassA.Method2", benchmarks);
        }

        [Fact]
        public void CanNotSelectClassesUsingTypeNames()
        {
            var benchmarks = Filter(
                [typeof(ClassA), typeof(ClassB)],
                ["--filter", "ClassC", "ClassA"]);

            Assert.Empty(benchmarks); // it's not supported anymore
        }

        [Fact]
        public void CanSelectClassesWithFullName()
        {
            var benchmarks = Filter(
                [typeof(ClassA), typeof(ClassB)],
                ["--filter", "BenchmarkDotNet.Tests.ClassC*", "BenchmarkDotNet.Tests.ClassA*"]);

            // ClassC not matched as it has NO methods with the [Benchmark] attribute
            Assert.Equal(2, benchmarks.Count);
            Assert.Contains("ClassA.Method1", benchmarks);
            Assert.Contains("ClassA.Method2", benchmarks);
        }

        [Fact]
        public void CanSelectClassesUsingPattern()
        {
            var benchmarks = Filter(
                [typeof(ClassA), typeof(ClassB)],
                ["--filter", "BenchmarkDotNet.Tests.Class*A*"]);

            // ClassC not matched as it has NO methods with the [Benchmark] attribute
            Assert.Equal(2, benchmarks.Count);
            Assert.Contains("ClassA.Method1", benchmarks);
            Assert.Contains("ClassA.Method2", benchmarks);
        }

        [Fact]
        public void CanSelectAttributes()
        {
            var benchmarks = Filter(
                [typeof(ClassA), typeof(ClassB), typeof(NOTTests.ClassD)],
                ["--attribute", "Run"]);

            Assert.Equal(3, benchmarks.Count);
            Assert.Contains("ClassA.Method1", benchmarks);
            Assert.Contains("ClassA.Method2", benchmarks);
            Assert.Contains("ClassD.Method1", benchmarks);
        }

        [Fact]
        public void CanSelectAttributesWithFullName()
        {
            var benchmarks = Filter(
                [typeof(ClassA), typeof(ClassB), typeof(NOTTests.ClassD)],
                ["--attribute", "DontRunAttribute"]);

            Assert.Equal(4, benchmarks.Count);
            Assert.Contains("ClassB.Method1", benchmarks);
            Assert.Contains("ClassB.Method2", benchmarks);
            Assert.Contains("ClassB.Method3", benchmarks);
            Assert.Contains("ClassD.Method2", benchmarks);
        }

        [Fact]
        public void CanSelectNamespaces()
        {
            var benchmarks = Filter(
                [typeof(ClassA), typeof(ClassB), typeof(NOTTests.ClassD)],
                ["--filter", "BenchmarkDotNet.Tests*"]);

            Assert.Equal(5, benchmarks.Count);
            Assert.Contains("ClassA.Method1", benchmarks);
            Assert.Contains("ClassA.Method2", benchmarks);
            Assert.Contains("ClassB.Method1", benchmarks);
            Assert.Contains("ClassB.Method2", benchmarks);
            Assert.Contains("ClassB.Method3", benchmarks);
        }

        [Fact]
        public void ClassAndMethodsCanCombined()
        {
            var benchmarks = Filter(
                [typeof(ClassA), typeof(ClassB), typeof(NOTTests.ClassD)],
                ["--filter", "*ClassA.Method2", "*ClassA.Method3"]);

            Assert.Single(benchmarks);
            Assert.Contains("ClassA.Method2", benchmarks);
        }

        [Fact]
        public void MethodCanBeFilteredByParameters()
        {
            var benchmarks = Filter(
                [typeof(ClassA), typeof(ClassB), typeof(ClassE), typeof(NOTTests.ClassD)],
                ["--filter", "BenchmarkDotNet.Tests.ClassE.Method1(value: 0)"]);

            Assert.Single(benchmarks);
            Assert.Contains("ClassE.Method1", benchmarks);
        }

        [Fact]
        public void GenericTypesCanBeFilteredByDisplayName()
        {
            var benchmarks = Filter(
                [typeof(SomeGeneric<>)],
                ["--filter", "*SomeGeneric<Int32>*"]);

            Assert.Single(benchmarks);
            Assert.Contains("SomeGeneric<Int32>.Create", benchmarks);
        }

        [Fact]
        public void ReportsATypeWhoseAttributesCannotBeRead()
        {
            // The [Config] of this type throws while reflection constructs it, so the type is dropped rather than
            // allowed to abort the whole run - but it has to be said out loud, because GenericBenchmarksValidator
            // never gets to report it when nothing of the assembly survives, and "No benchmarks were found" on its
            // own sends the user looking in the wrong place.
            var logger = new AccumulationLogger();

            var benchmarks = Filter([typeof(ClassWithUnreadableConfig), typeof(ClassA)], ["--filter", "*"], logger);

            Assert.Equal(2, benchmarks.Count);
            Assert.Contains("ClassA.Method1", benchmarks);
            Assert.DoesNotContain("ClassWithUnreadableConfig.Method1", benchmarks);
            Assert.Contains(nameof(ClassWithUnreadableConfig), logger.GetLog(), StringComparison.Ordinal);
        }

        [Fact]
        public void ReportsATypeWhoseAttributesCannotBeReadWhenItIsTheOnlyOne()
        {
            // With nothing else to run, the validator never gets a chance to say anything, so this is the one place
            // left to explain why nothing was found - and the message must not be that no [Benchmark] was found.
            var logger = new AccumulationLogger();

            var benchmarks = Filter([typeof(ClassWithUnreadableConfig)], ["--filter", "*"], logger);

            Assert.Empty(benchmarks);
            Assert.Contains(nameof(ClassWithUnreadableConfig), logger.GetLog(), StringComparison.Ordinal);
            Assert.DoesNotContain("No [Benchmark] attribute found", logger.GetLog(), StringComparison.Ordinal);
        }

#if NETCOREAPP
        [Fact]
        public void ReportsAnAssemblyWhoseOnlyBenchmarkTypeCannotBeRead()
        {
            // BenchmarkSwitcher.FromAssembly(assembly).Run(args) comes in through the assembly rather than through
            // types, where "does this assembly declare benchmarks" used to be answered from the readable types only:
            // an assembly whose only benchmark class is unreadable was then told it has no [Benchmark] at all, which
            // is both silent about the cause and wrong. Emitted rather than compiled, because every real assembly
            // of this repository declares readable benchmarks too.
            var assembly = EmitAssemblyWithAnUnreadableBenchmarkType();
            var logger = new AccumulationLogger();

            var (allTypesValid, runnable) = TypeFilter.GetTypesWithRunnableBenchmarks([], [assembly], logger);

            Assert.True(allTypesValid);
            Assert.Empty(runnable);
            Assert.Contains("Unreadable was ignored because its attributes could not be read", logger.GetLog(), StringComparison.Ordinal);
            Assert.DoesNotContain("No [Benchmark] attribute found", logger.GetLog(), StringComparison.Ordinal);
        }

        private static System.Reflection.Assembly EmitAssemblyWithAnUnreadableBenchmarkType()
        {
            var assembly = System.Reflection.Emit.AssemblyBuilder.DefineDynamicAssembly(
                new System.Reflection.AssemblyName("UnreadableBenchmarks"),
                System.Reflection.Emit.AssemblyBuilderAccess.Run);
            var type = assembly.DefineDynamicModule("UnreadableBenchmarks").DefineType(
                "Unreadable",
                System.Reflection.TypeAttributes.Public | System.Reflection.TypeAttributes.Class);

            // [Config(typeof(AbstractConfig))]: reflection constructs the attribute in order to hand it back, and
            // ConfigAttribute instantiates the config it is given, which an abstract one cannot be.
            type.SetCustomAttribute(new System.Reflection.Emit.CustomAttributeBuilder(
                typeof(ConfigAttribute).GetConstructor([typeof(Type)])!,
                [typeof(AbstractConfig)]));

            // [Benchmark] public void Method1() { }
            var method = type.DefineMethod("Method1", System.Reflection.MethodAttributes.Public, typeof(void), Type.EmptyTypes);
            method.SetCustomAttribute(new System.Reflection.Emit.CustomAttributeBuilder(
                typeof(BenchmarkAttribute).GetConstructor([typeof(int), typeof(string)])!,
                [0, ""]));
            method.GetILGenerator().Emit(System.Reflection.Emit.OpCodes.Ret);

            type.CreateType();

            return assembly;
        }
#endif

        private HashSet<string> Filter(Type[] types, string[] args, ILogger? logger = null)
        {
            var nonNullLogger = logger ?? new OutputLogger(Output);

            var config = ConfigParser.Parse(args, nonNullLogger);

            var runnableTypes = TypeFilter.GetTypesWithRunnableBenchmarks(types, [], nonNullLogger);

            return new HashSet<string>(TypeFilter.Filter(config.config!, runnableTypes.runnable)
                .SelectMany(runInfo => runInfo.BenchmarksCases)
                .Select(benchmark => $"{benchmark.Descriptor.Type.GetDisplayName()}.{benchmark.Descriptor.WorkloadMethod.Name}"));
        }
    }
}

namespace BenchmarkDotNet.Tests
{
    public class RunAttribute : Attribute { }

    public class DontRunAttribute : Attribute { }

    [Run]
    public class ClassA
    {
        [Benchmark]
        public void Method1() { }

        [Benchmark]
        public void Method2() { }
    }

    [DontRun]
    public class ClassB
    {
        [Benchmark]
        public void Method1() { }

        [Benchmark]
        public void Method2() { }

        [Benchmark]
        public void Method3() { }
    }

    [Config(typeof(AbstractConfig))]
    public class ClassWithUnreadableConfig
    {
        [Benchmark]
        public void Method1() { }
    }

    // ConfigAttribute instantiates the type it is given, and an abstract one cannot be instantiated.
    public abstract class AbstractConfig : ManualConfig
    {
    }

    public class ClassC
    {
        // None of these methods are actually Benchmarks!!
        [UsedImplicitly]
        public void Method1() { }

        [UsedImplicitly]
        public void Method2() { }

        [UsedImplicitly]
        public void Method3() { }
    }

    [GenericTypeArguments(typeof(int))]
    [GenericTypeArguments(typeof(string))]
    public class SomeGeneric<T>
    {
        [Benchmark]
        public T Create() => Activator.CreateInstance<T>();
    }

    [Run]
    public class ClassE
    {
        public static IEnumerable<object> Values =>
        [
            uint.MinValue,
            (uint)12345, // same value used by other tests to compare the perf
            uint.MaxValue,
        ];

        [Benchmark]
        [ArgumentsSource(nameof(Values))]
        public string Method1(uint value) => value.ToString();
    }
}

namespace BenchmarkDotNet.NOTTests
{
    public class ClassD
    {
        [Run]
        [Benchmark]
        public void Method1() { }

        [DontRun]
        [Benchmark]
        public void Method2() { }
    }
}