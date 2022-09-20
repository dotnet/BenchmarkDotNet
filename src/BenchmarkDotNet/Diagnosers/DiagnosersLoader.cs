using System;
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
            = new Lazy<IDiagnoser[]>(() => LoadDiagnosers().ToArray(), LazyThreadSafetyMode.ExecutionAndPublication);

        internal static IDiagnoser GetImplementation<TDiagnoser>() where TDiagnoser : IDiagnoser
            => LazyLoadedDiagnosers.Value
                    .FirstOrDefault(diagnoser => diagnoser is TDiagnoser) // few diagnosers can implement same interface, order matters
                ?? GetUnresolvedDiagnoser<TDiagnoser>();

        internal static IDiagnoser GetImplementation<TDiagnoser>(Predicate<TDiagnoser> filter) where TDiagnoser : IDiagnoser
            => LazyLoadedDiagnosers.Value
                    .FirstOrDefault(diagnoser => diagnoser is TDiagnoser typed && filter(typed)) // few diagnosers can implement same interface, order matters
               ?? GetUnresolvedDiagnoser<TDiagnoser>();

        private static IDiagnoser GetUnresolvedDiagnoser<TDiagnoser>() => new UnresolvedDiagnoser(typeof(TDiagnoser));

        private static IEnumerable<IDiagnoser> LoadDiagnosers()
        {
            yield return MemoryDiagnoser.Default;
            yield return new DisassemblyDiagnoser(new DisassemblyDiagnoserConfig());

            if (RuntimeInformation.IsNetCore)
            {
                yield return EventPipeProfiler.Default;

                if (RuntimeInformation.IsLinux())
                    yield return PerfCollectProfiler.Default;
            }

            if (!RuntimeInformation.IsWindows())
                yield break;

            foreach (var windowsDiagnoser in LoadWindowsDiagnosers())
                yield return windowsDiagnoser;
        }

        private static IDiagnoser[] LoadWindowsDiagnosers()
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

                    return Array.Empty<IDiagnoser>();
                }

                return new[]
                {
                    CreateDiagnoser(diagnosticsAssembly, "BenchmarkDotNet.Diagnostics.Windows.InliningDiagnoser"),
                    CreateDiagnoser(diagnosticsAssembly, "BenchmarkDotNet.Diagnostics.Windows.EtwProfiler"),
                    CreateDiagnoser(diagnosticsAssembly, "BenchmarkDotNet.Diagnostics.Windows.ConcurrencyVisualizerProfiler"),
                    CreateDiagnoser(diagnosticsAssembly, "BenchmarkDotNet.Diagnostics.Windows.NativeMemoryProfiler")
                };
            }
            catch (Exception ex) // we're loading a plug-in, better to be safe rather than sorry
            {
                ConsoleLogger.Default.WriteLineError($"Error loading {WindowsDiagnosticAssemblyFileName}: {ex.GetType().Name} - {ex.Message}");

                return Array.Empty<IDiagnoser>();
            }
        }

        private static IDiagnoser CreateDiagnoser(Assembly loadedAssembly, string typeName)
            => (IDiagnoser)Activator.CreateInstance(loadedAssembly.GetType(typeName));
    }
}