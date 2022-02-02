using System;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Extensions;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Portability;
using BenchmarkDotNet.Reports;
using JetBrains.Annotations;

namespace BenchmarkDotNet.Running
{
    // VS generates bad assembly binding redirects for ValueTuple for Full .NET Framework
    // we need to keep the logic that uses it in a separate method and create DirtyAssemblyResolveHelper first
    // so it can ignore the version mismatch ;)
    public static class BenchmarkRunner
    {
        [PublicAPI]
        public static Summary Run<T>(IConfig config = null, string[] args = null)
        {
            using (DirtyAssemblyResolveHelper.Create())
                return RunWithExceptionHandling(() => RunWithDirtyAssemblyResolveHelper(typeof(T), config, args));
        }

        [PublicAPI]
        public static Summary Run(Type type, IConfig config = null, string[] args = null)
        {
            using (DirtyAssemblyResolveHelper.Create())
                return RunWithExceptionHandling(() => RunWithDirtyAssemblyResolveHelper(type, config, args));
        }

        [PublicAPI]
        public static Summary[] Run(Type[] types, IConfig config = null, string[] args = null)
        {
            using (DirtyAssemblyResolveHelper.Create())
                return RunWithExceptionHandling(() => RunWithDirtyAssemblyResolveHelper(types, config, args));
        }

        [PublicAPI]
        public static Summary Run(Type type, MethodInfo[] methods, IConfig config = null)
        {
            using (DirtyAssemblyResolveHelper.Create())
                return RunWithExceptionHandling(() => RunWithDirtyAssemblyResolveHelper(type, methods, config));
        }

        [PublicAPI]
        public static Summary[] Run(Assembly assembly, IConfig config = null, string[] args = null)
        {
            using (DirtyAssemblyResolveHelper.Create())
                return RunWithExceptionHandling(() => RunWithDirtyAssemblyResolveHelper(assembly, config, args));
        }

        [PublicAPI]
        public static Summary Run(BenchmarkRunInfo benchmarkRunInfo)
        {
            using (DirtyAssemblyResolveHelper.Create())
                return RunWithExceptionHandling(() => RunWithDirtyAssemblyResolveHelper(new[] { benchmarkRunInfo }).Single());
        }

        [PublicAPI]
        public static Summary[] Run(BenchmarkRunInfo[] benchmarkRunInfos)
        {
            using (DirtyAssemblyResolveHelper.Create())
                return RunWithExceptionHandling(() => RunWithDirtyAssemblyResolveHelper(benchmarkRunInfos));
        }

        /// <summary>
        /// Supported only on Full .NET Framework. Not recommended.
        /// </summary>
        [PublicAPI]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public static Summary RunUrl(string url, IConfig config = null)
        {
            using (DirtyAssemblyResolveHelper.Create())
                return RunWithExceptionHandling(() => RunUrlWithDirtyAssemblyResolveHelper(url, config));
        }

        /// <summary>
        /// Supported only on Full .NET Framework. Not recommended.
        /// </summary>
        [PublicAPI]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public static Summary RunSource(string source, IConfig config = null)
        {
            using (DirtyAssemblyResolveHelper.Create())
                return RunWithExceptionHandling(() => RunSourceWithDirtyAssemblyResolveHelper(source, config));
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static Summary RunWithDirtyAssemblyResolveHelper(Type type, IConfig config, string[] args)
            => (args == null
                ? BenchmarkRunnerClean.Run(TypeToBenchmarks(type, config))
                : new BenchmarkSwitcher(new[] { type }).RunWithDirtyAssemblyResolveHelper(args, config, false))
                .Single();

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static Summary RunWithDirtyAssemblyResolveHelper(Type type, MethodInfo[] methods, IConfig config = null)
            => BenchmarkRunnerClean.Run(MethodsToBenchmarks(type, methods, config)).Single();

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static Summary[] RunWithDirtyAssemblyResolveHelper(Assembly assembly, IConfig config, string[] args)
            => args == null
                ? BenchmarkRunnerClean.Run(AssemblyToBenchmarks(assembly, config))
                : new BenchmarkSwitcher(assembly).RunWithDirtyAssemblyResolveHelper(args, config, false).ToArray();

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static Summary[] RunWithDirtyAssemblyResolveHelper(Type[] types, IConfig config, string[] args)
            => args == null
                ? BenchmarkRunnerClean.Run(TypesToBenchmarks(types, config))
                : new BenchmarkSwitcher(types).RunWithDirtyAssemblyResolveHelper(args, config, false).ToArray();

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static Summary[] RunWithDirtyAssemblyResolveHelper(BenchmarkRunInfo[] benchmarkRunInfos)
            => BenchmarkRunnerClean.Run(benchmarkRunInfos);

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static Summary RunUrlWithDirtyAssemblyResolveHelper(string url, IConfig config = null)
            => RuntimeInformation.IsFullFramework
                ? BenchmarkRunnerClean.Run(BenchmarkConverter.UrlToBenchmarks(url, config)).Single()
                : throw new InvalidBenchmarkDeclarationException("Supported only on Full .NET Framework");

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static Summary RunSourceWithDirtyAssemblyResolveHelper(string source, IConfig config = null)
            => RuntimeInformation.IsFullFramework
                ? BenchmarkRunnerClean.Run(BenchmarkConverter.SourceToBenchmarks(source, config)).Single()
                : throw new InvalidBenchmarkDeclarationException("Supported only on Full .NET Framework");

        private static Summary RunWithExceptionHandling(Func<Summary> run)
        {
            try
            {
                return run();
            }
            catch (InvalidBenchmarkDeclarationException e)
            {
                ConsoleLogger.Default.WriteLineError(e.Message);
                return Summary.NothingToRun(e.Message, string.Empty, string.Empty);
            }
        }

        private static Summary[] RunWithExceptionHandling(Func<Summary[]> run)
        {
            try
            {
                return run();
            }
            catch (InvalidBenchmarkDeclarationException e)
            {
                ConsoleLogger.Default.WriteLineError(e.Message);
                return new[] { Summary.NothingToRun(e.Message, string.Empty, string.Empty) };
            }
        }

        private static BenchmarkRunInfo[] TypeToBenchmarks(Type type, IConfig config)
        {
            if (type is null)
                throw new InvalidBenchmarkDeclarationException("Type not provided.");

            if (!type.ContainsRunnableBenchmarks())
                throw new InvalidBenchmarkDeclarationException($"{type.Name} must be public non-sealed non-static type and contain public [Benchmark] methods.");

            return new[] { BenchmarkConverter.TypeToBenchmarks(type, config) };
        }
        
        private static BenchmarkRunInfo[] MethodsToBenchmarks(Type containingType, MethodInfo[] methods, IConfig config)
        {
            if (containingType is null)
                throw new InvalidBenchmarkDeclarationException("Type not provided.");

            if (!containingType.ContainsRunnableBenchmarks())
                throw new InvalidBenchmarkDeclarationException($"{containingType.Name} must be public non-sealed non-static type and contain public [Benchmark] methods.");

            if (methods.IsNullOrEmpty())
                throw new InvalidBenchmarkDeclarationException($"No methods provided for {containingType.Name} type.");

            if (methods.Any(m => m is null))
                throw new InvalidBenchmarkDeclarationException($"Null not allowed for benchmark methods.");

            var publicBenchmarkMethodsOfType = containingType.GetMethods().Where(m => m.HasAttribute<BenchmarkAttribute>()).ToArray();
            var wrongMethods = methods.Except(publicBenchmarkMethodsOfType).ToArray();
            if (!wrongMethods.IsEmpty())
                throw new InvalidBenchmarkDeclarationException(string.Join(", ", wrongMethods.Select(m => m.Name)) + 
                                                               $" are wrong. Methods should be with [Benchmark] attribute of {containingType} type.");

            return new[] { BenchmarkConverter.MethodsToBenchmarks(containingType, methods, config) };
        }


        private static BenchmarkRunInfo[] AssemblyToBenchmarks(Assembly assembly, IConfig config)
        {
            if (assembly is null)
                throw new InvalidBenchmarkDeclarationException("Assembly not provided.");

            var benchmarkTypes = assembly.GetRunnableBenchmarks();

            if (benchmarkTypes.IsEmpty())
                throw new InvalidBenchmarkDeclarationException("No benchmarks to choose from. Make sure you provided public non-sealed non-static types with public [Benchmark] methods.");

            return benchmarkTypes.Select(t => BenchmarkConverter.TypeToBenchmarks(t, config)).ToArray();
        }

        private static BenchmarkRunInfo[] TypesToBenchmarks(Type[] types, IConfig config)
        {
            if (types.IsNullOrEmpty())
                throw new InvalidBenchmarkDeclarationException("No types provided.");

            var nonBenchmarkTypes = types.Where(t => !t.ContainsRunnableBenchmarks()).ToArray();

            if (!nonBenchmarkTypes.IsEmpty())
                throw new InvalidBenchmarkDeclarationException(string.Join(",", nonBenchmarkTypes.Select(type => type.Name))
                                                               + " should be public non-sealed non-static types with public [Benchmark] methods.");

            return types.Select(t => BenchmarkConverter.TypeToBenchmarks(t, config)).ToArray();
        }
    }
}
