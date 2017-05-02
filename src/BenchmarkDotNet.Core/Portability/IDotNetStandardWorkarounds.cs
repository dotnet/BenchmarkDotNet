using System.Reflection;

namespace BenchmarkDotNet.Portability
{
    /// <summary>
    /// .NET Standard 1.3 does not expose few things that are available for the frameworks that we target
    /// </summary>
    internal interface IDotNetStandardWorkarounds
    {
        string GetLocation(Assembly assembly);

        AssemblyName[] GetReferencedAssemblies(Assembly assembly);
    }
}