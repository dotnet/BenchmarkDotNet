using System;
using System.IO;
using System.Linq;
using System.Reflection;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Loggers;

namespace BenchmarkDotNet.Full
{
    public class ClassicDiagnosersLoader : IDiagnosersLoader
    {
        const string DiagnosticAssemblyFileName = "BenchmarkDotNet.Diagnostics.Windows.dll";
        const string DiagnosticAssemblyName = "BenchmarkDotNet.Diagnostics.Windows";

        private static readonly Lazy<IDiagnoser[]> diagnosers = new Lazy<IDiagnoser[]>(Load);

        public IDiagnoser[] LoadDiagnosers() => diagnosers.Value;

        private static IDiagnoser[] Load()
        {
            try
            {
                var benchmarkDotNetCoreAssembly = typeof(DefaultConfig).GetTypeInfo().Assembly;

                var diagnosticsAssembly = LoadDiagnosticsAssembly(benchmarkDotNetCoreAssembly);

                if (diagnosticsAssembly.GetName().Version != benchmarkDotNetCoreAssembly.GetName().Version)
                {
                    var errorMsg =
                        $"Unable to load: {DiagnosticAssemblyFileName} version {diagnosticsAssembly.GetName().Version}" +
                        Environment.NewLine +
                        $"Does not match: {System.IO.Path.GetFileName(benchmarkDotNetCoreAssembly.Location)} version {benchmarkDotNetCoreAssembly.GetName().Version}";
                    ConsoleLogger.Default.WriteLineError(errorMsg);
                }
                else
                {
                    return new[]
                    {
                        MemoryDiagnoser.Default,
                        CreateDiagnoser(diagnosticsAssembly, "BenchmarkDotNet.Diagnostics.Windows.InliningDiagnoser"),
                        CreateDiagnoser(diagnosticsAssembly, "BenchmarkDotNet.Diagnostics.Windows.PmcDiagnoser"),
                    };
                }
            }
            catch (Exception ex) // we're loading a plug-in, better to be safe rather than sorry
            {
                ConsoleLogger.Default.WriteLineError($"Error loading {DiagnosticAssemblyFileName}: {ex.GetType().Name} - {ex.Message}");
            }

            return new IDiagnoser[]
            {
                MemoryDiagnoser.Default
            };
        }

        private static Assembly LoadDiagnosticsAssembly(Assembly benchmarkDotNetCoreAssembly)
        {
            // it not enough to just install NuGet to be "referenced", the project has to consume the dll for real to be on the referenced assembly list
            var referencedAssemblyName = Assembly.GetEntryAssembly().GetReferencedAssemblies().SingleOrDefault(name => name.Name == DiagnosticAssemblyName);
            if (referencedAssemblyName != default(AssemblyName))
                return Assembly.Load(referencedAssemblyName);

            // we use the location of BenchmarkDotNet.Runtime.dll, because it should be in the same folder
            var diagnosticAssemblyBinPath = Path.Combine(new FileInfo(benchmarkDotNetCoreAssembly.Location).DirectoryName, DiagnosticAssemblyFileName);
            if (File.Exists(diagnosticAssemblyBinPath))
                return Assembly.LoadFile(diagnosticAssemblyBinPath);

            // Assembly.LoadFrom(fileName) searches in current directory, not bin, but it's our last chance
            return Assembly.LoadFrom(DiagnosticAssemblyFileName);
        }

        private static IDiagnoser CreateDiagnoser(Assembly loadedAssembly, string typeName)
            => (IDiagnoser)Activator.CreateInstance(loadedAssembly.GetType(typeName));
    }
}