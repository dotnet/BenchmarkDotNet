using BenchmarkDotNet.Characteristics;
using BenchmarkDotNet.Environments;
using BenchmarkDotNet.Extensions;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Toolchains.DotNetCli;
using BenchmarkDotNet.Validators;

namespace BenchmarkDotNet.Toolchains.CoreRun
{
    public sealed class CoreRunToolchain : IToolchain, IHasSettings
    {
        private const string DefaultTargetFrameworkMoniker = "net11.0";

        private CoreRunToolchain(CoreRunSettings settings)
        {
            if (!settings.SourceCoreRun.Exists)
                throw new FileNotFoundException("Provided CoreRun path does not exist. Please remember that BDN expects path to CoreRun.exe (corerun on Unix), not to Core_Root folder.");

            Settings = settings;
            SourceCoreRun = settings.SourceCoreRun;
            CopyCoreRun = settings.CreateCopy ? GetShadowCopyPath(settings.SourceCoreRun) : settings.SourceCoreRun;

            // The build components receive the resolved settings (target framework moniker filled in); the original
            // `settings` is stored in Settings for equality and the settings column.
            var resolvedSettings = Resolve(settings, DefaultTargetFrameworkMoniker);
            var version = Version.Parse(resolvedSettings.TargetFrameworkMoniker.Replace("netcoreapp", "").Replace("net", ""));
            Runtime = CoreRuntime.From(version);
            Generator = new CoreRunGenerator(SourceCoreRun, CopyCoreRun, resolvedSettings);
            Builder = new CoreRunPublisher(resolvedSettings, CopyCoreRun);
            Executor = new DotNetCliExecutor(customDotNetCliPath: CopyCoreRun); // instead of executing "dotnet $pathToDll" we do "CoreRun $pathToDll"
        }

        /// <summary>Returns a toolchain that uses the provided CoreRun to execute .NET Core apps.</summary>
        public static CoreRunToolchain From(CoreRunSettings settings) => new(settings);

        // Fills the target framework moniker in with the default only when the user left it unset, avoiding the
        // settings copy otherwise. CoreRun has no runtime to derive it from - it parses the runtime out of the moniker.
        private static CoreRunSettings Resolve(CoreRunSettings settings, string fallbackTfm)
            => settings.TargetFrameworkMoniker.IsNotBlank() ? settings : settings with { TargetFrameworkMoniker = fallbackTfm };

        internal CoreRunSettings Settings { get; }

        ISettings IHasSettings.Settings => Settings;

        public Runtime Runtime { get; }

        public IGenerator Generator { get; }

        public IBuilder Builder { get; }

        public IExecutor Executor { get; }

        public bool IsInProcess => false;

        public FileInfo SourceCoreRun { get; }

        public FileInfo CopyCoreRun { get; }

        public override string ToString() => $"{Settings.DisplayName} {Runtime.Version}";

        public override bool Equals(object? obj)
            => obj is CoreRunToolchain other
            && Runtime.Equals(other.Runtime)
            && Settings.Equals(other.Settings);

        public override int GetHashCode() => HashCode.Combine(Runtime, Settings);

        public async IAsyncEnumerable<ValidationError> ValidateAsync(BenchmarkCase benchmark, IResolver resolver)
        {
            if (!SourceCoreRun.Exists)
            {
                yield return new ValidationError(true,
                    $"Provided CoreRun path does not exist, benchmark '{benchmark.DisplayInfo}' will not be executed. Please remember that BDN expects path to CoreRun.exe (corerun on Unix), not to Core_Root folder.",
                    benchmark);
            }
            else if (DotNetSdkValidator.IsCliPathInvalid(Settings.CliPath, benchmark, out var invalidCliError))
            {
                yield return invalidCliError;
            }
        }

        private static FileInfo GetShadowCopyPath(FileInfo coreRunPath)
        {
            string randomSubfolderName = Guid.NewGuid().ToString();

            FileInfo coreRunCopy = coreRunPath.Directory!.Parent != null
                ? new FileInfo(Path.Combine(coreRunPath.Directory.Parent.FullName, randomSubfolderName, coreRunPath.Name))
                : new FileInfo(Path.Combine(coreRunPath.Directory.FullName, randomSubfolderName, coreRunPath.Name)); // C:\CoreRun.exe case

            if (!TryToCreateSubfolder(coreRunCopy.Directory!))
            {
                // we are most likely missing permissions to write to given folder (it can be readonly etc)
                // in such case, CoreRun copy is going to be stored in TEMP
                coreRunCopy = new FileInfo(Path.Combine(Path.GetTempPath(), randomSubfolderName, coreRunPath.Name));

                if (!TryToCreateSubfolder(coreRunCopy.Directory!))
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
