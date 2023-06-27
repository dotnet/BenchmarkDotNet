using System;
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
        protected readonly Descriptor Descriptor;

        internal DeclarationsProvider(Descriptor descriptor) => Descriptor = descriptor;

        public string OperationsPerInvoke => Descriptor.OperationsPerInvoke.ToString();

        public string WorkloadTypeName => Descriptor.Type.GetCorrectCSharpTypeName();

        public string GlobalSetupMethodName => GetMethodName(Descriptor.GlobalSetupMethod);

        public string GlobalCleanupMethodName => GetMethodName(Descriptor.GlobalCleanupMethod);

        public string IterationSetupMethodName => GetMethodName(Descriptor.IterationSetupMethod);

        public string IterationCleanupMethodName => GetMethodName(Descriptor.IterationCleanupMethod);

        public abstract string ReturnsDefinition { get; }

        protected virtual Type WorkloadMethodReturnType => Descriptor.WorkloadMethod.ReturnType;

        public virtual string WorkloadMethodReturnTypeName => WorkloadMethodReturnType.GetCorrectCSharpTypeName();

        public virtual string WorkloadMethodDelegate(string passArguments) => Descriptor.WorkloadMethod.Name;

        public virtual string WorkloadMethodReturnTypeModifiers => null;

        public virtual string GetWorkloadMethodCall(string passArguments) => $"{Descriptor.WorkloadMethod.Name}({passArguments})";

        public virtual string ConsumeField => null;

        protected abstract Type OverheadMethodReturnType { get; }

        public string OverheadMethodReturnTypeName => OverheadMethodReturnType.GetCorrectCSharpTypeName();

        public virtual string AsyncBenchmarkRunnerTypeName => null;

        public virtual void OverrideUnrollFactor(BenchmarkCase benchmarkCase) { }

        public abstract string OverheadImplementation { get; }

        private string GetMethodName(MethodInfo method)
        {
            // "Setup" or "Cleanup" methods are optional, so default to a simple delegate, so there is always something that can be invoked
            if (method == null)
            {
                return "() => new System.Threading.Tasks.ValueTask()";
            }

            if (method.ReturnType == typeof(Task) ||
                method.ReturnType == typeof(ValueTask) ||
                (method.ReturnType.IsGenericType &&
                    (method.ReturnType.GetGenericTypeDefinition() == typeof(Task<>) ||
                     method.ReturnType.GetGenericTypeDefinition() == typeof(ValueTask<>))))
            {
                return $"() => BenchmarkDotNet.Helpers.AwaitHelper.ToValueTaskVoid({method.Name}())";
            }

            return $"() => {{ {method.Name}(); return new System.Threading.Tasks.ValueTask(); }}";
        }
    }

    internal class VoidDeclarationsProvider : DeclarationsProvider
    {
        public VoidDeclarationsProvider(Descriptor descriptor) : base(descriptor) { }

        public override string ReturnsDefinition => "RETURNS_VOID";

        protected override Type OverheadMethodReturnType => typeof(void);

        public override string OverheadImplementation => string.Empty;
    }

    internal class NonVoidDeclarationsProvider : DeclarationsProvider
    {
        public NonVoidDeclarationsProvider(Descriptor descriptor) : base(descriptor) { }

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

        public override string ReturnsDefinition
            => Consumer.IsConsumable(WorkloadMethodReturnType) || Consumer.HasConsumableField(WorkloadMethodReturnType, out _)
                ? "RETURNS_CONSUMABLE"
                : "RETURNS_NON_CONSUMABLE_STRUCT";
    }

    internal class ByRefDeclarationsProvider : NonVoidDeclarationsProvider
    {
        public ByRefDeclarationsProvider(Descriptor descriptor) : base(descriptor) { }

        protected override Type OverheadMethodReturnType => typeof(IntPtr);

        public override string WorkloadMethodReturnTypeName => base.WorkloadMethodReturnTypeName.Replace("&", string.Empty);

        public override string ConsumeField => null;

        public override string OverheadImplementation => $"return default(System.{nameof(IntPtr)});";

        public override string ReturnsDefinition => "RETURNS_BYREF";

        public override string WorkloadMethodReturnTypeModifiers => "ref";
    }

    internal class ByReadOnlyRefDeclarationsProvider : ByRefDeclarationsProvider
    {
        public ByReadOnlyRefDeclarationsProvider(Descriptor descriptor) : base(descriptor) { }

        public override string ReturnsDefinition => "RETURNS_BYREF_READONLY";

        public override string WorkloadMethodReturnTypeModifiers => "ref readonly";
    }

    internal class AsyncDeclarationsProvider : DeclarationsProvider
    {
        private readonly Type asyncConsumerType;
        public AsyncDeclarationsProvider(Descriptor descriptor, Type asyncConsumerType) : base(descriptor) => this.asyncConsumerType = asyncConsumerType;

        public override string ReturnsDefinition => "RETURNS_AWAITABLE";

        public override string OverheadImplementation => $"return default({OverheadMethodReturnType.GetCorrectCSharpTypeName()});";

        protected override Type OverheadMethodReturnType => WorkloadMethodReturnType;

        public override string AsyncBenchmarkRunnerTypeName
        {
            get
            {
                string consumerTypeName = asyncConsumerType.GetCorrectCSharpTypeName();
                string awaiterTypeName = asyncConsumerType.GetInterfaces()
                    .First(t => t.IsGenericType && t.GetGenericTypeDefinition() == typeof(IAsyncConsumer<,>))
                    .GetGenericArguments()[1].GetCorrectCSharpTypeName();
                return $"BenchmarkDotNet.Engines.AsyncBenchmarkRunner<BenchmarkFunc, {consumerTypeName}, {WorkloadMethodReturnTypeName}, {awaiterTypeName}>";
            }
        }

        public override void OverrideUnrollFactor(BenchmarkCase benchmarkCase) => benchmarkCase.ForceUnrollFactorForAsync();
    }
}