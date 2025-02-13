using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using Mono.Cecil;

namespace BenchmarkDotNet.Weaver;

internal class CustomAssemblyResolver : DefaultAssemblyResolver
{
    public override AssemblyDefinition Resolve(AssemblyNameReference name, ReaderParameters parameters)
        // Fix StackOverflow https://github.com/jbevain/cecil/issues/573
        => name.FullName.StartsWith("netstandard") || name.FullName.StartsWith("mscorlib") || name.FullName.StartsWith("System.Private.CoreLib")
            ? AssemblyDefinition.ReadAssembly(Path.Combine(Path.GetDirectoryName(typeof(object).Assembly.Location), Path.ChangeExtension(name.Name, ".dll")), parameters)
            : base.Resolve(name, parameters);
}

/// <summary>
/// The Task used by MSBuild to weave the assemblies.
/// </summary>
public sealed class WeaveAssembliesTask : Task
{
    /// <summary>
    /// The directory of the output.
    /// </summary>
    [Required]
    public string TargetDir { get; set; }

    /// <summary>
    /// The path of the target assemblies.
    /// </summary>
    [Required]
    public string TargetAssembly { get; set; }

    private readonly List<string> _warningMessages = [$"Benchmark methods were found in 1 or more assemblies that require NoInlining, and assembly weaving failed."];

    /// <summary>
    /// Runs the weave assemblies task.
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

        ProcessAssembly(TargetAssembly, readerParameters, out bool isExecutable);

        foreach (var assemblyPath in Directory.GetFiles(TargetDir, "*.dll"))
        {
            if (assemblyPath == TargetAssembly)
            {
                continue;
            }
            ProcessAssembly(assemblyPath, readerParameters, out _);
        }

        // Assembly resolution can fail for library projects that contain references if the project does not have <CopyLocalLockFileAssemblies>true</CopyLocalLockFileAssemblies>.
        // Because the library project could be built as a dependency of the executable, we only log the warning if the target assembly is executable.
        if (_warningMessages.Count > 1 && isExecutable)
        {
            Log.LogWarning(string.Join(Environment.NewLine, _warningMessages));
        }
        return true;
    }

    private void ProcessAssembly(string assemblyPath, ReaderParameters readerParameters, out bool isExecutable)
    {
        isExecutable = false;
        bool benchmarkMethodsImplAdjusted = false;
        try
        {
            using var module = ModuleDefinition.ReadModule(assemblyPath, readerParameters);
            isExecutable = module.EntryPoint != null;

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
                _warningMessages.Add($"Assembly: {assemblyPath}, error: {e.Message}");
            }
        }
    }

    private void ProcessType(TypeDefinition type, ref bool benchmarkMethodsImplAdjusted)
    {
        // We can skip non-public types as they are not valid for benchmarks.
        if (type.IsNotPublic)
        {
            return;
        }

        // Remove AggressiveInlining and add NoInlining to all [Benchmark] methods.
        foreach (var method in type.Methods)
        {
            if (method.CustomAttributes.Any(attr => attr.AttributeType.FullName == "BenchmarkDotNet.Attributes.BenchmarkAttribute"))
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
}