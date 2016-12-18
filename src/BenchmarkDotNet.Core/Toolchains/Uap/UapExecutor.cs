using System;
using BenchmarkDotNet.Characteristics;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Toolchains.Results;
using System.IO;

namespace BenchmarkDotNet.Toolchains.Uap
{
#if !UAP
    internal class UapExecutor : IExecutor
    {
        public ExecuteResult Execute(BuildResult buildResult, Benchmark benchmark, ILogger logger,
            IResolver resolver, IDiagnoser diagnoser = null)
        {
            DevicePortalApiWrapper dpClient = null;
            DevicePortalApiWrapper.PackageStruct app = null;
            try
            {
                dpClient = new DevicePortalApiWrapper("zEMTyPRdx8hi664vnp9Qq8rWH9D3coMc", "4812916714250432482004543481302430336069675778629049400138865675", "https://192.168.1.51");
                app = dpClient.DeployApplication(Path.Combine(buildResult.ArtifactsPaths.BinariesDirectoryPath, @"AppPackages\UapBenchmarkProject_1.0.0.0_ARM_Test\UapBenchmarkProject_1.0.0.0_ARM.appx"));

                dpClient.RunApplication(app);

                return new ExecuteResult(true, 0, new string[0], new string[0]);
            }
            catch (Exception)
            {
                return new ExecuteResult(false, -1, new string[0], new string[0]);
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
#endif
}