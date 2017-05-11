using System;
using BenchmarkDotNet.Diagnosers;

namespace BenchmarkDotNet
{
    public class DiagnosersLoader : IDiagnosersLoader
    {
        public IDiagnoser[] LoadDiagnosers() => Array.Empty<IDiagnoser>();
    }
}