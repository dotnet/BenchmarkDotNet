using System.Reflection;
using System.Threading.Tasks;
using BenchmarkDotNet.Extensions;
using BenchmarkDotNet.Running;

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

        public virtual string GetWorkloadMethodCall(string passArguments) => $"{Descriptor.WorkloadMethod.Name}({passArguments})";

        private string GetMethodName(MethodInfo method)
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
                return $"() => BenchmarkDotNet.Helpers.AwaitHelper.GetResult({method.Name}())";
            }

            return method.Name;
        }
    }

    internal class SyncDeclarationsProvider : DeclarationsProvider
    {
        public SyncDeclarationsProvider(Descriptor descriptor) : base(descriptor) { }
    }

    internal class AsyncDeclarationsProvider : DeclarationsProvider
    {
        public AsyncDeclarationsProvider(Descriptor descriptor) : base(descriptor) { }

        public override string GetWorkloadMethodCall(string passArguments) => $"BenchmarkDotNet.Helpers.AwaitHelper.GetResult({Descriptor.WorkloadMethod.Name}({passArguments}))";
    }
}