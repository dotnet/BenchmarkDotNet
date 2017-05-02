using System.Reflection;
using BenchmarkDotNet.Portability;

namespace BenchmarkDotNet
{
    /*
     * CAUTION: this file is referenced as link in multiple projects just to avoid copying of the code
     */
    internal class DotNetStandardWorkarounds : IDotNetStandardWorkarounds
    {
        public string GetLocation(Assembly assembly) => assembly.Location;

        public AssemblyName[] GetReferencedAssemblies(Assembly assembly) => assembly.GetReferencedAssemblies();
    }
}