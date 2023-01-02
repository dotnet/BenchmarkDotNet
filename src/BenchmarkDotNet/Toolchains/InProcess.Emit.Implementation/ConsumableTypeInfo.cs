using BenchmarkDotNet.Engines;
using JetBrains.Annotations;
using System;
using System.Collections.Generic;
using System.Linq;
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

            WorkloadMethodReturnType = methodReturnType;

            // Only support (Value)Task for parity with other toolchains (and so we can use AwaitHelper).
            IsAwaitable = methodReturnType == typeof(Task) || methodReturnType == typeof(ValueTask)
                || (methodReturnType.GetTypeInfo().IsGenericType
                    && (methodReturnType.GetTypeInfo().GetGenericTypeDefinition() == typeof(Task<>)
                    || methodReturnType.GetTypeInfo().GetGenericTypeDefinition() == typeof(ValueTask<>)));

            if (WorkloadMethodReturnType == null)
                throw new InvalidOperationException("Bug: (WorkloadMethodReturnType == null");

            var consumableField = default(FieldInfo);
            if (IsAwaitable)
            {
                OverheadMethodReturnType = WorkloadMethodReturnType;
            }
            else if (WorkloadMethodReturnType == typeof(void))
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

        public Type OriginMethodReturnType { get; }
        public Type WorkloadMethodReturnType { get; }
        public Type OverheadMethodReturnType { get; }

        public bool IsVoid { get; }
        public bool IsByRef { get; }
        public bool IsConsumable { get; }
        public FieldInfo? WorkloadConsumableField { get; }

        public bool IsAwaitable { get; }
    }
}