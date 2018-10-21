using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using BenchmarkDotNet.Engines;
using BenchmarkDotNet.Extensions;
using BenchmarkDotNet.Helpers;
using BenchmarkDotNet.Running;

namespace BenchmarkDotNet.Code
{
    internal abstract class DeclarationsProvider
    {
        // "GlobalSetup" or "GlobalCleanup" methods are optional, so default to an empty delegate, so there is always something that can be invoked
        private const string EmptyAction = "() => { }";

        protected readonly Descriptor Descriptor;

        internal DeclarationsProvider(Descriptor descriptor)
        {
            Descriptor = descriptor;
        }

        public string OperationsPerInvoke => Descriptor.OperationsPerInvoke.ToString();

        public string WorkloadTypeNamespace => string.IsNullOrWhiteSpace(Descriptor.Type.Namespace) ? string.Empty : $"using {Descriptor.Type.Namespace};";

        public string WorkloadTypeName => Descriptor.Type.GetCorrectCSharpTypeName();

        public string GlobalSetupMethodName => GetMethodName(Descriptor.GlobalSetupMethod);

        public string GlobalCleanupMethodName => Descriptor.GlobalCleanupMethod?.Name ?? EmptyAction;

        public string IterationSetupMethodName => Descriptor.IterationSetupMethod?.Name ?? EmptyAction;

        public string IterationCleanupMethodName => Descriptor.IterationCleanupMethod?.Name ?? EmptyAction;

        public abstract string ExtraDefines { get; }

        public abstract string WorkloadMethodReturnTypeNamespace { get; }

        protected virtual Type WorkloadMethodReturnType => Descriptor.WorkloadMethod.ReturnType;

        public virtual string WorkloadMethodReturnTypeName => WorkloadMethodReturnType.GetCorrectCSharpTypeName();

        public virtual string WorkloadMethodDelegate(string passArguments) => Descriptor.WorkloadMethod.Name;

        public virtual string GetWorkloadMethodCall(string passArguments) => $"{Descriptor.WorkloadMethod.Name}({passArguments})";

        public virtual string ConsumeField => null;

        protected abstract Type OverheadMethodReturnType { get; }

        public string OverheadMethodReturnTypeName => OverheadMethodReturnType.GetCorrectCSharpTypeName();

        public abstract string OverheadImplementation { get; }

        public virtual bool UseRefKeyword => false;

        public IEnumerable<string> ArgumentsNamespaces
            => Descriptor.WorkloadMethod.GetParameters()
                .Where(arg => !string.IsNullOrEmpty(arg.ParameterType.Namespace))
                .Select(arg => $"using {arg.ParameterType.Namespace};");

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
                return $"() => {method.Name}().GetAwaiter().GetResult()";
            }

            return method.Name;
        }
    }

    internal class VoidDeclarationsProvider : DeclarationsProvider
    {
        public VoidDeclarationsProvider(Descriptor descriptor) : base(descriptor) { }

        public override string ExtraDefines => "#define RETURNS_VOID";

        public override string WorkloadMethodReturnTypeNamespace => string.Empty;

        protected override Type OverheadMethodReturnType => typeof(void);

        public override string OverheadImplementation => string.Empty;
    }

    internal class NonVoidDeclarationsProvider : DeclarationsProvider
    {
        public NonVoidDeclarationsProvider(Descriptor descriptor) : base(descriptor) { }

        public override string WorkloadMethodReturnTypeNamespace
            => WorkloadMethodReturnType.Namespace == "System" // As "using System;" is always included in the template, don't emit it again
                || string.IsNullOrWhiteSpace(WorkloadMethodReturnType.Namespace)
                    ? string.Empty
                    : $"using {WorkloadMethodReturnType.Namespace};";

        public override string ConsumeField
            => !Consumer.IsConsumable(WorkloadMethodReturnType) && Consumer.HasConsumableField(WorkloadMethodReturnType, out var field)
                ? $".{field.Name}"
                : null;

        protected override Type OverheadMethodReturnType
            => Consumer.IsConsumable(WorkloadMethodReturnType)
                ? WorkloadMethodReturnType
                : (Consumer.HasConsumableField(WorkloadMethodReturnType, out var field)
                    ? field.FieldType
                    : typeof(int)); // we return this simple type because creating bigger ValueType could take longer than benchmarked method itself

        public override string OverheadImplementation
        {
            get
            {
                string value;
                var type = OverheadMethodReturnType;
                if (type.GetTypeInfo().IsPrimitive)
                    value = $"default({type.GetCorrectCSharpTypeName()})";
                else if (type.GetTypeInfo().IsClass || type.GetTypeInfo().IsInterface)
                    value = "null";
                else
                    value = SourceCodeHelper.ToSourceCode(Activator.CreateInstance(type)) + ";";
                return $"return {value};";
            }
        }

        public override string ExtraDefines
            => Consumer.IsConsumable(WorkloadMethodReturnType) || Consumer.HasConsumableField(WorkloadMethodReturnType, out _)
                ? "#define RETURNS_CONSUMABLE"
                : "#define RETURNS_NON_CONSUMABLE_STRUCT";
    }

    internal class ByRefDeclarationsProvider : NonVoidDeclarationsProvider
    {
        public ByRefDeclarationsProvider(Descriptor descriptor) : base(descriptor) { }

        protected override Type OverheadMethodReturnType => typeof(IntPtr);

        public override string WorkloadMethodReturnTypeName => base.WorkloadMethodReturnTypeName.Replace("&", string.Empty);

        public override string ConsumeField => null;

        public override string OverheadImplementation => $"return default({nameof(IntPtr)});";

        public override string ExtraDefines => "#define RETURNS_BYREF";

        public override bool UseRefKeyword => true;
    }

    internal class TaskDeclarationsProvider : VoidDeclarationsProvider
    {
        public TaskDeclarationsProvider(Descriptor descriptor) : base(descriptor) { }

        // we use GetAwaiter().GetResult() because it's fastest way to obtain the result in blocking way, 
        // and will eventually throw actual exception, not aggregated one
        public override string WorkloadMethodDelegate(string passArguments)
            => $"({passArguments}) => {{ {Descriptor.WorkloadMethod.Name}({passArguments}).GetAwaiter().GetResult(); }}";

        public override string GetWorkloadMethodCall(string passArguments) => $"{Descriptor.WorkloadMethod.Name}({passArguments}).GetAwaiter().GetResult()";

        protected override Type WorkloadMethodReturnType => typeof(void);
    }

    /// <summary>
    /// declarations provider for <see cref="Task{TResult}" /> and <see cref="ValueTask{TResult}" />
    /// </summary>
    internal class GenericTaskDeclarationsProvider : NonVoidDeclarationsProvider
    {
        public GenericTaskDeclarationsProvider(Descriptor descriptor) : base(descriptor) { }

        protected override Type WorkloadMethodReturnType => Descriptor.WorkloadMethod.ReturnType.GetTypeInfo().GetGenericArguments().Single();

        // we use GetAwaiter().GetResult() because it's fastest way to obtain the result in blocking way, 
        // and will eventually throw actual exception, not aggregated one
        public override string WorkloadMethodDelegate(string passArguments)
            => $"({passArguments}) => {{ return {Descriptor.WorkloadMethod.Name}({passArguments}).GetAwaiter().GetResult(); }}";

        public override string GetWorkloadMethodCall(string passArguments) => $"{Descriptor.WorkloadMethod.Name}({passArguments}).GetAwaiter().GetResult()";
    }
}