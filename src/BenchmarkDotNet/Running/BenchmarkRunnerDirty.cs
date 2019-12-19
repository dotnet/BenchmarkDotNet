using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.ConsoleArguments;
using BenchmarkDotNet.ConsoleArguments.ListBenchmarks;
using BenchmarkDotNet.Environments;
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
                return RunWithExceptionHandling(() => RunWithDirtyAssemblyResolveHelper(typeof(T), config, args ?? Array.Empty<string>()));
        }

        [PublicAPI]
        public static Summary Run(Type type, IConfig config = null, string[] args = null)
        {
            using (DirtyAssemblyResolveHelper.Create())
                return RunWithExceptionHandling(() => RunWithDirtyAssemblyResolveHelper(type, config, args ?? Array.Empty<string>()));
        }

        [PublicAPI]
        public static Summary Run(Assembly assembly, IConfig config = null, string[] args = null)
        {
            using (DirtyAssemblyResolveHelper.Create())
                return RunWithExceptionHandling(() => RunWithDirtyAssemblyResolveHelper(assembly, config, args ?? Array.Empty<string>()));
        }

        [PublicAPI]
        public static Summary Run(Type type, MethodInfo[] methods, IConfig config = null)
        {
            using (DirtyAssemblyResolveHelper.Create())
                return RunWithExceptionHandling(() => RunWithDirtyAssemblyResolveHelper(type, methods, config));
        }

        [PublicAPI]
        public static Summary[] Run(Assembly assembly, IConfig config = null)
        {
            using (DirtyAssemblyResolveHelper.Create())
                return RunWithExceptionHandling(() => RunWithDirtyAssemblyResolveHelper(assembly, config));
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
        public static Summary RunUrl(string url, IConfig config = null)
        {
            using (DirtyAssemblyResolveHelper.Create())
                return RunWithExceptionHandling(() => RunUrlWithDirtyAssemblyResolveHelper(url, config));
        }

        [PublicAPI]
        public static Summary RunSource(string source, IConfig config = null)
        {
            using (DirtyAssemblyResolveHelper.Create())
                return RunWithExceptionHandling(() => RunSourceWithDirtyAssemblyResolveHelper(source, config));
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static Summary RunWithDirtyAssemblyResolveHelper(Type type, IConfig config, string[] args)
            => RunWithDirtyAssemblyResolveHelper(new[] { type }, Array.Empty<Assembly>(), config, args);

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static Summary RunWithDirtyAssemblyResolveHelper(Assembly assembly, IConfig config, string[] args)
             => RunWithDirtyAssemblyResolveHelper(Array.Empty<Type>(), new[] { assembly }, config, args);

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static Summary RunWithDirtyAssemblyResolveHelper(Type type, MethodInfo[] methods, IConfig config = null)
            => BenchmarkRunnerClean.Run(new[] { BenchmarkConverter.MethodsToBenchmarks(type, methods, config) }).Single();
        
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static Summary[] RunWithDirtyAssemblyResolveHelper(Assembly assembly, IConfig config = null)
            => BenchmarkRunnerClean.Run(assembly.GetRunnableBenchmarks().Select(type => BenchmarkConverter.TypeToBenchmarks(type, config)).ToArray());

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static Summary[] RunWithDirtyAssemblyResolveHelper(BenchmarkRunInfo[] benchmarkRunInfos)
            => BenchmarkRunnerClean.Run(benchmarkRunInfos);

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static Summary RunUrlWithDirtyAssemblyResolveHelper(string url, IConfig config = null)
            => RuntimeInformation.IsFullFramework
                ? BenchmarkRunnerClean.Run(BenchmarkConverter.UrlToBenchmarks(url, config)).Single()
                : throw new NotSupportedException("Supported only on Full .NET Framework");

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static Summary RunSourceWithDirtyAssemblyResolveHelper(string source, IConfig config = null)
            => RuntimeInformation.IsFullFramework
                ? BenchmarkRunnerClean.Run(BenchmarkConverter.SourceToBenchmarks(source, config)).Single()
                : throw new NotSupportedException("Supported only on Full .NET Framework");

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

        private static Summary RunWithDirtyAssemblyResolveHelper(IEnumerable<Type> types, IEnumerable<Assembly> assemblies, IConfig config, string[] args)
        {
            var logger = config.GetNonNullCompositeLogger();
            var userInteraction = new UserInteraction();
            var (isParsingSuccess, parsedConfig, options) = ConfigParser.Parse(args, logger, config);
            if (!isParsingSuccess) // invalid console args, the ConfigParser printed the error
                return null;

            if (options.PrintInformation)
            {
                logger.WriteLine(HostEnvironmentInfo.GetInformation());
                return null;
            }

            var effectiveConfig = ManualConfig.Union(config, parsedConfig);
            var (allTypesValid, allAvailableTypesWithRunnableBenchmarks) = TypeFilter.GetTypesWithRunnableBenchmarks(types, assemblies, logger);
            
            if (!allTypesValid) // there were some invalid and TypeFilter printed errors
                return null;

            if (allAvailableTypesWithRunnableBenchmarks.IsEmpty())
            {
                userInteraction.PrintNoBenchmarksError(logger);
                return null;
            }

            if (options.ListBenchmarkCaseMode != ListBenchmarkCaseMode.Disabled)
            {
                BenchmarkCasesPrinter.PrintList(logger, effectiveConfig, allAvailableTypesWithRunnableBenchmarks, options);
                return null;
            }

            var BenchmarkRunInfos = new List<BenchmarkRunInfo[]>();

            foreach (Type type in types)
            {
                var BenchmarkRunInfo = options.UserProvidedFilters
                               ? TypeFilter.Filter(effectiveConfig, allAvailableTypesWithRunnableBenchmarks)
                               : new BenchmarkRunInfo[] { BenchmarkConverter.TypeToBenchmarks(type, config) };

                BenchmarkRunInfos.Add(BenchmarkRunInfo);
            }

            Summary benchmark = BenchmarkRunnerClean.Run(BenchmarkRunInfos.First()).Single();

            foreach (var BenchmarkRunInfo in BenchmarkRunInfos.Skip(1))
            {
                benchmark = BenchmarkRunnerClean.Run(BenchmarkRunInfo).Single();
            }

            return benchmark;
        }

    }
}
