using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Portability;

namespace BenchmarkDotNet.Diagnosers
{
    internal static class DiagnosersLoader
    {
        private const string DiagnosticAssemblyFileName = "BenchmarkDotNet.Diagnostics.Windows.dll";
        private const string DiagnosticAssemblyName = "BenchmarkDotNet.Diagnostics.Windows";

        // Make the Diagnosers lazy-loaded, so they are only instantiated if needed
        private static readonly Lazy<IDiagnoser[]> LazyLoadedDiagnosers 
            = new Lazy<IDiagnoser[]>(LoadDiagnosers, LazyThreadSafetyMode.ExecutionAndPublication);

        internal static IDiagnoser GetImplementation<TDiagnoser>() where TDiagnoser : IDiagnoser
            => LazyLoadedDiagnosers.Value
                    .FirstOrDefault(diagnoser => diagnoser is TDiagnoser) // few diagnosers can implement same interface, order matters
                ?? GetUnresolvedDiagnoser<TDiagnoser>();
        
        internal static IDiagnoser GetImplementation<TDiagnoser>(Predicate<TDiagnoser> filter) where TDiagnoser : IDiagnoser
            => LazyLoadedDiagnosers.Value
                    .FirstOrDefault(diagnoser => diagnoser is TDiagnoser typed && filter(typed)) // few diagnosers can implement same interface, order matters
               ?? GetUnresolvedDiagnoser<TDiagnoser>();

        private static IDiagnoser GetUnresolvedDiagnoser<TDiagnoser>() => new UnresolvedDiagnoser(typeof(TDiagnoser));

        private static IDiagnoser[] LoadDiagnosers()
        {
            if (RuntimeInformation.IsMono)
                return LoadMono();
            if (RuntimeInformation.IsFullFramework)
                return LoadClassic();

            // we can try to load `BenchmarkDotNet.Diagnostics.Windows` on Windows because it's using a .NET Standard compatible EventTrace lib now
            if (RuntimeInformation.IsWindows())
                return LoadClassic();

            return LoadCore();
        }

        private static IDiagnoser[] LoadCore() => new IDiagnoser[] { MemoryDiagnoser.Default };

        private static IDiagnoser[] LoadMono() 
            => new IDiagnoser[]
            {
                // this method should return a IHardwareCountersDiagnoser when we implement Hardware Counters for Unix
                MemoryDiagnoser.Default,
                DisassemblyDiagnoser.Create(new DisassemblyDiagnoserConfig()) 
            }; 

        private static IDiagnoser[] LoadClassic()
        {
            try
            {
                var benchmarkDotNetAssembly = typeof(DefaultConfig).GetTypeInfo().Assembly;

                var diagnosticsAssembly = Assembly.Load(new AssemblyName(DiagnosticAssemblyName));

                if (diagnosticsAssembly.GetName().Version != benchmarkDotNetAssembly.GetName().Version)
                {
                    string errorMsg =
                        $"Unable to load: {DiagnosticAssemblyFileName} version {diagnosticsAssembly.GetName().Version}" +
                        Environment.NewLine +
                        $"Does not match: {Path.GetFileName(benchmarkDotNetAssembly.Location)} version {benchmarkDotNetAssembly.GetName().Version}";
                    ConsoleLogger.Default.WriteLineError(errorMsg);
                }
                else
                {
                    return new[]
                    {
                        MemoryDiagnoser.Default,
                        DisassemblyDiagnoser.Create(new DisassemblyDiagnoserConfig()),
                        CreateDiagnoser(diagnosticsAssembly, "BenchmarkDotNet.Diagnostics.Windows.InliningDiagnoser"),
                        CreateDiagnoser(diagnosticsAssembly, "BenchmarkDotNet.Diagnostics.Windows.PmcDiagnoser"),
                        CreateDiagnoser(diagnosticsAssembly, "BenchmarkDotNet.Diagnostics.Windows.EtwProfiler"),
                    };
                }
            }
            catch (Exception ex) // we're loading a plug-in, better to be safe rather than sorry
            {
                ConsoleLogger.Default.WriteLineError($"Error loading {DiagnosticAssemblyFileName}: {ex.GetType().Name} - {ex.Message}");
            }

            return new IDiagnoser[]
            {
                MemoryDiagnoser.Default,
                DisassemblyDiagnoser.Create(new DisassemblyDiagnoserConfig())
            };
        }

        private static IDiagnoser CreateDiagnoser(Assembly loadedAssembly, string typeName)
            => (IDiagnoser)Activator.CreateInstance(loadedAssembly.GetType(typeName));
    }
}