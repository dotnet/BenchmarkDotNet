using System;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
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
            AssertNotNull(type, nameof(type));

            using (DirtyAssemblyResolveHelper.Create())
                return RunWithExceptionHandling(() => RunWithDirtyAssemblyResolveHelper(type, config, args));
        }

        [PublicAPI]
        public static Summary[] Run(Type[] types, IConfig config = null, string[] args = null)
        {
            AssertNotNull(types, nameof(types));

            using (DirtyAssemblyResolveHelper.Create())
                return RunWithExceptionHandling(() => RunWithDirtyAssemblyResolveHelper(types, config, args));
        }

        [PublicAPI]
        public static Summary Run(Type type, MethodInfo[] methods, IConfig config = null)
        {
            AssertNotNull(type, nameof(type));
            AssertNotNull(methods, nameof(methods));

            using (DirtyAssemblyResolveHelper.Create())
                return RunWithExceptionHandling(() => RunWithDirtyAssemblyResolveHelper(type, methods, config));
        }

        [PublicAPI]
        public static Summary[] Run(Assembly assembly, IConfig config = null, string[] args = null)
        {
            AssertNotNull(assembly, nameof(assembly));

            using (DirtyAssemblyResolveHelper.Create())
                return RunWithExceptionHandling(() => RunWithDirtyAssemblyResolveHelper(assembly, config, args));
        }

        [PublicAPI]
        public static Summary Run(BenchmarkRunInfo benchmarkRunInfo)
        {
            AssertNotNull(benchmarkRunInfo, nameof(benchmarkRunInfo));

            using (DirtyAssemblyResolveHelper.Create())
                return RunWithExceptionHandling(() => RunWithDirtyAssemblyResolveHelper(new[] { benchmarkRunInfo }).Single());
        }

        [PublicAPI]
        public static Summary[] Run(BenchmarkRunInfo[] benchmarkRunInfos)
        {
            AssertNotNull(benchmarkRunInfos, nameof(benchmarkRunInfos));

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
        {
            Validate(type);

            return (args == null
                    ? BenchmarkRunnerClean.Run(new[] { BenchmarkConverter.TypeToBenchmarks(type, config) })
                    : new BenchmarkSwitcher(new[] { type }).RunWithDirtyAssemblyResolveHelper(args, config, false))
                .Single();
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static Summary RunWithDirtyAssemblyResolveHelper(Type type, MethodInfo[] methods, IConfig config = null)
        {
            Validate(type, methods);

            return BenchmarkRunnerClean.Run(new[] { BenchmarkConverter.MethodsToBenchmarks(type, methods, config) }).Single();
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static Summary[] RunWithDirtyAssemblyResolveHelper(Assembly assembly, IConfig config, string[] args)
        {
            Validate(assembly);

            return args == null
                ? BenchmarkRunnerClean.Run(assembly.GetRunnableBenchmarks().Select(type => BenchmarkConverter.TypeToBenchmarks(type, config)).ToArray())
                : new BenchmarkSwitcher(assembly).RunWithDirtyAssemblyResolveHelper(args, config, false).ToArray();
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static Summary[] RunWithDirtyAssemblyResolveHelper(Type[] types, IConfig config, string[] args)
        {
            Validate(types);

            return args == null
                ? BenchmarkRunnerClean.Run(types.Select(type => BenchmarkConverter.TypeToBenchmarks(type, config)).ToArray())
                : new BenchmarkSwitcher(types).RunWithDirtyAssemblyResolveHelper(args, config, false).ToArray();
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static Summary[] RunWithDirtyAssemblyResolveHelper(BenchmarkRunInfo[] benchmarkRunInfos)
        {
            Validate(benchmarkRunInfos);

            return BenchmarkRunnerClean.Run(benchmarkRunInfos);
        }

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

        private static void AssertNotNull<T>(T instance, string paramName)
        {
            if (instance is null)
                throw new ArgumentNullException(paramName);
        }

        private static void Validate(Type[] types)
        {
            if (types.IsEmpty())
                throw new InvalidBenchmarkDeclarationException("No types provided.");

            if (types.Any(t => t is null))
                throw new InvalidBenchmarkDeclarationException("Null not allowed.");

            var nonBenchmarkTypes = types.Where(t => !t.ContainsRunnableBenchmarks()).ToArray();
            if (!nonBenchmarkTypes.IsEmpty())
            {
                var invalidNames = string.Join("\n", nonBenchmarkTypes.Select(type => $"  {type.FullName}"));
                throw new InvalidBenchmarkDeclarationException($"Invalid Types:\n{invalidNames}\nOnly public, non-generic (closed generic types with public parameterless ctors are supported), non-abstract, non-sealed, non-static types with public instance [Benchmark] method(s) are supported.");
            }
        }

        private static void Validate(BenchmarkRunInfo[] benchmarkRunInfos)
        {
            if (benchmarkRunInfos.IsEmpty())
                throw new InvalidBenchmarkDeclarationException($"No BenchmarkRunInfos provided.");

            if (benchmarkRunInfos.Any(v => v is null))
                throw new InvalidBenchmarkDeclarationException($"Null not allowed.");

            foreach (var benchmarkRunInfo in benchmarkRunInfos)
            {
                if (benchmarkRunInfo.Config is null ||
                    benchmarkRunInfo.BenchmarksCases is null ||
                    benchmarkRunInfo.BenchmarksCases.IsEmpty() ||
                    benchmarkRunInfo.BenchmarksCases.Any(c => c is null))
                    throw new InvalidBenchmarkDeclarationException("BenchmarkRunInfo do not support null values.");
            }
        }

        private static void Validate(Type type)
        {
            if (!type.ContainsRunnableBenchmarks())
                throw new InvalidBenchmarkDeclarationException($"Type {type} is invalid. Only public, non-generic (closed generic types with public parameterless ctors are supported), non-abstract, non-sealed, non-static types with public instance [Benchmark] method(s) are supported.");
        }

        private static void Validate(Type type, MethodInfo[] methods)
        {
            if (!type.ContainsRunnableBenchmarks())
                throw new InvalidBenchmarkDeclarationException($"Type {type} is invalid. Only public, non-generic (closed generic types with public parameterless ctors are supported), non-abstract, non-sealed, non-static types with public instance [Benchmark] method(s) are supported.");

            if (methods.IsEmpty())
                throw new InvalidBenchmarkDeclarationException($"No methods provided for {type}.");

            if (methods.Any(m => m is null))
                throw new InvalidBenchmarkDeclarationException($"Null not allowed.");

            var benchmarkMethods = type.GetRunnableBenchmarks();
            var invalidMethods = methods.Except(benchmarkMethods).ToArray();
            if (!invalidMethods.IsEmpty())
            {
                var invalidNames = string.Join("\n", invalidMethods.Select(m => $"  {m.ReflectedType?.FullName}.{m.Name}"));
                throw new InvalidBenchmarkDeclarationException($"Invalid methods:\n{invalidNames}\nMethods must be of {type.FullName} type. Only public, non-generic (closed generic types with public parameterless ctors are supported), non-abstract, non-sealed, non-static types with public instance [Benchmark] method(s) are supported.");
            }
        }

        private static void Validate(Assembly assembly)
        {
            if (assembly.GetRunnableBenchmarks().IsEmpty())
                throw new InvalidBenchmarkDeclarationException("No benchmarks to choose from. Make sure you provided public non-sealed non-static types with public [Benchmark] methods.");
        }
    }
}
