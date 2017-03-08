﻿#if !UAP
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
                else if(config.CSRFCookieValue != null)
                {
                    dpClient = new DevicePortalApiWrapper(config.CSRFCookieValue, config.WMIDCookieValue, config.DevicePortalUri);
                }
                else
                {
                    dpClient = new DevicePortalApiWrapper(username: config.Username, password: config.Password, devicePortalAddress: config.DevicePortalUri);
                }

                var ct = dpClient.StartListening();

                var appxFileName = $"AppPackages\\UapBenchmarkProject_1.0.0.0_{config.Platform}_Test\\UapBenchmarkProject_1.0.0.0_{config.Platform}.appx";
                app = dpClient.DeployApplication(Path.Combine(buildResult.ArtifactsPaths.BinariesDirectoryPath, appxFileName), config.Platform);
                
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