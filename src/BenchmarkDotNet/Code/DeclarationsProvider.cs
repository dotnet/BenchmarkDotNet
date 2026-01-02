using System.Reflection;
using System.Threading.Tasks;
using BenchmarkDotNet.Extensions;
using BenchmarkDotNet.Running;

#nullable enable

namespace BenchmarkDotNet.Code
{
    internal abstract class DeclarationsProvider
    {
        // "GlobalSetup" or "GlobalCleanup" methods are optional, so default to an empty delegate, so there is always something that can be invoked
        private const string EmptyAction = "() => { }";

        protected readonly Descriptor Descriptor;

        internal DeclarationsProvider(Descriptor descriptor) => Descriptor = descriptor;

        public string OperationsPerInvoke => Descriptor.OperationsPerInvoke.ToString();

        public string WorkloadTypeName => Descriptor.Type.GetCorrectCSharpTypeName();

        public string GlobalSetupMethodName => GetMethodName(Descriptor.GlobalSetupMethod);

        public string GlobalCleanupMethodName => GetMethodName(Descriptor.GlobalCleanupMethod);

        public string IterationSetupMethodName => Descriptor.IterationSetupMethod?.Name ?? EmptyAction;

        public string IterationCleanupMethodName => Descriptor.IterationCleanupMethod?.Name ?? EmptyAction;

        public abstract string GetWorkloadMethodCall(string passArguments);

        protected static string GetMethodPrefix(MethodInfo method)
            => method.IsStatic ? method.DeclaringType!.GetCorrectCSharpTypeName() : "base";

        private string GetMethodName(MethodInfo? method)
        {
            if (method == null)
            {
                return EmptyAction;
            }

            if (method.ReturnType == typeof(Task) ||
                method.ReturnType == typeof(ValueTask) ||
                (method.ReturnType.IsGenericType &&
                    (method.ReturnType.GetGenericTypeDefinition() == typeof(Task<>) ||
                     method.ReturnType.GetGenericTypeDefinition() == typeof(ValueTask<>))))
            {
                return $"() => global::BenchmarkDotNet.Helpers.AwaitHelper.GetResult({GetMethodPrefix(Descriptor.WorkloadMethod)}.{method.Name}())";
            }

            return $"{GetMethodPrefix(Descriptor.WorkloadMethod)}.{method.Name}";
        }
    }

    internal class SyncDeclarationsProvider(Descriptor descriptor) : DeclarationsProvider(descriptor)
    {
        public override string GetWorkloadMethodCall(string passArguments)
             => $"{GetMethodPrefix(Descriptor.WorkloadMethod)}.{Descriptor.WorkloadMethod.Name}({passArguments})";
    }

    internal class AsyncDeclarationsProvider(Descriptor descriptor) : DeclarationsProvider(descriptor)
    {
        public override string GetWorkloadMethodCall(string passArguments)
            => $"global::BenchmarkDotNet.Helpers.AwaitHelper.GetResult({GetMethodPrefix(Descriptor.WorkloadMethod)}.{Descriptor.WorkloadMethod.Name}({passArguments}))";
    }
}