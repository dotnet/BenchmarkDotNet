using System;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;

namespace BenchmarkDotNet.Toolchains.InProcess.Emit.Implementation
{
    /// <summary>
    /// this class is a set of hack extensions that allow us to target .NET Standard 2.0 only and keep some .NET Framework-specific logic
    /// do not copy, it's a set of dirty hacks
    /// </summary>
    internal static class EmitExtensions
    {
        internal static void Save(this AssemblyBuilder assemblyBuilder, string assemblyFileName)
            => ExecuteMethodUsingReflection(assemblyBuilder, nameof(Save), assemblyFileName);

        internal static ModuleBuilder DefineDynamicModule(this AssemblyBuilder assemblyBuilder, string moduleName, string moduleFileName)
            => (ModuleBuilder)ExecuteMethodUsingReflection(assemblyBuilder, nameof(AssemblyBuilder.DefineDynamicModule), moduleName, moduleFileName);

        internal static AssemblyBuilder DefineDynamicAssembly(this AppDomain domain, AssemblyName assemblyName, AssemblyBuilderAccess assemblyMode, string assemblyDirectory)
            => (AssemblyBuilder)ExecuteMethodUsingReflection(domain, nameof(AssemblyBuilder.DefineDynamicAssembly), assemblyName, assemblyMode, assemblyDirectory);

        private static object ExecuteMethodUsingReflection<T>(T instance, string methodName, params object[] arguments)
        {
            var type = typeof(T);

            var method = type.GetMethod(name: methodName, types: arguments.Select(argument => argument.GetType()).ToArray());

            try
            {
                return method.Invoke(instance, parameters: arguments);
            }
            catch (TargetInvocationException wrappedByReflection)
            {
                throw wrappedByReflection.InnerException;
            }
        }
    }
}