namespace BenchmarkDotNet.Plugins.Diagnosers
{
    /// <summary>
    /// This is the interface that we expect the BenchmarkDotNet.Diagnostics "plugin" to implement
    /// </summary>
    public interface IBenchmarkDiagnoser
    {
        void PrintCodeForMethod(bool printAssembly, bool printIL, bool printDiagnostics);
    }
}
