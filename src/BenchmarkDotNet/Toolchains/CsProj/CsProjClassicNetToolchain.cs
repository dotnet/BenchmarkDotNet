using System.Collections.Generic;
using BenchmarkDotNet.Characteristics;
using BenchmarkDotNet.Portability;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Toolchains.DotNetCli;
using BenchmarkDotNet.Validators;
using JetBrains.Annotations;

namespace BenchmarkDotNet.Toolchains.CsProj
{
    /// <summary>
    /// this toolchain is designed for the new .csprojs, to build .NET 4.x benchmarks from the context of .NET Core host process
    /// it does not work with the old .csprojs or project.json!
    /// </summary>
    [PublicAPI]
    public class CsProjClassicNetToolchain : Toolchain
    {
        [PublicAPI] public static readonly IToolchain Net461 = new CsProjClassicNetToolchain("net461", ".NET Framework 4.6.1");
        [PublicAPI] public static readonly IToolchain Net462 = new CsProjClassicNetToolchain("net462", ".NET Framework 4.6.2");
        [PublicAPI] public static readonly IToolchain Net47 = new CsProjClassicNetToolchain("net47", ".NET Framework 4.7");
        [PublicAPI] public static readonly IToolchain Net471 = new CsProjClassicNetToolchain("net471", ".NET Framework 4.7.1");
        [PublicAPI] public static readonly IToolchain Net472 = new CsProjClassicNetToolchain("net472", ".NET Framework 4.7.2");
        [PublicAPI] public static readonly IToolchain Net48 = new CsProjClassicNetToolchain("net48", ".NET Framework 4.8");
        [PublicAPI] public static readonly IToolchain Net481 = new CsProjClassicNetToolchain("net481", ".NET Framework 4.8.1");

        private CsProjClassicNetToolchain(string targetFrameworkMoniker, string name, string packagesPath = null)
            : base(name,
                new CsProjGenerator(targetFrameworkMoniker, cliPath: null, packagesPath: packagesPath, runtimeFrameworkVersion: null, isNetCore: false),
                new DotNetCliBuilder(targetFrameworkMoniker, customDotNetCliPath: null),
                new Executor())
        {
        }

        public static IToolchain From(string targetFrameworkMoniker, string packagesPath = null)
            => new CsProjClassicNetToolchain(targetFrameworkMoniker, targetFrameworkMoniker, packagesPath);

        public override IEnumerable<ValidationError> Validate(BenchmarkCase benchmarkCase, IResolver resolver)
        {
            foreach (var validationError in base.Validate(benchmarkCase, resolver))
            {
                yield return validationError;
            }

            if (!RuntimeInformation.IsWindows())
            {
                yield return new ValidationError(true,
                    $"Classic .NET toolchain is supported only for Windows, benchmark '{benchmarkCase.DisplayInfo}' will not be executed",
                    benchmarkCase);
            }
            else if (IsCliPathInvalid(customDotNetCliPath: null, benchmarkCase, out var invalidCliError))
            {
                yield return invalidCliError;
            }
        }
    }
}