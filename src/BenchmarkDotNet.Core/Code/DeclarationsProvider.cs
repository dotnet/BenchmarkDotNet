using System;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using BenchmarkDotNet.Extensions;
using BenchmarkDotNet.Running;

namespace BenchmarkDotNet.Code
{
    internal abstract class DeclarationsProvider
    {
        // "Setup" or "Cleanup" methods are optional, so default to an empty delegate, so there is always something that can be invoked
        private const string EmptyAction = "() => { }";

        protected readonly Target Target;

        internal DeclarationsProvider(Target target)
        {
            Target = target;
        }

        public string OperationsPerInvoke => Target.OperationsPerInvoke.ToString();

        public string TargetTypeNamespace => string.IsNullOrWhiteSpace(Target.Type.Namespace) ? string.Empty : $"using {Target.Type.Namespace};";

        public string TargetTypeName => Target.Type.GetCorrectTypeName();

        public string SetupMethodName => Target.SetupMethod?.Name ?? EmptyAction;

        public string CleanupMethodName => Target.CleanupMethod?.Name ?? EmptyAction;

        public virtual string TargetMethodDelegate => Target.Method.Name;

        public abstract string TargetMethodReturnTypeNamespace { get; }

        public abstract string TargetMethodReturnType { get; }

        public abstract string TargetMethodResultHolder { get; }

        public abstract string TargetMethodHoldValue { get; }

        public abstract string TargetMethodDelegateType { get; }

        public abstract string IdleMethodReturnType { get; }

        public abstract string IdleMethodDelegateType { get; }

        public abstract string IdleImplementation { get; }
    }

    internal class VoidDeclarationsProvider : DeclarationsProvider
    {
        public VoidDeclarationsProvider(Target target) : base(target) { }

        public override string TargetMethodReturnTypeNamespace => string.Empty;

        public override string TargetMethodReturnType => "void";

        public override string TargetMethodResultHolder => string.Empty;

        public override string TargetMethodHoldValue => string.Empty;

        public override string TargetMethodDelegateType => "Action";

        public override string IdleMethodReturnType => TargetMethodReturnType;

        public override string IdleMethodDelegateType => TargetMethodDelegateType;

        public override string IdleImplementation => string.Empty;
    }

    internal class NonVoidDeclarationsProvider : DeclarationsProvider
    {
        public NonVoidDeclarationsProvider(Target target) : base(target) { }

        public override string TargetMethodReturnTypeNamespace 
            => Target.Method.ReturnType.Namespace == "System" // As "using System;" is always included in the template, don't emit it again
                || string.IsNullOrWhiteSpace(Target.Method.ReturnType.Namespace) 
                    ? string.Empty 
                    : $"using {Target.Method.ReturnType.Namespace};";

        public override string TargetMethodReturnType => Target.Method.ReturnType.GetCorrectTypeName();

        public override string TargetMethodResultHolder => $"private {TargetMethodReturnType} value;";

        public override string TargetMethodHoldValue => "value = ";

        public override string TargetMethodDelegateType => $"Func<{TargetMethodReturnType}>";

        public override string IdleMethodReturnType
            => Target.Method.ReturnType.GetTypeInfo().IsValueType
                ? "int" // we return int because creating bigger ValueType could take longer than benchmarked method itself
                : TargetMethodReturnType;

        public override string IdleMethodDelegateType
            => Target.Method.ReturnType.GetTypeInfo().IsValueType
                ? "Func<int>" 
                : $"Func<{TargetMethodReturnType}>";

        public override string IdleImplementation
            => Target.Method.ReturnType.GetTypeInfo().IsValueType
                ? "return 0;"
                : "return null;";
    }

    internal class TaskDeclarationsProvider : VoidDeclarationsProvider
    {
        public TaskDeclarationsProvider(Target target) : base(target) { }

        public override string TargetMethodDelegate
            => $"() => {{ BenchmarkDotNet.Running.TaskMethodInvoker.ExecuteBlocking({Target.Method.Name}); }}";        

        public override string IdleImplementation
            => $"BenchmarkDotNet.Running.TaskMethodInvoker.Idle();";
    }

    /// <summary>
    /// declarations provider for <see cref="Task{TResult}" /> and <see cref="ValueTask{T}" />
    /// </summary>
    internal class GenericTaskDeclarationsProvider : NonVoidDeclarationsProvider
    {
        private const char GenericArgumentSign = '`';

        private readonly string invokerFullName;

        public GenericTaskDeclarationsProvider(Target target, Type invoker) : base(target)
        {
            invokerFullName = invoker.GetTypeInfo().FullName.Split(GenericArgumentSign).First();
        }

        public override string TargetMethodReturnType 
            => Target.Method.ReturnType.GetTypeInfo().GetGenericArguments().Single().GetCorrectTypeName();

        public override string TargetMethodDelegate
            => $"() => {{ return {invokerFullName}<{TargetMethodReturnType}>.ExecuteBlocking({Target.Method.Name}); }}";

        public override string IdleMethodReturnType => TargetMethodReturnType;

        public override string IdleMethodDelegateType => TargetMethodDelegateType;

        public override string IdleImplementation 
            => $"return {invokerFullName}<{TargetMethodReturnType}>.Idle();";
    }
}