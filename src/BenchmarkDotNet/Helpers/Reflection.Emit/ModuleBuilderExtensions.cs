using System;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;

namespace BenchmarkDotNet.Helpers.Reflection.Emit
{
    internal static class ModuleBuilderExtensions
    {
        public static Type EmitCustomDelegate(
            this ModuleBuilder moduleBuilder,
            string delegateTypeName,
            ParameterInfo returnType,
            ParameterInfo[] parameters)
        {
            // TODO: begin/end invoke ?
            var delegatePatternType = typeof(Action);

            var typeBuilder = moduleBuilder.DefineType(
                delegateTypeName,
                delegatePatternType.Attributes,
                delegatePatternType.BaseType);

            var ctorPattern = delegatePatternType.GetConstructors().Single();
            var ctorBuilder = typeBuilder.DefineConstructor(
                ctorPattern.Attributes,
                ctorPattern.CallingConvention,
                ctorPattern.GetParameterTypes());

            foreach (var parameterInfo in ctorPattern.GetParameters())
            {
                ctorBuilder.DefineParameter(parameterInfo.Position + 1, parameterInfo.Attributes, parameterInfo.Name);
            }

            ctorBuilder.SetImplementationFlags(ctorPattern.GetMethodImplementationFlags());

            var invokePatternMethod = TypeBuilderExtensions.GetDelegateInvokeMethod(delegatePatternType);

            var invokeBuilder = typeBuilder.DefineMethod(
                invokePatternMethod.Name,
                invokePatternMethod.Attributes,
                invokePatternMethod.CallingConvention,
                returnType.ParameterType,
                parameters.Select(p => p.ParameterType).ToArray());
            foreach (var parameterInfo in parameters)
            {
                invokeBuilder.DefineParameter(parameterInfo.Position + 1, parameterInfo.Attributes, parameterInfo.Name);
            }
            invokeBuilder.DefineParameter(0, returnType.Attributes, "");

            invokeBuilder.SetImplementationFlags(invokePatternMethod.GetMethodImplementationFlags());

#if NETFRAMEWORK
            return typeBuilder.CreateType();
#else
            return typeBuilder.CreateTypeInfo();
#endif
        }
    }
}