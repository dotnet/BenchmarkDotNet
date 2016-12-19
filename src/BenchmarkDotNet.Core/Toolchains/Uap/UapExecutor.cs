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
                dpClient = new DevicePortalApiWrapper("tsNwc9d5WOvuRpqSHL0Yr4iXaAnhEMrk", "1079552595942511295369298866917553000457380973401454626786566684", "https://192.168.1.51");
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