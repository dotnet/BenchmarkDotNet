using System;
using System.IO;
using System.Linq;
using System.Reflection;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Toolchains;
using BenchmarkDotNet.Toolchains.InProcess;

namespace BenchmarkDotNet.Portability
{
    internal static class ServicesProvider
    {
        private static readonly Lazy<Services> current = new Lazy<Services>(Load);

        internal static RuntimeInformation RuntimeInformation => current.Value.RuntimeInformation;

        internal static IDiagnosersLoader DiagnosersLoader => current.Value.DiagnosersLoader;

        internal static Func<ILogger, IDisposable> AssemblyResolverFactory => current.Value.AssemblyResolverFactory;

        internal static IResourcesService ResourcesService => current.Value.ResourcesService;

        internal static Func<TimeSpan, BenchmarkActionCodegen, bool, IExecutor> InProcessExecutorFactory => current.Value.InProcessExecutorFactory;

        internal static IDotNetStandardWorkarounds DotNetStandardWorkarounds => current.Value.DotNetStandardWorkarounds;

        internal static IBenchmarkConverter BenchmarkCoverter => current.Value.BenchmarkConverter;

        private static Services Load()
        {
            var typeInfo = typeof(ServicesProvider).GetTypeInfo();

            var location = typeInfo.Assembly.ReadProperty<Assembly, string>("Location");

            var directoryPath = Path.GetDirectoryName(location);

            var runtimeSpecificDll = new DirectoryInfo(directoryPath).EnumerateFileSystemInfos("BenchmarkDotNet.Runtime*.dll").OrderByDescending(file => file.Name.Length).First();

            string assemblyQualifiedName = typeInfo.AssemblyQualifiedName
                                                   .Replace("BenchmarkDotNet.Runtime", Path.GetFileNameWithoutExtension(runtimeSpecificDll.Name))
                                                   .Replace("BenchmarkDotNet.Portability.ServicesProvider", "BenchmarkDotNet.ServicesProviderContract");

            var runtimeSpecificImplementation = Type.GetType(assemblyQualifiedName);

            var settingsField = runtimeSpecificImplementation.GetRuntimeFields().Single(field => field.IsStatic && field.Name == "Settings");

            return (Services)settingsField.GetValue(null);
        }

        private static TProperty ReadProperty<TType, TProperty>(this TType instance, string propertyName)
        {
            var property = typeof(TType).GetTypeInfo().GetDeclaredProperty(propertyName);

            return (TProperty)property.GetMethod.Invoke(instance, null);
        }
    }

    internal class Services
    {
        internal Services(RuntimeInformation runtimeInformation, IDiagnosersLoader diagnosersLoader, IResourcesService resourcesService, Func<ILogger, IDisposable> assemblyResolverFactory, Func<TimeSpan, BenchmarkActionCodegen, bool, IExecutor> inProcessExecutorFactory, IDotNetStandardWorkarounds dotNetStandardWorkarounds, IBenchmarkConverter benchmarkConverter)
        {
            RuntimeInformation = runtimeInformation;
            DiagnosersLoader = diagnosersLoader;
            ResourcesService = resourcesService;
            AssemblyResolverFactory = assemblyResolverFactory;
            InProcessExecutorFactory = inProcessExecutorFactory;
            DotNetStandardWorkarounds = dotNetStandardWorkarounds;
            BenchmarkConverter = benchmarkConverter;
        }

        internal RuntimeInformation RuntimeInformation { get; }

        internal IDiagnosersLoader DiagnosersLoader { get; }

        internal IResourcesService ResourcesService { get; }

        internal Func<ILogger, IDisposable> AssemblyResolverFactory { get; }

        internal Func<TimeSpan, BenchmarkActionCodegen, bool, IExecutor> InProcessExecutorFactory { get; }

        internal IDotNetStandardWorkarounds DotNetStandardWorkarounds { get; }

        internal IBenchmarkConverter BenchmarkConverter { get; }
    }
}