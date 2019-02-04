using System;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;

namespace BenchmarkDotNet.Helpers.Reflection.Emit
{
    internal static class TypeBuilderExtensions
    {
        private static void DefineParameters(this ConstructorBuilder constructorBuilder, params ParameterInfo[] parameters)
        {
            foreach (var parameterInfo in parameters)
            {
                constructorBuilder.DefineParameter(parameterInfo.Position + 1, parameterInfo.Attributes, parameterInfo.Name);
            }
        }

        private static void DefineParameters(this MethodBuilder methodBuilder, ParameterInfo returnType, params ParameterInfo[] parameters)
        {
            foreach (var parameterInfo in parameters)
            {
                methodBuilder.DefineParameter(parameterInfo.Position + 1, parameterInfo.Attributes, parameterInfo.Name);
            }

            methodBuilder.DefineParameter(0, returnType.Attributes, "");
        }

        public static MethodInfo GetDelegateInvokeMethod(Type delegateType)
        {
            var result = delegateType.GetMethod(nameof(Action.Invoke));
            if (result == null)
                throw new ArgumentException($"The type {delegateType} nas no Invoke method.", nameof(delegateType));

            return result;
        }

        public static ConstructorBuilder DefinePublicInstanceCtor(this TypeBuilder typeBuilder, params ParameterInfo[] parameters)
        {
            // .method public hidebysig specialname rtspecialname
            //     instance void.ctor() cil managed
            var result = typeBuilder.DefineConstructor(
                MethodAttributes.Public
                | MethodAttributes.HideBySig
                | MethodAttributes.SpecialName
                | MethodAttributes.RTSpecialName,
                CallingConventions.HasThis,
                parameters.Select(p => p.ParameterType).ToArray());
            result.DefineParameters(parameters);

            return result;
        }

        public static MethodBuilder DefinePublicNonVirtualVoidInstanceMethod(
            this TypeBuilder typeBuilder,
            string name,
            params ParameterInfo[] parameters)
        {
            return DefineNonVirtualInstanceMethod(
                typeBuilder,
                name,
                MethodAttributes.Public,
                EmitParameterInfo.CreateReturnVoidParameter(),
                parameters);
        }

        public static MethodBuilder DefinePrivateVoidInstanceMethod(
            this TypeBuilder typeBuilder,
            string name,
            params ParameterInfo[] parameters)
        {
            return DefineNonVirtualInstanceMethod(
                typeBuilder,
                name,
                MethodAttributes.Private,
                EmitParameterInfo.CreateReturnVoidParameter(),
                parameters);
        }

        public static MethodBuilder DefineNonVirtualInstanceMethod(
            this TypeBuilder typeBuilder,
            string name,
            MethodAttributes visibility,
            ParameterInfo returnType,
            params ParameterInfo[] parameters)
        {
            // .method public hidebysig instance
            var result = typeBuilder.DefineMethod(
                name,
                visibility
                | MethodAttributes.HideBySig,
                CallingConventions.HasThis,
                returnType.ParameterType,
                parameters.Select(p => p.ParameterType).ToArray()
            );
            result.DefineParameters(returnType, parameters);

            return result;
        }

        public static MethodBuilder DefineStaticMethod(
            this TypeBuilder typeBuilder,
            string name,
            MethodAttributes visibility,
            ParameterInfo returnType,
            params ParameterInfo[] parameters)
        {
            // .method public hidebysig static
            var result = typeBuilder.DefineMethod(
                name,
                visibility
                | MethodAttributes.HideBySig
                | MethodAttributes.Static,
                CallingConventions.Standard,
                returnType.ParameterType,
                parameters.Select(p => p.ParameterType).ToArray()
            );
            result.DefineParameters(returnType, parameters);

            return result;
        }
    }
}