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

            // Only support (Value)Task for parity with other toolchains (and so we can use AwaitHelper).
            IsAwaitable = methodReturnType == typeof(Task) || methodReturnType == typeof(ValueTask)
                || (methodReturnType.GetTypeInfo().IsGenericType
                    && (methodReturnType.GetTypeInfo().GetGenericTypeDefinition() == typeof(Task<>)
                    || methodReturnType.GetTypeInfo().GetGenericTypeDefinition() == typeof(ValueTask<>)));

            if (!IsAwaitable)
            {
                WorkloadMethodReturnType = methodReturnType;
            }
            else
            {
                WorkloadMethodReturnType = methodReturnType
                    .GetMethod(nameof(Task.GetAwaiter), BindingFlagsPublicInstance)!
                    .ReturnType
                    .GetMethod(nameof(TaskAwaiter.GetResult), BindingFlagsPublicInstance)!
                    .ReturnType;
                GetResultMethod = Helpers.AwaitHelper.GetGetResultMethod(methodReturnType);
            }

            if (WorkloadMethodReturnType == null)
                throw new InvalidOperationException("Bug: (WorkloadMethodReturnType == null");

            if (WorkloadMethodReturnType == typeof(void))
            {
                IsVoid = true;
            }
            else if (WorkloadMethodReturnType.IsByRef)
            {
                IsByRef = true;
            }
        }

        public Type OriginMethodReturnType { get; }
        public Type WorkloadMethodReturnType { get; }

        public MethodInfo? GetResultMethod { get; }

        public bool IsVoid { get; }
        public bool IsByRef { get; }

        public bool IsAwaitable { get; }
    }
}