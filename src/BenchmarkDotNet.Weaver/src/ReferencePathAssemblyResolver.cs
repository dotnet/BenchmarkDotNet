using System;
using System.Collections.Generic;
using System.IO;
using AsmResolver;
using AsmResolver.DotNet;

namespace BenchmarkDotNet.Weaver;

internal sealed class ReferencePathAssemblyResolver : IAssemblyResolver
{
    private readonly RuntimeContext _defaultRuntimeContext;
    private readonly Dictionary<Utf8String, AssemblyDefinition?> _cache = [];
    private readonly Dictionary<string, string> _referenceMap;

    public ReferencePathAssemblyResolver(RuntimeContext defaultRuntimeContext, string[] referencePaths)
    {
        _defaultRuntimeContext = defaultRuntimeContext;

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
        // Try default resolver first
        var status = _defaultRuntimeContext.AssemblyResolver.Resolve(assembly, originModule, out result);
        if (status != ResolutionStatus.AssemblyNotFound)
            return status;

        // Check cache
        if (_cache.TryGetValue(assembly.Name!, out var asmDef))
        {
            result = asmDef;
            return asmDef is not null
                ? ResolutionStatus.Success
                : ResolutionStatus.AssemblyNotFound;
        }

        // Try ReferencePath
        var simpleName = assembly.Name!.Value;
        if (_referenceMap.TryGetValue(simpleName, out var fullPath) && File.Exists(fullPath))
        {
            try
            {
                asmDef = AssemblyDefinition.FromFile(fullPath, _defaultRuntimeContext.DefaultReaderParameters, createRuntimeContext: false);
            }
            catch (BadImageFormatException)
            {
                result = null;
                return ResolutionStatus.AssemblyBadImage;
            }
            _cache[assembly.Name!] = asmDef;
            result = asmDef;
            return ResolutionStatus.Success;
        }

        // Not found
        result = null;
        _cache[assembly.Name!] = null;
        return ResolutionStatus.AssemblyNotFound;
    }
}
