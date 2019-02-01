using System;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Extensions;
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
        public static Summary Run<T>(IConfig config = null)
        {
            using (DirtyAssemblyResolveHelper.Create())
                return RunWithDirtyAssemblyResolveHelper(typeof(T), config);
        }

        [PublicAPI]
        public static Summary Run(Type type, IConfig config = null)
        {
            using (DirtyAssemblyResolveHelper.Create())
                return RunWithDirtyAssemblyResolveHelper(type, config);
        }

        [PublicAPI]
        public static Summary Run(Type type, MethodInfo[] methods, IConfig config = null)
        {
            using (DirtyAssemblyResolveHelper.Create())
                return RunWithDirtyAssemblyResolveHelper(type, methods, config);
        }

        [PublicAPI]
        public static Summary[] Run(Assembly assembly, IConfig config = null)
        {
            using (DirtyAssemblyResolveHelper.Create())
                return RunWithDirtyAssemblyResolveHelper(assembly, config);
        }

        [PublicAPI]
        public static Summary Run(BenchmarkRunInfo benchmarkRunInfo)
        {
            using (DirtyAssemblyResolveHelper.Create())
                return RunWithDirtyAssemblyResolveHelper(new[] { benchmarkRunInfo }).Single();
        }

        [PublicAPI]
        public static Summary[] Run(BenchmarkRunInfo[] benchmarkRunInfos)
        {
            using (DirtyAssemblyResolveHelper.Create())
                return RunWithDirtyAssemblyResolveHelper(benchmarkRunInfos);
        }

        [PublicAPI]
        public static Summary RunUrl(string url, IConfig config = null)
        {
            using (DirtyAssemblyResolveHelper.Create())
                return RunUrlWithDirtyAssemblyResolveHelper(url, config);
        }

        [PublicAPI]
        public static Summary RunSource(string source, IConfig config = null)
        {
            using (DirtyAssemblyResolveHelper.Create())
                return RunSourceWithDirtyAssemblyResolveHelper(source, config);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static Summary RunWithDirtyAssemblyResolveHelper(Type type, IConfig config)
            => BenchmarkRunnerClean.Run(new[] { BenchmarkConverter.TypeToBenchmarks(type, config) }).Single();

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
    }
}