using BenchmarkDotNet.Toolchains.CsProj;
using JetBrains.Annotations;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BenchmarkDotNet.Toolchains.MsBuildSdkExtras
{
    /// <summary>
    /// A csproj generator that will generate projects compatible with the MsBuild.Sdk.Extra format
    /// that will allow additional TargetFramework's to be targeted.
    /// </summary>
    [PublicAPI]
    public class MsBuildSdkExtrasGenerator : CsProjGenerator
    {
        public MsBuildSdkExtrasGenerator(string targetFrameworkMoniker, string cliPath, string packagesPath, string runtimeFrameworkVersion)
            : base(targetFrameworkMoniker, cliPath, packagesPath, runtimeFrameworkVersion)
        {
        }

        protected override string TemplateName => "MsBuildSdkExtrasCsProj.txt";
    }
}
