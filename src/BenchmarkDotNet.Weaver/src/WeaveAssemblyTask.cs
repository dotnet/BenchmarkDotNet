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


        bool benchmarkMethodsImplAdjusted = false;
        try
        {
            var module = ModuleDefinition.FromFile(TargetAssembly);

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

            if (benchmarkMethodsImplAdjusted)
            {
                // Write to a memory stream before overwriting the original file in case an exception occurs during the write (like unsupported platform).
                // https://github.com/Washi1337/AsmResolver/issues/640
                var memoryStream = new MemoryStream();
                try
                {
                    module.Write(memoryStream);
                    using var fileStream = new FileStream(TargetAssembly, FileMode.Truncate, FileAccess.Write);
                    memoryStream.WriteTo(fileStream);
                }
                catch (OutOfMemoryException)
                {
                    // If there is not enough memory, fallback to write to null stream then write to file.
                    memoryStream.Dispose();
                    memoryStream = null;
                    GC.Collect();
                    module.Write(Stream.Null);
                    module.Write(TargetAssembly);
                }
                finally
                {
                    memoryStream?.Dispose();
                }
            }
        }
        catch (Exception e)
        {
            Log.LogWarning($"Assembly weaving failed. Benchmark methods found requiring NoInlining: {benchmarkMethodsImplAdjusted}. Error:{Environment.NewLine}{e}");
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