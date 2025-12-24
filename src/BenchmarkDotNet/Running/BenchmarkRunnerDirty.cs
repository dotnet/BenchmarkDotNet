using System;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Engines;
using BenchmarkDotNet.Extensions;
using BenchmarkDotNet.Loggers;
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
            using var context = BenchmarkSynchronizationContext.CreateAndSetCurrent();
            return context.ExecuteUntilComplete(RunAsync<T>(config, args));
        }

        [PublicAPI]
        public static Summary Run(Type type, IConfig? config = null, string[]? args = null)
        {
            using var context = BenchmarkSynchronizationContext.CreateAndSetCurrent();
            return context.ExecuteUntilComplete(RunAsync(type, config, args));
        }

        [PublicAPI]
        public static Summary[] Run(Type[] types, IConfig? config = null, string[]? args = null)
        {
            using var context = BenchmarkSynchronizationContext.CreateAndSetCurrent();
            return context.ExecuteUntilComplete(RunAsync(types, config, args));
        }

        [PublicAPI]
        public static Summary Run(Type type, MethodInfo[] methods, IConfig? config = null)
        {
            using var context = BenchmarkSynchronizationContext.CreateAndSetCurrent();
            return context.ExecuteUntilComplete(RunAsync(type, methods, config));
        }

        [PublicAPI]
        public static Summary[] Run(Assembly assembly, IConfig? config = null, string[]? args = null)
        {
            using var context = BenchmarkSynchronizationContext.CreateAndSetCurrent();
            return context.ExecuteUntilComplete(RunAsync(assembly, config, args));
        }

        [PublicAPI]
        public static Summary Run(BenchmarkRunInfo benchmarkRunInfo)
        {
            using var context = BenchmarkSynchronizationContext.CreateAndSetCurrent();
            return context.ExecuteUntilComplete(RunAsync(benchmarkRunInfo));
        }

        [PublicAPI]
        public static Summary[] Run(BenchmarkRunInfo[] benchmarkRunInfos)
        {
            using var context = BenchmarkSynchronizationContext.CreateAndSetCurrent();
            return context.ExecuteUntilComplete(RunAsync(benchmarkRunInfos));
        }

        /// <summary>
        /// Runs async if any benchmark is async and ran in-process; otherwise runs sync.
        /// </summary>
        [PublicAPI]
        public static ValueTask<Summary> RunAsync<T>(IConfig? config = null, string[]? args = null)
            => RunAsync(typeof(T), config, args);

        /// <summary>
        /// Runs async if any benchmark is async and ran in-process; otherwise runs sync.
        /// </summary>
        [PublicAPI]
        public static async ValueTask<Summary> RunAsync(Type type, IConfig? config = null, string[]? args = null)
        {
            using (DirtyAssemblyResolveHelper.Create())
            {
                return await RunWithExceptionHandling(() => RunWithDirtyAssemblyResolveHelper(type, config, args));
            }
        }

        /// <summary>
        /// Runs async if any benchmark is async and ran in-process; otherwise runs sync.
        /// </summary>
        [PublicAPI]
        public static async ValueTask<Summary[]> RunAsync(Type[] types, IConfig? config = null, string[]? args = null)
        {
            using (DirtyAssemblyResolveHelper.Create())
            {
                return await RunWithExceptionHandling(() => RunWithDirtyAssemblyResolveHelper(types, config, args));
            }
        }

        /// <summary>
        /// Runs async if any benchmark is async and ran in-process; otherwise runs sync.
        /// </summary>
        [PublicAPI]
        public static async ValueTask<Summary> RunAsync(Type type, MethodInfo[] methods, IConfig? config = null)
        {
            using (DirtyAssemblyResolveHelper.Create())
            {
                return await RunWithExceptionHandling(() => RunWithDirtyAssemblyResolveHelper(type, methods, config));
            }
        }

        /// <summary>
        /// Runs async if any benchmark is async and ran in-process; otherwise runs sync.
        /// </summary>
        [PublicAPI]
        public static async ValueTask<Summary[]> RunAsync(Assembly assembly, IConfig? config = null, string[]? args = null)
        {
            using (DirtyAssemblyResolveHelper.Create())
            {
                return await RunWithExceptionHandling(() => RunWithDirtyAssemblyResolveHelper(assembly, config, args));
            }
        }

        /// <summary>
        /// Runs async if any benchmark is async and ran in-process; otherwise runs sync.
        /// </summary>
        [PublicAPI]
        public static async ValueTask<Summary> RunAsync(BenchmarkRunInfo benchmarkRunInfo)
            => (await RunAsync([benchmarkRunInfo])).Single();

        /// <summary>
        /// Runs async if any benchmark is async and ran in-process; otherwise runs sync.
        /// </summary>
        [PublicAPI]
        public static async ValueTask<Summary[]> RunAsync(BenchmarkRunInfo[] benchmarkRunInfos)
        {
            using (DirtyAssemblyResolveHelper.Create())
            {
                return await RunWithExceptionHandling(() => RunWithDirtyAssemblyResolveHelper(benchmarkRunInfos));
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static async ValueTask<Summary> RunWithDirtyAssemblyResolveHelper(Type type, IConfig? config, string[]? args)
        {
            var summaries = args == null
                ? await BenchmarkRunnerClean.Run([BenchmarkConverter.TypeToBenchmarks(type, config)])
                : await new BenchmarkSwitcher([type]).RunWithDirtyAssemblyResolveHelper(args, config, false);

            return summaries.SingleOrDefault()
                ?? Summary.ValidationFailed($"No benchmarks found in type '{type.Name}'", string.Empty, string.Empty);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static async ValueTask<Summary> RunWithDirtyAssemblyResolveHelper(Type type, MethodInfo[] methods, IConfig? config = null)
        {
            var summaries = await BenchmarkRunnerClean.Run([BenchmarkConverter.MethodsToBenchmarks(type, methods, config)]);

            return summaries.SingleOrDefault()
                ?? Summary.ValidationFailed($"No benchmarks found in type '{type.Name}'", string.Empty, string.Empty);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static async ValueTask<Summary[]> RunWithDirtyAssemblyResolveHelper(Assembly assembly, IConfig? config, string[]? args)
            => args == null
                ? await BenchmarkRunnerClean.Run(assembly.GetRunnableBenchmarks().Select(type => BenchmarkConverter.TypeToBenchmarks(type, config)).ToArray())
                : (await new BenchmarkSwitcher(assembly).RunWithDirtyAssemblyResolveHelper(args, config, false)).ToArray();

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static async ValueTask<Summary[]> RunWithDirtyAssemblyResolveHelper(Type[] types, IConfig? config, string[]? args)
            => args == null
                ? await BenchmarkRunnerClean.Run(types.Select(type => BenchmarkConverter.TypeToBenchmarks(type, config)).ToArray())
                : (await new BenchmarkSwitcher(types).RunWithDirtyAssemblyResolveHelper(args, config, false)).ToArray();

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static ValueTask<Summary[]> RunWithDirtyAssemblyResolveHelper(BenchmarkRunInfo[] benchmarkRunInfos)
            => BenchmarkRunnerClean.Run(benchmarkRunInfos);

        private static async ValueTask<Summary> RunWithExceptionHandling(Func<ValueTask<Summary>> run)
        {
            try
            {
                return await run();
            }
            catch (InvalidBenchmarkDeclarationException e)
            {
                ConsoleLogger.Default.WriteLineError(e.Message);
                return Summary.ValidationFailed(e.Message, string.Empty, string.Empty);
            }
        }

        private static async ValueTask<Summary[]> RunWithExceptionHandling(Func<ValueTask<Summary[]>> run)
        {
            try
            {
                return await run();
            }
            catch (InvalidBenchmarkDeclarationException e)
            {
                ConsoleLogger.Default.WriteLineError(e.Message);
                return [Summary.ValidationFailed(e.Message, string.Empty, string.Empty)];
            }
        }
    }
}
