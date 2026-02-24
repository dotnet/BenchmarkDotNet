using System;
using System.Collections.Generic;
using System.IO;
using AsmResolver.DotNet;

namespace BenchmarkDotNet.Weaver;

internal sealed class ReferencePathAssemblyResolver : IAssemblyResolver
{
    private readonly Dictionary<string, string> _referenceMap;

    public ReferencePathAssemblyResolver(string[] referencePaths)
    {
        // Build a lookup: simpleName → fullPath
        _referenceMap = new(StringComparer.OrdinalIgnoreCase);
        foreach (var path in referencePaths)
        {
            var fileName = Path.GetFileNameWithoutExtension(path);
            if (!string.IsNullOrEmpty(fileName))
                _referenceMap[fileName] = path;
        }
    }

    public ResolutionStatus Resolve(AssemblyDescriptor assembly, ModuleDefinition? originModule, out AssemblyDefinition? result)
    {
        if (!_referenceMap.TryGetValue(assembly.Name!.Value, out var fullPath) || !File.Exists(fullPath))
        {
            result = null;
            return ResolutionStatus.AssemblyNotFound;
        }

        try
        {
            result = AssemblyDefinition.FromFile(fullPath, createRuntimeContext: false);
            return ResolutionStatus.Success;
        }
        catch (BadImageFormatException)
        {
            result = null;
            return ResolutionStatus.AssemblyBadImage;
        }
    }
}
