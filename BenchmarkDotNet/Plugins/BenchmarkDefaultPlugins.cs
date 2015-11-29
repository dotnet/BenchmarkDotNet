using System;
using System.IO;
using System.Reflection;
using BenchmarkDotNet.Plugins.Diagnosers;
using BenchmarkDotNet.Plugins.Exporters;
using BenchmarkDotNet.Plugins.Loggers;

namespace BenchmarkDotNet.Plugins
{
    public static class BenchmarkDefaultPlugins
    {
        public static IBenchmarkLogger[] Loggers = { BenchmarkConsoleLogger.Default };
        public static IBenchmarkExporter[] Exporters = { BenchmarkCsvExporter.Default, BenchmarkMarkdownExporter.Default };
        public static IBenchmarkDiagnoser[] Diagnosers = LoadDiagnoser();

        private static IBenchmarkDiagnoser[] LoadDiagnoser()
        {
            var diagnosticAssembly = "BenchmarkDotNet.Diagnostics.dll";
            try
            {
                var loadedAssembly = Assembly.LoadFrom(diagnosticAssembly);
                var thisAssembly = Assembly.GetAssembly(typeof(BenchmarkPluginBuilder));
                if (loadedAssembly.GetName().Version != thisAssembly.GetName().Version)
                {
                    var errorMsg =
                        $"Unable to load: {diagnosticAssembly} version {loadedAssembly.GetName().Version}" +
                        Environment.NewLine +
                        $"Does not match: {Path.GetFileName(thisAssembly.Location)} version {thisAssembly.GetName().Version}";
                    BenchmarkConsoleLogger.Default.WriteLineError(errorMsg);
                }
                else
                {
                    var runtimeDiagnoserType = loadedAssembly.GetType("BenchmarkDotNet.Diagnostics.BenchmarkRuntimeDiagnoser");
                    var runtimeDiagnoser = (IBenchmarkDiagnoser)Activator.CreateInstance(runtimeDiagnoserType);
                    var sourceDiagnoserType = loadedAssembly.GetType("BenchmarkDotNet.Diagnostics.BenchmarkSourceDiagnoser");
                    var sourceDiagnoser = (IBenchmarkDiagnoser)Activator.CreateInstance(sourceDiagnoserType);
                    return new[] { runtimeDiagnoser, sourceDiagnoser };
                }
            }
            catch (Exception ex) // we're loading a plug-in, better to be safe rather than sorry
            {
                BenchmarkConsoleLogger.Default.WriteLineError("Error loading {0}: {1}", diagnosticAssembly, ex.Message);
            }
            return new IBenchmarkDiagnoser[0];
        }
    }
}