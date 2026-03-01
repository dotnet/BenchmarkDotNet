using BenchmarkDotNet.Filters;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Samples.Sandbox;
using BenchmarkDotNet.Toolchains.CsProj;
using System.Linq;

namespace BenchmarkDotNet;

/// <summary>
/// Custom BenchmarkDotNet filter to exclude benchmarks based on target framework category.
/// This filter is required because BenchmarkDotNet don't support conditional benchmarks with `#if` directive.
/// </summary>
public partial class TargetFrameworkFilter : IFilter
{
    public static readonly TargetFrameworkFilter Instance = new();

    private TargetFrameworkFilter() { }

    public virtual bool Predicate(BenchmarkCase benchmarkCase)
    {
        var toolchain = benchmarkCase.Job.Infrastructure.Toolchain;
        if (toolchain == null || toolchain is not CsProjCoreToolchain)
            return true;

        var versionText = toolchain.Name.Split(' ').Last(); // Expected `.NET 8.0` format.
        if (!Version.TryParse(versionText, out var targetVersion))
            return true; // Return true if failed to resolve target version.

        var categories = benchmarkCase.Descriptor.Categories;
        foreach (var category in categories)
        {
            switch (category)
            {
                case Categories.Filters.NET8_0_OR_GREATER:
                    return targetVersion.Major >= 8;
                case Categories.Filters.NET9_0_OR_GREATER:
                    return targetVersion.Major >= 9;
                case Categories.Filters.NET10_0_OR_GREATER:
                    return targetVersion.Major >= 10;
                default:
                    continue;
            }
        }

        return true;
    }
}
