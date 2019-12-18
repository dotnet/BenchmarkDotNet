using System;
using System.Collections;
using System.Collections.Generic;
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
        private const string WindowsDiagnosticAssemblyFileName = "BenchmarkDotNet.Diagnostics.Windows.dll";
        private const string WindowsDiagnosticAssemblyName = "BenchmarkDotNet.Diagnostics.Windows";

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

            if (RuntimeInformation.IsWindows())
                return LoadCoreOnWindows();

            return LoadCore();
        }

        private static IDiagnoser[] LoadMono()
            => new IDiagnoser[]
            {
                // this method should return a IHardwareCountersDiagnoser when we implement Hardware Counters for Unix
                MemoryDiagnoser.Default,
                DisassemblyDiagnoser.Create(new DisassemblyDiagnoserConfig())
            };

        private static IDiagnoser[] LoadCore()
            => new IDiagnoser[]
            {
                MemoryDiagnoser.Default,
                EventPipeProfiler.Default,
            };

        private static IDiagnoser[] LoadCoreOnWindows()
        {
            List<IDiagnoser> result = new List<IDiagnoser>
            {
                MemoryDiagnoser.Default,
                EventPipeProfiler.Default,
                DisassemblyDiagnoser.Create(new DisassemblyDiagnoserConfig()),
            };

            // For .Net Core we can try to load `BenchmarkDotNet.Diagnostics.Windows` on Windows because it's using a .NET Standard compatible EventTrace lib now
            LoadWindowsDiagnosers(result);

            return result.ToArray();
        }

        private static IDiagnoser[] LoadClassic()
        {
            List<IDiagnoser> result = new List<IDiagnoser>
            {
                MemoryDiagnoser.Default, 
                DisassemblyDiagnoser.Create(new DisassemblyDiagnoserConfig())
            };

            LoadWindowsDiagnosers(result);
            
            return result.ToArray();
        }

        private static void LoadWindowsDiagnosers(List<IDiagnoser> result)
        {
            try
            {
                var benchmarkDotNetAssembly = typeof(DefaultConfig).GetTypeInfo().Assembly;

                var diagnosticsAssembly = Assembly.Load(new AssemblyName(WindowsDiagnosticAssemblyName));

                if (diagnosticsAssembly.GetName().Version != benchmarkDotNetAssembly.GetName().Version)
                {
                    string errorMsg =
                        $"Unable to load: {WindowsDiagnosticAssemblyFileName} version {diagnosticsAssembly.GetName().Version}" +
                        Environment.NewLine +
                        $"Does not match: {Path.GetFileName(benchmarkDotNetAssembly.Location)} version {benchmarkDotNetAssembly.GetName().Version}";
                    ConsoleLogger.Default.WriteLineError(errorMsg);
                }
                else
                {
                    result.Add(CreateDiagnoser(diagnosticsAssembly, "BenchmarkDotNet.Diagnostics.Windows.InliningDiagnoser"));
                    result.Add(CreateDiagnoser(diagnosticsAssembly, "BenchmarkDotNet.Diagnostics.Windows.PmcDiagnoser"));
                    result.Add(CreateDiagnoser(diagnosticsAssembly, "BenchmarkDotNet.Diagnostics.Windows.EtwProfiler"));
                    result.Add(CreateDiagnoser(diagnosticsAssembly, "BenchmarkDotNet.Diagnostics.Windows.ConcurrencyVisualizerProfiler"));
                    result.Add(CreateDiagnoser(diagnosticsAssembly, "BenchmarkDotNet.Diagnostics.Windows.NativeMemoryProfiler"));
                }
            }
            catch (Exception ex) // we're loading a plug-in, better to be safe rather than sorry
            {
                ConsoleLogger.Default.WriteLineError($"Error loading {WindowsDiagnosticAssemblyFileName}: {ex.GetType().Name} - {ex.Message}");
            }
        }

        private static IDiagnoser CreateDiagnoser(Assembly loadedAssembly, string typeName)
            => (IDiagnoser)Activator.CreateInstance(loadedAssembly.GetType(typeName));
    }
}