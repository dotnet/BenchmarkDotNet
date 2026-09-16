using BenchmarkDotNet.Extensions;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace BenchmarkDotNet.Helpers;

internal static class ShadowCopyHelper
{
    /// <summary>
    /// Shadow copy is an AppDomain feature, so it can only happen on .NET Framework. It runs the assembly from a cache directory that holds that assembly alone,
    /// leaving the code base pointing at the file it was copied from - where the assembly's dependencies still are. #558
    /// </summary>
    internal static bool TryGetOriginalLocation(Assembly assembly, [NotNullWhen(true)] out string? originalLocation)
    {
#if !NET
        // The code base is not available for an assembly that was not loaded from a file, which is not a shadow copy either.
        if (Portability.RuntimeInformation.IsFullFramework && !assembly.IsDynamic)
            return TryGetOriginalLocation(assembly.CodeBase, assembly.Location, out originalLocation);
#endif
        originalLocation = null;
        return false;
    }

#if !NET
    /// <summary>
    /// The code base is a URI and the location a path, so the two are compared as paths.
    /// </summary>
    internal static bool TryGetOriginalLocation(string? codeBase, string? location, [NotNullWhen(true)] out string? originalLocation)
    {
        originalLocation = null;

        if (codeBase.IsBlank() || location.IsBlank() || !Uri.TryCreate(codeBase, UriKind.Absolute, out var codeBaseUri) || !codeBaseUri.IsFile)
            return false;

        string codeBasePath = Path.GetFullPath(codeBaseUri.LocalPath);
        if (string.Equals(codeBasePath, Path.GetFullPath(location), StringComparison.OrdinalIgnoreCase))
            return false;

        originalLocation = codeBasePath;
        return true;
    }
#endif
}
