using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using BenchmarkDotNet.Engines;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Extensions;
using BenchmarkDotNet.Helpers;

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

        public string TargetTypeNamespace => string.IsNullOrWhiteSpace(Descriptor.Type.Namespace) ? string.Empty : $"using {Descriptor.Type.Namespace};";

        public string TargetTypeName => Descriptor.Type.GetCorrectCSharpTypeName();

        public string GlobalSetupMethodName => Descriptor.GlobalSetupMethod?.Name ?? EmptyAction;

        public string GlobalCleanupMethodName => Descriptor.GlobalCleanupMethod?.Name ?? EmptyAction;

        public string IterationSetupMethodName => Descriptor.IterationSetupMethod?.Name ?? EmptyAction;

        public string IterationCleanupMethodName => Descriptor.IterationCleanupMethod?.Name ?? EmptyAction;

        public abstract string ExtraDefines { get; }

        public abstract string TargetMethodReturnTypeNamespace { get; }

        protected virtual Type TargetMethodReturnType => Descriptor.Method.ReturnType;

        public virtual string TargetMethodReturnTypeName => TargetMethodReturnType.GetCorrectCSharpTypeName();

        public virtual string TargetMethodDelegate => Descriptor.Method.Name;

        public virtual string GetTargetMethodCall(string passArguments) => $"{Descriptor.Method.Name}({passArguments})";

        public virtual string ConsumeField => null;

        protected abstract Type IdleMethodReturnType { get; }

        public string IdleMethodReturnTypeName => IdleMethodReturnType.GetCorrectCSharpTypeName();

        public abstract string IdleImplementation { get; }

        public virtual bool UseRefKeyword => false;
    }

    internal class VoidDeclarationsProvider : DeclarationsProvider
    {
        public VoidDeclarationsProvider(Descriptor descriptor) : base(descriptor) { }

        public override string ExtraDefines => "#define RETURNS_VOID";

        public override string TargetMethodReturnTypeNamespace => string.Empty;

        protected override Type IdleMethodReturnType => typeof(void);

        public override string IdleImplementation => string.Empty;
    }

    internal class NonVoidDeclarationsProvider : DeclarationsProvider
    {
        public NonVoidDeclarationsProvider(Descriptor descriptor) : base(descriptor) { }

        public override string TargetMethodReturnTypeNamespace 
            => TargetMethodReturnType.Namespace == "System" // As "using System;" is always included in the template, don't emit it again
                || string.IsNullOrWhiteSpace(TargetMethodReturnType.Namespace) 
                    ? string.Empty 
                    : $"using {TargetMethodReturnType.Namespace};";

        public override string ConsumeField
            => !Consumer.IsConsumable(TargetMethodReturnType) && Consumer.HasConsumableField(TargetMethodReturnType, out var field)
                ? $".{field.Name}"
                : null;

        protected override Type IdleMethodReturnType
            => Consumer.IsConsumable(TargetMethodReturnType)
                ? TargetMethodReturnType
                : (Consumer.HasConsumableField(TargetMethodReturnType, out var field)
                    ? field.FieldType
                    : typeof(int)); // we return this simple type because creating bigger ValueType could take longer than benchmarked method itself

        public override string IdleImplementation
        {
            get
            {
                string value;
                var type = IdleMethodReturnType;
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
            => Consumer.IsConsumable(TargetMethodReturnType) || Consumer.HasConsumableField(TargetMethodReturnType, out var _)
                ? "#define RETURNS_CONSUMABLE"
                : "#define RETURNS_NON_CONSUMABLE_STRUCT";
    }

    internal class ByRefDeclarationsProvider : NonVoidDeclarationsProvider
    {
        public ByRefDeclarationsProvider(Descriptor descriptor) : base(descriptor) { }

        protected override Type IdleMethodReturnType => typeof(IntPtr);

        public override string TargetMethodReturnTypeName => base.TargetMethodReturnTypeName.Replace("&", string.Empty);

        public override string ConsumeField => null;

        public override string IdleImplementation => $"return default({nameof(IntPtr)});";

        public override string ExtraDefines => "#define RETURNS_BYREF";

        public override bool UseRefKeyword => true;
    }

    internal class TaskDeclarationsProvider : VoidDeclarationsProvider
    {
        public TaskDeclarationsProvider(Descriptor descriptor) : base(descriptor) { }

        // we use GetAwaiter().GetResult() because it's fastest way to obtain the result in blocking way, 
        // and will eventually throw actual exception, not aggregated one
        public override string TargetMethodDelegate
            => $"() => {{ {Descriptor.Method.Name}().GetAwaiter().GetResult(); }}";

        public override string GetTargetMethodCall(string passArguments) => $"{Descriptor.Method.Name}({passArguments}).GetAwaiter().GetResult()";

        protected override Type TargetMethodReturnType => typeof(void);
    }

    /// <summary>
    /// declarations provider for <see cref="Task{TResult}" /> and <see cref="ValueTask{TResult}" />
    /// </summary>
    internal class GenericTaskDeclarationsProvider : NonVoidDeclarationsProvider
    {
        public GenericTaskDeclarationsProvider(Descriptor descriptor) : base(descriptor) { }

        protected override Type TargetMethodReturnType => Descriptor.Method.ReturnType.GetTypeInfo().GetGenericArguments().Single();

        // we use GetAwaiter().GetResult() because it's fastest way to obtain the result in blocking way, 
        // and will eventually throw actual exception, not aggregated one
        public override string TargetMethodDelegate
            => $"() => {{ return {Descriptor.Method.Name}().GetAwaiter().GetResult(); }}";

        public override string GetTargetMethodCall(string passArguments) => $"{Descriptor.Method.Name}({passArguments}).GetAwaiter().GetResult()";
    }
}