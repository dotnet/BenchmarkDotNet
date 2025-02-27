using System;
using System.IO;
using System.Linq;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using Mono.Cecil;

namespace BenchmarkDotNet.Weaver;

internal class CustomAssemblyResolver : DefaultAssemblyResolver
{
    public override AssemblyDefinition Resolve(AssemblyNameReference name, ReaderParameters parameters)
        // NetStandard causes StackOverflow. https://github.com/jbevain/cecil/issues/573
        // Mscorlib fails to resolve in Visual Studio. https://github.com/jbevain/cecil/issues/966
        // We don't care about any types from runtime assemblies anyway, so just skip resolving them.
        => name.Name is "netstandard" or "mscorlib" or "System.Runtime" or "System.Private.CoreLib"
            ? null
            : base.Resolve(name, parameters);
}

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

        var resolver = new CustomAssemblyResolver();
        resolver.AddSearchDirectory(TargetDir);

        // ReaderParameters { ReadWrite = true } is necessary to later write the file.
        // https://stackoverflow.com/questions/41840455/locked-target-assembly-with-mono-cecil-and-pcl-code-injection
        var readerParameters = new ReaderParameters
        {
            ReadWrite = true,
            AssemblyResolver = resolver
        };

        bool benchmarkMethodsImplAdjusted = false;
        try
        {
            using var module = ModuleDefinition.ReadModule(TargetAssembly, readerParameters);

            foreach (var type in module.Types)
            {
                ProcessType(type, ref benchmarkMethodsImplAdjusted);
            }

            // Write the modified assembly to file.
            module.Write();
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

    private static void ProcessType(TypeDefinition type, ref bool benchmarkMethodsImplAdjusted)
    {
        // We can skip non-public types as they are not valid for benchmarks.
        if (type.IsNotPublic)
        {
            return;
        }

        // Remove AggressiveInlining and add NoInlining to all [Benchmark] methods.
        foreach (var method in type.Methods)
        {
            if (method.CustomAttributes.Any(IsBenchmarkAttribute))
            {
                var oldImpl = method.ImplAttributes;
                method.ImplAttributes = (oldImpl & ~MethodImplAttributes.AggressiveInlining) | MethodImplAttributes.NoInlining;
                benchmarkMethodsImplAdjusted |= (oldImpl & MethodImplAttributes.NoInlining) == 0;
            }
        }

        // Recursively process nested types
        foreach (var nestedType in type.NestedTypes)
        {
            ProcessType(nestedType, ref benchmarkMethodsImplAdjusted);
        }
    }

    private static bool IsBenchmarkAttribute(CustomAttribute attribute)
    {
        // BenchmarkAttribute is unsealed, so we need to walk its hierarchy.
        for (var attr = attribute.AttributeType; attr != null; attr = attr.Resolve()?.BaseType)
        {
            if (attr.FullName == "BenchmarkDotNet.Attributes.BenchmarkAttribute")
            {
                return true;
            }
        }
        return false;
    }
}