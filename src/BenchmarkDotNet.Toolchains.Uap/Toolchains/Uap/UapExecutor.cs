#if !UAP
using System;
using System.Linq;
using BenchmarkDotNet.Characteristics;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Toolchains.Results;
using System.IO;

namespace BenchmarkDotNet.Toolchains.Uap
{
    internal class UapExecutor : IExecutor
    {
        private readonly UapToolchainConfig config;

        public UapExecutor(UapToolchainConfig config)
        {
            this.config = config;
        }

        public ExecuteResult Execute(BuildResult buildResult, Benchmark benchmark, ILogger logger,
            IResolver resolver, IDiagnoser diagnoser = null)
        {
            DevicePortalApiWrapper dpClient = null;
            DevicePortalApiWrapper.PackageStruct app = null;
            try
            {
                if (config.Pin != null)
                {
                    dpClient = new DevicePortalApiWrapper(config.Pin, config.DevicePortalUri);
                }
                else
                {
                    dpClient = new DevicePortalApiWrapper(config.CSRFCookieValue, config.WMIDCookieValue, config.DevicePortalUri);
                }
                
                app = dpClient.DeployApplication(Path.Combine(buildResult.ArtifactsPaths.BinariesDirectoryPath, @"AppPackages\UapBenchmarkProject_1.0.0.0_ARM_Test\UapBenchmarkProject_1.0.0.0_ARM.appx"));

                dpClient.DeleteApplication(app);
                app = dpClient.DeployApplication(Path.Combine(buildResult.ArtifactsPaths.BinariesDirectoryPath, @"AppPackages\UapBenchmarkProject_1.0.0.0_ARM_Test\UapBenchmarkProject_1.0.0.0_ARM.appx"));

                var ct = dpClient.StartListening();
                dpClient.RunApplication(app);
                string[] allStrings = dpClient.StopListening(ct);

                string[] resultStrings = allStrings.Where(x => !x.StartsWith("//")).ToArray();
                string[] extraStrings = allStrings.Where(x => x.StartsWith("//")).ToArray();
                return new ExecuteResult(true, 0, resultStrings, allStrings);
            }
            finally
            {
                if (app != null && dpClient != null)
                {
                    dpClient.DeleteApplication(app);
                }
            }
        }
    }
}
#endif