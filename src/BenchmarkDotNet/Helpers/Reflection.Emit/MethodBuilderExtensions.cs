using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;

namespace BenchmarkDotNet.Helpers.Reflection.Emit
{
    internal static class MethodBuilderExtensions
    {
        public static ParameterInfo[] GetEmitParameters(this MethodBuilder method, IEnumerable<ParameterInfo> signatureParameters) =>
            signatureParameters
                .Select(p =>
                    (ParameterInfo)new EmitParameterInfo(
                        p.Position,
                        p.Name,
                        p.ParameterType,
                        p.Attributes,
                        method))
                .ToArray();

        public static MethodBuilder SetNoInliningImplementationFlag(this MethodBuilder methodBuilder)
        {
            methodBuilder.SetImplementationFlags(
                methodBuilder.GetMethodImplementationFlags() | MethodImplAttributes.NoInlining);

            return methodBuilder;
        }

        public static MethodBuilder SetNoOptimizationImplementationFlag(this MethodBuilder methodBuilder)
        {
            methodBuilder.SetImplementationFlags(
                methodBuilder.GetMethodImplementationFlags() | MethodImplAttributes.NoOptimization);

            return methodBuilder;
        }
    }
}