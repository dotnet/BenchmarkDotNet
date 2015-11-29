namespace BenchmarkDotNet.Plugins.Diagnosers
{
    public class BenchmarkCompositeDiagnoser : IBenchmarkDiagnoser
    {
        private readonly IBenchmarkDiagnoser[] diagnosers;

        public BenchmarkCompositeDiagnoser(params IBenchmarkDiagnoser[] diagnosers)
        {
            this.diagnosers = diagnosers;
        }

        public void PrintCodeForMethod(bool printAssembly, bool printIL, bool printDiagnostics)
        {
            foreach (var diagnoster in diagnosers)
                diagnoster.PrintCodeForMethod(printAssembly, printIL, printDiagnostics);
        }
    }
}