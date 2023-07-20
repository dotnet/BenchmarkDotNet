using System.Reflection;
using System.Runtime.CompilerServices;

namespace BenchmarkDotNet.Portability
{
    public static class CodeGenHelper
    {
        // AggressiveOptimization is not available in netstandard2.0, so just use the value casted to enum.
        public const MethodImplOptions AggressiveOptimizationOption = (MethodImplOptions) 512;
        public const MethodImplAttributes AggressiveOptimizationOptionForEmit = (MethodImplAttributes) 512;
    }
}
