// *****
// If any changes are made to this file, increment the WeaverVersionSuffix in the common.props file,
// then run `build.cmd pack-weaver`.
// *****

using AsmResolver.DotNet;
using AsmResolver.PE.DotNet.Metadata.Tables;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using System;
using System.IO;
using System.Linq;

namespace BenchmarkDotNet.Weaver;

/// <summary>
/// The Task used by MSBuild to weave the assembly.
/// </summary>
public sealed class WeaveAssemblyTask : Task
{
    /// <summary>
    /// The directory of the output.
    /// </summary>
    [Required]
    public string TargetDir { get; set; }

    /// <summary>
    /// The path of the target assembly.
    /// </summary>
    [Required]
    public string TargetAssembly { get; set; }

    /// <summary>
    /// Runs the weave assembly task.
    /// </summary>
    /// <returns><see langword="true"/> if successful; <see langword="false"/> otherwise.</returns>
    public override bool Execute()
    {
        if (!File.Exists(TargetAssembly))
        {
            Log.LogError($"Assembly not found: {TargetAssembly}");
            return false;
        }

        // Load the assembly using AsmResolver.
        var module = ModuleDefinition.FromFile(TargetAssembly);

        bool benchmarkMethodsImplAdjusted = false;
        try
        {
            foreach (var type in module.GetAllTypes())
            {
                // We can skip non-public types as they are not valid for benchmarks.
                if (type.IsNotPublic)
                {
                    continue;
                }

                foreach (var method in type.Methods)
                {
                    if (method.CustomAttributes.Any(IsBenchmarkAttribute))
                    {
                        var oldImpl = method.ImplAttributes;
                        // Remove AggressiveInlining and add NoInlining.
                        method.ImplAttributes = (oldImpl & ~MethodImplAttributes.AggressiveInlining) | MethodImplAttributes.NoInlining;
                        benchmarkMethodsImplAdjusted |= (oldImpl & MethodImplAttributes.NoInlining) == 0;
                    }
                }
            }

            // Write the modified assembly to file.
            module.Write(TargetAssembly);
        }
        catch (Exception e)
        {
            if (benchmarkMethodsImplAdjusted)
            {
                Log.LogWarning($"Benchmark methods were found that require NoInlining, and assembly weaving failed.{Environment.NewLine}{e}");
            }
        }
        return true;
    }

    private static bool IsBenchmarkAttribute(CustomAttribute attribute)
    {
        // BenchmarkAttribute is unsealed, so we need to walk its hierarchy.
        for (var attr = attribute.Constructor.DeclaringType; attr != null; attr = attr.Resolve()?.BaseType)
        {
            if (attr.FullName == "BenchmarkDotNet.Attributes.BenchmarkAttribute")
            {
                return true;
            }
        }
        return false;
    }
}