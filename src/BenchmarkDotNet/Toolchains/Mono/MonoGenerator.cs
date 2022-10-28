using BenchmarkDotNet.Characteristics;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Toolchains.CsProj;

namespace BenchmarkDotNet.Toolchains.Mono
{
    public class MonoGenerator : CsProjGenerator
    {
        public MonoGenerator(string targetFrameworkMoniker, string cliPath, string packagesPath, string runtimeFrameworkVersion) : base(targetFrameworkMoniker, cliPath, packagesPath, runtimeFrameworkVersion, true)
        {
        }

        protected override string GetRuntimeSettings(GcMode gcMode, IResolver resolver)
        {
            // Workaround for 'Found multiple publish output files with the same relative path' error
            return base.GetRuntimeSettings(gcMode, resolver) + "<PropertyGroup><ErrorOnDuplicatePublishOutputFiles>false</ErrorOnDuplicatePublishOutputFiles></PropertyGroup>";
        }
    }
}
