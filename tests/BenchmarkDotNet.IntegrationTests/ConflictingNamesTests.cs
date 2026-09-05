using BenchmarkDotNet.Attributes;

namespace BenchmarkDotNet.IntegrationTests;

public class ConflictingNamesTests(ITestOutputHelper output) : BenchmarkTestExecutor(output)
{
    // The un-prefixed template names (OverheadActionUnroll, WorkloadActionUnroll, ...) are usable because generated
    // members are __-prefixed, and a benchmark method named like a generated member (e.g. __Overhead) coexists with it
    // via C# member hiding. Only a [Params*] member's name can collide (it's assigned by the runnable's object
    // initializer) - that case is guarded by the ParamsValidator / BDN1208 analyzer instead. #2821
    [Fact]
    public void BenchmarkMethodsCanUseTemplateNames() => CanExecute<WithNamesUsedByTemplate>();

    public class WithNamesUsedByTemplate
    {
        [Params(1)]
        public int OverheadActionUnroll { get; set; }

        // The runnable's entry point and arguments struct are __Run and __FieldsContainer, so these names are free.
        [Params(2)]
        public int Run { get; set; }

        [Params(3)]
        public int FieldsContainer { get; set; }

        [Benchmark]
        [Arguments(2)]
        public void System(int OverheadActionNoUnroll)
        {

        }

        [Benchmark]
        public void BenchmarkDotNet()
        {

        }

        // Not a compile-time constant, so the child process re-obtains it through an expression the renderer emits
        // - the only place the generated code names the BenchmarkDotNet namespace from inside this type.
        public static IEnumerable<object> NonConstantValues()
        {
            yield return new object();
        }

        [Benchmark]
        [ArgumentsSource(nameof(NonConstantValues))]
        public void NonConstantArgument(object argument)
        {

        }

        [Benchmark]
        public void __Overhead()
        {

        }

        [Benchmark]
        [Arguments(3)]
        public void WorkloadActionUnroll(int WorkloadActionNoUnroll)
        {

        }
    }
}
