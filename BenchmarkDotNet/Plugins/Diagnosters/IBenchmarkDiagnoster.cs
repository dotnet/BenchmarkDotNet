namespace BenchmarkDotNet.Plugins.Diagnosters
{
    /// <summary>
    /// This is the interface that we expect the BenchmarkDotNet.Diagnostics "plugin" to implement
    /// </summary>
    public interface IBenchmarkDiagnoster
    {
        void PrintCodeForMethod(bool printAssembly, bool printIL, bool printDiagnostics);
    }
}
