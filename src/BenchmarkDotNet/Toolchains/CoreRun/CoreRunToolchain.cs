using System;
using System.IO;
using BenchmarkDotNet.Characteristics;
using BenchmarkDotNet.Environments;
using BenchmarkDotNet.Extensions;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Toolchains.DotNetCli;

namespace BenchmarkDotNet.Toolchains.CoreRun
{
    public class CoreRunToolchain : IToolchain
    {
        /// <summary>
        /// creates a CoreRunToolchain which is using provided CoreRun to execute .NET Core apps
        /// </summary>
        /// <param name="coreRun">the path to CoreRun</param>
        /// /<param name="createCopy">should a copy of CoreRun be performed? True by default. <remarks>The toolchain replaces old dependencies in CoreRun folder with newer versions if used by the benchmarks.</remarks></param>
        /// <param name="targetFrameworkMoniker">TFM, netcoreapp2.1 is the default</param>
        /// <param name="customDotNetCliPath">path to dotnet cli, if not provided the one from PATH will be used</param>
        /// <param name="displayName">display name, CoreRun is the default value</param>
        /// <param name="restorePath">the directory to restore packages to</param>
        /// <param name="timeout">the timeout for building the benchmarks</param>
        public CoreRunToolchain(FileInfo coreRun, bool createCopy = true,
            string targetFrameworkMoniker = "netcoreapp2.1", 
            FileInfo customDotNetCliPath = null, DirectoryInfo restorePath = null,
            string displayName = "CoreRun",
            TimeSpan? timeout = null) 
        {
            if (coreRun == null) throw new ArgumentNullException(nameof(coreRun));
            if (!coreRun.Exists) throw new FileNotFoundException("Provided CoreRun path does not exist");

            SourceCoreRun = coreRun;
            CopyCoreRun = createCopy ? GetShadowCopyPath(coreRun) : coreRun;
            CustomDotNetCliPath = customDotNetCliPath;
            RestorePath = restorePath;

            Name = displayName;
            Generator = new CoreRunGenerator(SourceCoreRun, CopyCoreRun, targetFrameworkMoniker, customDotNetCliPath?.FullName, restorePath?.FullName);
            Builder = new CoreRunPublisher(CopyCoreRun, customDotNetCliPath, timeout);
            Executor = new DotNetCliExecutor(customDotNetCliPath: CopyCoreRun.FullName); // instead of executing "dotnet $pathToDll" we do "CoreRun $pathToDll" 
        }

        public string Name { get; }
        
        public IGenerator Generator { get; }
        
        public IBuilder Builder { get; }
        
        public IExecutor Executor { get; }

        public FileInfo SourceCoreRun { get; }

        public FileInfo CopyCoreRun { get; }

        public FileInfo CustomDotNetCliPath { get; }
        
        public DirectoryInfo RestorePath { get; }

        public override string ToString() => Name;

        public bool IsSupported(BenchmarkCase benchmark, ILogger logger, IResolver resolver)
        {
            if (!SourceCoreRun.Exists)
            {
                logger.WriteLineError($"Provided CoreRun path does not exist, benchmark '{benchmark.DisplayInfo}' will not be executed");
                return false;
            }

            if (CustomDotNetCliPath == null && !HostEnvironmentInfo.GetCurrent().IsDotNetCliInstalled())
            {
                logger.WriteLineError($"BenchmarkDotNet requires dotnet cli toolchain to be installed, benchmark '{benchmark.DisplayInfo}' will not be executed");
                return false;
            }

            if (CustomDotNetCliPath.IsNotNullButDoesNotExist())
            {
                logger.WriteLineError($"Provided custom dotnet cli path does not exist, benchmark '{benchmark.DisplayInfo}' will not be executed");
                return false;
            }

            return true;
        }

        private static FileInfo GetShadowCopyPath(FileInfo coreRunPath)
            => coreRunPath.Directory.Parent != null
                ? new FileInfo(Path.Combine(coreRunPath.Directory.Parent.FullName, Guid.NewGuid().ToString(), coreRunPath.Name))
                : new FileInfo(Path.Combine(coreRunPath.Directory.FullName, Guid.NewGuid().ToString(), coreRunPath.Name)); // C:\CoreRun.exe case
    }
}