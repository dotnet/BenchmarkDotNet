using BenchmarkDotNet.Engines;
using JetBrains.Annotations;
using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using static BenchmarkDotNet.Toolchains.InProcess.Emit.Implementation.RunnableReflectionHelpers;

namespace BenchmarkDotNet.Toolchains.InProcess.Emit.Implementation
{
    public class ConsumableTypeInfo
    {
        public ConsumableTypeInfo(Type methodReturnType)
        {
            if (methodReturnType == null)
                throw new ArgumentNullException(nameof(methodReturnType));

            OriginMethodReturnType = methodReturnType;

            // Please note this code does not support await over extension methods.
            var getAwaiterMethod = methodReturnType.GetMethod(nameof(Task<int>.GetAwaiter), BindingFlagsPublicInstance);
            if (getAwaiterMethod == null)
            {
                WorkloadMethodReturnType = methodReturnType;
            }
            else
            {
                var getResultMethod = getAwaiterMethod
                    .ReturnType
                    .GetMethod(nameof(TaskAwaiter.GetResult), BindingFlagsPublicInstance);

                if (getResultMethod == null)
                {
                    WorkloadMethodReturnType = methodReturnType;
                }
                else
                {
                    WorkloadMethodReturnType = getResultMethod.ReturnType;
                    GetAwaiterMethod = getAwaiterMethod;
                    GetResultMethod = getResultMethod;
                }
            }

            if (WorkloadMethodReturnType == null)
                throw new InvalidOperationException("Bug: (WorkloadMethodReturnType == null");

            var consumableField = default(FieldInfo);
            if (WorkloadMethodReturnType == typeof(void))
            {
                IsVoid = true;
                OverheadMethodReturnType = WorkloadMethodReturnType;
            }
            else if (WorkloadMethodReturnType.IsByRef)
            {
                IsByRef = true;
                OverheadMethodReturnType = typeof(IntPtr);
            }
            else if (Consumer.IsConsumable(WorkloadMethodReturnType)
                || Consumer.HasConsumableField(WorkloadMethodReturnType, out consumableField))
            {
                IsConsumable = true;
                WorkloadConsumableField = consumableField;
                OverheadMethodReturnType = consumableField?.FieldType ?? WorkloadMethodReturnType;
            }
            else
            {
                OverheadMethodReturnType = typeof(int); // we return this simple type because creating bigger ValueType could take longer than benchmarked method itself
            }

            if (OverheadMethodReturnType == null)
                throw new InvalidOperationException("Bug: (OverheadResultType == null");
        }

        [NotNull]
        public Type OriginMethodReturnType { get; }
        [NotNull]
        public Type WorkloadMethodReturnType { get; }
        [NotNull]
        public Type OverheadMethodReturnType { get; }

        [CanBeNull]
        public MethodInfo GetAwaiterMethod { get; }
        [CanBeNull]
        public MethodInfo GetResultMethod { get; }

        public bool IsVoid { get; }
        public bool IsByRef { get; }
        public bool IsConsumable { get; }
        [CanBeNull]
        public FieldInfo WorkloadConsumableField { get; }

        public bool IsAwaitable => GetAwaiterMethod != null && GetResultMethod != null;
    }
}