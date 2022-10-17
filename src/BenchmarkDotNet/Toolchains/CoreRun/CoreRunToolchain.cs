using System;
using System.Collections.Generic;
using System.IO;
using BenchmarkDotNet.Characteristics;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Toolchains.DotNetCli;
using BenchmarkDotNet.Validators;

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
        public CoreRunToolchain(FileInfo coreRun, bool createCopy = true,
            string targetFrameworkMoniker = "netcoreapp2.1",
            FileInfo customDotNetCliPath = null, DirectoryInfo restorePath = null,
            string displayName = "CoreRun")
        {
            if (coreRun == null) throw new ArgumentNullException(nameof(coreRun));
            if (!coreRun.Exists) throw new FileNotFoundException("Provided CoreRun path does not exist. Please remember that BDN expects path to CoreRun.exe (corerun on Unix), not to Core_Root folder.");

            SourceCoreRun = coreRun;
            CopyCoreRun = createCopy ? GetShadowCopyPath(coreRun) : coreRun;
            CustomDotNetCliPath = customDotNetCliPath;
            RestorePath = restorePath;

            Name = displayName;
            Generator = new CoreRunGenerator(SourceCoreRun, CopyCoreRun, targetFrameworkMoniker, customDotNetCliPath?.FullName, restorePath?.FullName);
            Builder = new CoreRunPublisher(CopyCoreRun, customDotNetCliPath);
            Executor = new DotNetCliExecutor(customDotNetCliPath: CopyCoreRun.FullName); // instead of executing "dotnet $pathToDll" we do "CoreRun $pathToDll"
        }

        public string Name { get; }

        public IGenerator Generator { get; }

        public IBuilder Builder { get; }

        public IExecutor Executor { get; }

        public bool IsInProcess => false;

        public FileInfo SourceCoreRun { get; }

        public FileInfo CopyCoreRun { get; }

        public FileInfo CustomDotNetCliPath { get; }

        public DirectoryInfo RestorePath { get; }

        public override string ToString() => Name;

        public IEnumerable<ValidationError> Validate(BenchmarkCase benchmark, IResolver resolver)
        {
            if (!SourceCoreRun.Exists)
            {
                yield return new ValidationError(true,
                    $"Provided CoreRun path does not exist, benchmark '{benchmark.DisplayInfo}' will not be executed. Please remember that BDN expects path to CoreRun.exe (corerun on Unix), not to Core_Root folder.",
                    benchmark);
            }
            else if (Toolchain.IsCliPathInvalid(CustomDotNetCliPath?.FullName, benchmark, out var invalidCliError))
            {
                yield return invalidCliError;
            }
        }

        private static FileInfo GetShadowCopyPath(FileInfo coreRunPath)
        {
            string randomSubfolderName = Guid.NewGuid().ToString();

            FileInfo coreRunCopy = coreRunPath.Directory.Parent != null
                ? new FileInfo(Path.Combine(coreRunPath.Directory.Parent.FullName, randomSubfolderName, coreRunPath.Name))
                : new FileInfo(Path.Combine(coreRunPath.Directory.FullName, randomSubfolderName, coreRunPath.Name)); // C:\CoreRun.exe case

            if (!TryToCreateSubfolder(coreRunCopy.Directory))
            {
                // we are most likely missing permissions to write to given folder (it can be readonly etc)
                // in such case, CoreRun copy is going to be stored in TEMP
                coreRunCopy = new FileInfo(Path.Combine(Path.GetTempPath(), randomSubfolderName, coreRunPath.Name));

                if (!TryToCreateSubfolder(coreRunCopy.Directory))
                {
                    // if even that is impossible, we return the original path and nothing is going to be copied
                    return coreRunPath;
                }
            }

            return coreRunCopy;

            static bool TryToCreateSubfolder(DirectoryInfo directory)
            {
                try
                {
                    if (!directory.Exists)
                    {
                        directory.Create();
                    }

                    return true;
                }
                catch
                {
                    return false;
                }
            }
        }
    }
}