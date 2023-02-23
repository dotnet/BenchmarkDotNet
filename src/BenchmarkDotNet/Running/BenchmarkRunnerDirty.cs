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
        public static Summary Run<T>(IConfig? config = null, string[]? args = null)
        {
            using (DirtyAssemblyResolveHelper.Create())
                return RunWithExceptionHandling(() => RunWithDirtyAssemblyResolveHelper(typeof(T), config, args));
        }

        [PublicAPI]
        public static Summary Run(Type type, IConfig? config = null, string[]? args = null)
        {
            using (DirtyAssemblyResolveHelper.Create())
                return RunWithExceptionHandling(() => RunWithDirtyAssemblyResolveHelper(type, config, args));
        }

        [PublicAPI]
        public static Summary[] Run(Type[] types, IConfig? config = null, string[]? args = null)
        {
            using (DirtyAssemblyResolveHelper.Create())
                return RunWithExceptionHandling(() => RunWithDirtyAssemblyResolveHelper(types, config, args));
        }

        [PublicAPI]
        public static Summary Run(Type type, MethodInfo[] methods, IConfig? config = null)
        {
            using (DirtyAssemblyResolveHelper.Create())
                return RunWithExceptionHandling(() => RunWithDirtyAssemblyResolveHelper(type, methods, config));
        }

        [PublicAPI]
        public static Summary[] Run(Assembly assembly, IConfig? config = null, string[]? args = null)
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

        [PublicAPI]
        [EditorBrowsable(EditorBrowsableState.Never)]
        [Obsolete("This method will be removed soon as it is not supported in .NET Core")]
        public static Summary RunUrl(string url, IConfig? config = null)
        {
            using (DirtyAssemblyResolveHelper.Create())
                return RunWithExceptionHandling(() => RunUrlWithDirtyAssemblyResolveHelper(url, config));
        }

        [PublicAPI]
        [EditorBrowsable(EditorBrowsableState.Never)]
        [Obsolete("This method will be removed soon as it is not supported in .NET Core")]
        public static Summary RunSource(string source, IConfig? config = null)
        {
            using (DirtyAssemblyResolveHelper.Create())
                return RunWithExceptionHandling(() => RunSourceWithDirtyAssemblyResolveHelper(source, config));
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static Summary RunWithDirtyAssemblyResolveHelper(Type type, IConfig? config, string[]? args)
            => (args == null
                ? BenchmarkRunnerClean.Run(new[] { BenchmarkConverter.TypeToBenchmarks(type, config) })
                : new BenchmarkSwitcher(new[] { type }).RunWithDirtyAssemblyResolveHelper(args, config, false))
                .Single();

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static Summary RunWithDirtyAssemblyResolveHelper(Type type, MethodInfo[] methods, IConfig? config = null)
            => BenchmarkRunnerClean.Run(new[] { BenchmarkConverter.MethodsToBenchmarks(type, methods, config) }).Single();

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static Summary[] RunWithDirtyAssemblyResolveHelper(Assembly assembly, IConfig? config, string[]? args)
            => args == null
                ? BenchmarkRunnerClean.Run(assembly.GetRunnableBenchmarks().Select(type => BenchmarkConverter.TypeToBenchmarks(type, config)).ToArray())
                : new BenchmarkSwitcher(assembly).RunWithDirtyAssemblyResolveHelper(args, config, false).ToArray();

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static Summary[] RunWithDirtyAssemblyResolveHelper(Type[] types, IConfig? config, string[]? args)
            => args == null
                ? BenchmarkRunnerClean.Run(types.Select(type => BenchmarkConverter.TypeToBenchmarks(type, config)).ToArray())
                : new BenchmarkSwitcher(types).RunWithDirtyAssemblyResolveHelper(args, config, false).ToArray();

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static Summary[] RunWithDirtyAssemblyResolveHelper(BenchmarkRunInfo[] benchmarkRunInfos)
            => BenchmarkRunnerClean.Run(benchmarkRunInfos);

#pragma warning disable CS0618 // Use of obsolete symbol
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static Summary RunUrlWithDirtyAssemblyResolveHelper(string url, IConfig? config = null)
            => RuntimeInformation.IsFullFramework
                ? BenchmarkRunnerClean.Run(BenchmarkConverter.UrlToBenchmarks(url, config)).Single()
                : throw new InvalidBenchmarkDeclarationException("Supported only on Full .NET Framework");

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static Summary RunSourceWithDirtyAssemblyResolveHelper(string source, IConfig? config = null)
            => RuntimeInformation.IsFullFramework
                ? BenchmarkRunnerClean.Run(BenchmarkConverter.SourceToBenchmarks(source, config)).Single()
                : throw new InvalidBenchmarkDeclarationException("Supported only on Full .NET Framework");
#pragma warning restore CS0618 // Use of obsolete symbol

        private static Summary RunWithExceptionHandling(Func<Summary> run)
        {
            try
            {
                return run();
            }
            catch (InvalidBenchmarkDeclarationException e)
            {
                ConsoleLogger.Default.WriteLineError(e.Message);
                return Summary.ValidationFailed(e.Message, string.Empty, string.Empty);
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
                return new[] { Summary.ValidationFailed(e.Message, string.Empty, string.Empty) };
            }
        }
    }
}
