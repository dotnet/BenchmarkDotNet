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
        private static readonly Lazy<ServicesContainer> current = new Lazy<ServicesContainer>(Load);
        private static ServicesContainer configured;

        internal static RuntimeInformation RuntimeInformation => (configured ?? current.Value).Resolve<RuntimeInformation>();
        internal static IDiagnosersLoader DiagnosersLoader => (configured ?? current.Value).Resolve<IDiagnosersLoader>();
        internal static Func<ILogger, IDisposable> AssemblyResolverFactory => (configured ?? current.Value).Resolve<Func<ILogger, IDisposable>>();
        internal static IResourcesService ResourcesService => (configured ?? current.Value).Resolve<IResourcesService>();
        internal static Func<TimeSpan, BenchmarkActionCodegen, bool, IExecutor> InProcessExecutorFactory => (configured ?? current.Value).Resolve<Func<TimeSpan, BenchmarkActionCodegen, bool, IExecutor>>();
        internal static IDotNetStandardWorkarounds DotNetStandardWorkarounds => (configured ?? current.Value).Resolve<IDotNetStandardWorkarounds>();
        internal static IBenchmarkConverter BenchmarkCoverter => (configured ?? current.Value).Resolve<IBenchmarkConverter>();

        internal static void Configure(ServicesContainer instance) => configured = instance;

        internal static T Resolve<T>() => (configured ?? current.Value).Resolve<T>();

        private static ServicesContainer Load()
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

            return (ServicesContainer)settingsField.GetValue(null);
        }

        private static TProperty ReadProperty<TType, TProperty>(this TType instance, string propertyName)
        {
            var property = typeof(TType).GetTypeInfo().GetDeclaredProperty(propertyName);

            return (TProperty)property.GetMethod.Invoke(instance, null);
        }
    }

    internal class ServicesContainer
    {
        private readonly object[] services;

        internal ServicesContainer(params object[] services)
        {
            this.services = services;
        }

        internal T Resolve<T>()
        {
            var found = services.SingleOrDefault(service => service is T);

            return found == default(object) 
                ? default(T)
                : (T)found;
        } 
    }
}