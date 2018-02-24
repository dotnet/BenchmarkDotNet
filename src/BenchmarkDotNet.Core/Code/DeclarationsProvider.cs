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

        protected readonly Target Target;

        internal DeclarationsProvider(Target target)
        {
            Target = target;
        }

        public string OperationsPerInvoke => Target.OperationsPerInvoke.ToString();

        public string TargetTypeNamespace => string.IsNullOrWhiteSpace(Target.Type.Namespace) ? string.Empty : $"using {Target.Type.Namespace};";

        public string TargetTypeName => Target.Type.GetCorrectTypeName();

        public string GlobalSetupMethodName => Target.GlobalSetupMethod?.Name ?? EmptyAction;

        public string GlobalCleanupMethodName => Target.GlobalCleanupMethod?.Name ?? EmptyAction;

        public string IterationSetupMethodName => Target.IterationSetupMethod?.Name ?? EmptyAction;

        public string IterationCleanupMethodName => Target.IterationCleanupMethod?.Name ?? EmptyAction;

        public virtual string ExtraDefines => null;

        public abstract string TargetMethodReturnTypeNamespace { get; }

        protected virtual Type TargetMethodReturnType => Target.Method.ReturnType;

        public virtual string TargetMethodReturnTypeName => TargetMethodReturnType.GetCorrectTypeName();

        public virtual string TargetMethodDelegate => Target.Method.Name;

        public virtual string TargetMethodCall => $"{Target.Method.Name}()";

        public virtual string ConsumeField => null;

        protected abstract Type IdleMethodReturnType { get; }

        public string IdleMethodReturnTypeName => IdleMethodReturnType.GetCorrectTypeName();

        public abstract string IdleImplementation { get; }

        public virtual bool UseRefKeyword => false;
    }

    internal class VoidDeclarationsProvider : DeclarationsProvider
    {
        public VoidDeclarationsProvider(Target target) : base(target) { }

        public override string TargetMethodReturnTypeNamespace => string.Empty;

        protected override Type IdleMethodReturnType => typeof(void);

        public override string IdleImplementation => string.Empty;
    }

    internal class NonVoidDeclarationsProvider : DeclarationsProvider
    {
        public NonVoidDeclarationsProvider(Target target) : base(target) { }

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
                    value = $"default({type.GetCorrectTypeName()})";
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
        public ByRefDeclarationsProvider(Target target) : base(target) { }

        protected override Type IdleMethodReturnType => typeof(IntPtr);

        public override string TargetMethodReturnTypeName => base.TargetMethodReturnTypeName.Replace("&", string.Empty);

        public override string ConsumeField => null;

        public override string IdleImplementation => $"return default({nameof(IntPtr)});";

        public override string ExtraDefines => "#define RETURNS_BYREF";

        public override bool UseRefKeyword => true;
    }

    internal class TaskDeclarationsProvider : VoidDeclarationsProvider
    {
        public TaskDeclarationsProvider(Target target) : base(target) { }

        // we use GetAwaiter().GetResult() because it's fastest way to obtain the result in blocking way, 
        // and will eventually throw actual exception, not aggregated one
        public override string TargetMethodDelegate
            => $"() => {{ {Target.Method.Name}().GetAwaiter().GetResult(); }}";

        public override string TargetMethodCall => $"{Target.Method.Name}().GetAwaiter().GetResult()";

        protected override Type TargetMethodReturnType => typeof(void);
    }

    /// <summary>
    /// declarations provider for <see cref="Task{TResult}" /> and <see cref="ValueTask{TResult}" />
    /// </summary>
    internal class GenericTaskDeclarationsProvider : NonVoidDeclarationsProvider
    {
        public GenericTaskDeclarationsProvider(Target target) : base(target) { }

        protected override Type TargetMethodReturnType => Target.Method.ReturnType.GetTypeInfo().GetGenericArguments().Single();

        // we use GetAwaiter().GetResult() because it's fastest way to obtain the result in blocking way, 
        // and will eventually throw actual exception, not aggregated one
        public override string TargetMethodDelegate
            => $"() => {{ return {Target.Method.Name}().GetAwaiter().GetResult(); }}";

        public override string TargetMethodCall => $"{Target.Method.Name}().GetAwaiter().GetResult()";
    }
}