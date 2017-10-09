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
        const string DiagnosticAssemblyFileName = "BenchmarkDotNet.Diagnostics.Windows.dll";
        const string DiagnosticAssemblyName = "BenchmarkDotNet.Diagnostics.Windows";

        // Make the Diagnosers lazy-loaded, so they are only instantiated if needed
        internal static readonly Lazy<IDiagnoser[]> LazyLoadedDiagnosers 
            = new Lazy<IDiagnoser[]>(LoadDiagnosers, LazyThreadSafetyMode.ExecutionAndPublication);

        internal static IDiagnoser GetImplementation<TDiagnoser>() where TDiagnoser : IDiagnoser
            => LazyLoadedDiagnosers.Value
                .SingleOrDefault(diagnoser => diagnoser is TDiagnoser)
                ?? GetUnresolvedDiagnoser<TDiagnoser>();

        internal static IDiagnoser GetImplementation<TDiagnoser, TConfig>(TConfig config) where TDiagnoser : IConfigurableDiagnoser<TConfig>
            => LazyLoadedDiagnosers.Value
                .OfType<TDiagnoser>().SingleOrDefault()?.Configure(config)
                ?? GetUnresolvedDiagnoser<TDiagnoser>();

        private static IDiagnoser GetUnresolvedDiagnoser<TDiagnoser>() => new UnresolvedDiagnoser(typeof(TDiagnoser));

        private static IDiagnoser[] LoadDiagnosers()
        {
#if CLASSIC
            return RuntimeInformation.IsMono() ? LoadMono() : LoadClassic();
#else
            return LoadCore();
#endif
        }

        private static IDiagnoser[] LoadCore() => new IDiagnoser[] { MemoryDiagnoser.Default };

        private static IDiagnoser[] LoadMono() 
            => new IDiagnoser[]
            {
                // this method should return a IHardwareCountersDiagnoser when we implement Hardware Counters for Unix
                MemoryDiagnoser.Default,
                DisassemblyDiagnoser.Create(new DisassemblyDiagnoserConfig()), 
            }; 

#if CLASSIC
        private static IDiagnoser[] LoadClassic()
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
                        DisassemblyDiagnoser.Create(new DisassemblyDiagnoserConfig()),
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
                MemoryDiagnoser.Default,
                DisassemblyDiagnoser.Create(new DisassemblyDiagnoserConfig())
            };
        }

        private static Assembly LoadDiagnosticsAssembly(Assembly benchmarkDotNetCoreAssembly)
        {
            // it not enough to just install NuGet to be "referenced", the project has to consume the dll for real to be on the referenced assembly list
            var referencedAssemblyName = Assembly.GetEntryAssembly()?.GetReferencedAssemblies().SingleOrDefault(name => name.Name == DiagnosticAssemblyName);
            if (referencedAssemblyName != default(AssemblyName))
                return Assembly.Load(referencedAssemblyName);

            // we use the location of BenchmarkDotNet.Core.dll, because it should be in the same folder
            var diagnosticAssemblyBinPath = Path.Combine(new FileInfo(benchmarkDotNetCoreAssembly.Location).DirectoryName, DiagnosticAssemblyFileName);
            if (File.Exists(diagnosticAssemblyBinPath))
                return Assembly.LoadFile(diagnosticAssemblyBinPath);

            // Assembly.LoadFrom(fileName) searches in current directory, not bin, but it's our last chance
            return Assembly.LoadFrom(DiagnosticAssemblyFileName);
        }

        private static IDiagnoser CreateDiagnoser(Assembly loadedAssembly, string typeName)
            => (IDiagnoser)Activator.CreateInstance(loadedAssembly.GetType(typeName));
#endif
    }
}