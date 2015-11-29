using BenchmarkDotNet.Plugins.Diagnosers;
using BenchmarkDotNet.Plugins.Exporters;
using BenchmarkDotNet.Plugins.Loggers;
using BenchmarkDotNet.Plugins.Toolchains;

namespace BenchmarkDotNet.Plugins
{
    public static class BenchmarkPluginsExtensions
    {
        public static IBenchmarkPluginBuilder AddLoggers(this IBenchmarkPluginBuilder builder, params IBenchmarkLogger[] loggers)
        {
            foreach (var logger in loggers)
                builder.AddLogger(logger);
            return builder;
        }

        public static IBenchmarkPluginBuilder AddExporters(this IBenchmarkPluginBuilder builder, params IBenchmarkExporter[] exporters)
        {
            foreach (var exporter in exporters)
                builder.AddExporter(exporter);
            return builder;
        }

        public static IBenchmarkPluginBuilder AddDiagnosers(this IBenchmarkPluginBuilder builder, params IBenchmarkDiagnoser[] diagnosers)
        {
            foreach (var diagnoser in diagnosers)
                builder.AddDiagnoser(diagnoser);
            return builder;
        }

        public static IBenchmarkPluginBuilder AddToolchains(this IBenchmarkPluginBuilder builder, params IBenchmarkToolchainBuilder[] toolchains)
        {
            foreach (var toolchain in toolchains)
                builder.AddToolchain(toolchain);
            return builder;
        }
    }
}