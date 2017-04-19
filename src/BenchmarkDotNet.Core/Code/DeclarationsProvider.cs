using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using BenchmarkDotNet.Core.Helpers;
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

        public abstract string TargetMethodDelegateType { get; }

        public abstract string IdleMethodReturnType { get; }

        public abstract string IdleMethodDelegateType { get; }

        public abstract string IdleImplementation { get; }

        public abstract string HasReturnValue { get; }
    }

    internal class VoidDeclarationsProvider : DeclarationsProvider
    {
        public VoidDeclarationsProvider(Target target) : base(target) { }

        public override string TargetMethodReturnTypeNamespace => string.Empty;

        public override string TargetMethodDelegateType => "Action";

        public override string IdleMethodReturnType => "void";

        public override string IdleMethodDelegateType => TargetMethodDelegateType;

        public override string IdleImplementation => string.Empty;

        public override string HasReturnValue => "false";
    }

    internal class NonVoidDeclarationsProvider : DeclarationsProvider
    {
        public NonVoidDeclarationsProvider(Target target) : base(target) { }

        public override string TargetMethodReturnTypeNamespace 
            => TargetMethodReturnType.Namespace == "System" // As "using System;" is always included in the template, don't emit it again
                || string.IsNullOrWhiteSpace(TargetMethodReturnType.Namespace) 
                    ? string.Empty 
                    : $"using {TargetMethodReturnType.Namespace};";

        public virtual Type TargetMethodReturnType => Target.Method.ReturnType;

        public string TargetMethodReturnTypeName => TargetMethodReturnType.GetCorrectTypeName();

        public override string TargetMethodDelegateType => $"Func<{TargetMethodReturnTypeName}>";

        public override string IdleMethodReturnType
            => TargetMethodReturnType.IsStruct()
                ? "int" // we return int because creating bigger ValueType could take longer than benchmarked method itself
                : TargetMethodReturnTypeName;

        public override string IdleMethodDelegateType
            => TargetMethodReturnType.IsStruct()
                ? "Func<int>" 
                : $"Func<{TargetMethodReturnTypeName}>";

        public override string IdleImplementation
        {
            get
            {
                string value;
                var type = TargetMethodReturnType;
                if (type.IsStruct())
                    value = "0";
                else if (type.GetTypeInfo().IsClass || type.GetTypeInfo().IsInterface)
                    value = "null";
                else
                    value = SourceCodeHelper.ToSourceCode(Activator.CreateInstance(type)) + ";";                                    
                return $"return {value};";
            }
        }

        public override string HasReturnValue => "true";
    }

    internal class TaskDeclarationsProvider : VoidDeclarationsProvider
    {
        public TaskDeclarationsProvider(Target target) : base(target) { }

        // we use GetAwaiter().GetResult() because it's fastest way to obtain the result in blocking way, 
        // and will eventually throw actual exception, not aggregated one
        public override string TargetMethodDelegate
            => $"() => {{ {Target.Method.Name}().GetAwaiter().GetResult(); }}";        
    }

    /// <summary>
    /// declarations provider for <see cref="Task{TResult}" /> and <see cref="ValueTask{TResult}" />
    /// </summary>
    internal class GenericTaskDeclarationsProvider : NonVoidDeclarationsProvider
    {
        public GenericTaskDeclarationsProvider(Target target) : base(target)
        {
        }

        public override Type TargetMethodReturnType => Target.Method.ReturnType.GetTypeInfo().GetGenericArguments().Single();

        // we use GetAwaiter().GetResult() because it's fastest way to obtain the result in blocking way, 
        // and will eventually throw actual exception, not aggregated one
        public override string TargetMethodDelegate
            => $"() => {{ return {Target.Method.Name}().GetAwaiter().GetResult(); }}";
    }
}