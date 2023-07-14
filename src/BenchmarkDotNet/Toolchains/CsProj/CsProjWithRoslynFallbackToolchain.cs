using System.Collections.Generic;
using BenchmarkDotNet.Characteristics;
using BenchmarkDotNet.Portability;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Toolchains.Roslyn;
using BenchmarkDotNet.Validators;

namespace BenchmarkDotNet.Toolchains.CsProj
{
    /// <summary>
    /// This toolchain first attempts to use the <see cref="CsProjClassicNetToolchain"/>, then falls back to the <see cref="RoslynToolchain"/>.
    /// </summary>
    // Internal instead of public, because users should already know if their project can use CsProjClassicNetToolchain.
    internal class CsProjWithRoslynFallbackToolchain : Toolchain
    {
        internal CsProjWithRoslynFallbackToolchain(IToolchain csProjClassicNetToolchain)
            : base(csProjClassicNetToolchain.Name,
                new CsProjWithRoslynFallbackGenerator(csProjClassicNetToolchain.Generator, RoslynToolchain.Instance.Generator),
                new CsProjWithRoslynFallbackBuilder(csProjClassicNetToolchain.Builder, RoslynToolchain.Instance.Builder),
                new Executor())
        {
        }

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

            if (!RuntimeInformation.IsFullFramework)
            {
                yield return new ValidationError(true,
                    "The Roslyn toolchain is only supported on .NET Framework",
                    benchmarkCase);
            }
        }
    }
}