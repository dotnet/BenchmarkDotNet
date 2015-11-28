namespace BenchmarkDotNet.Plugins.Diagnosters
{
    public class BenchmarkCompositeDiagnoster : IBenchmarkDiagnoster
    {
        private readonly IBenchmarkDiagnoster[] diagnosters;

        public BenchmarkCompositeDiagnoster(params IBenchmarkDiagnoster[] diagnosters)
        {
            this.diagnosters = diagnosters;
        }

        public void PrintCodeForMethod(bool printAssembly, bool printIL, bool printDiagnostics)
        {
            foreach (var diagnoster in diagnosters)
                diagnoster.PrintCodeForMethod(printAssembly, printIL, printDiagnostics);
        }
    }
}