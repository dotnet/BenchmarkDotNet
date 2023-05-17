﻿using BenchmarkDotNet.Engines;
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
            }
            else if (WorkloadMethodReturnType.IsByRef)
            {
                IsByRef = true;
            }
            else if (Consumer.IsConsumable(WorkloadMethodReturnType)
                || Consumer.HasConsumableField(WorkloadMethodReturnType, out consumableField))
            {
                IsConsumable = true;
                WorkloadConsumableField = consumableField;
            }
        }

        public Type OriginMethodReturnType { get; }
        public Type WorkloadMethodReturnType { get; }

        public MethodInfo? GetAwaiterMethod { get; }
        public MethodInfo? GetResultMethod { get; }

        public bool IsVoid { get; }
        public bool IsByRef { get; }
        public bool IsConsumable { get; }
        public FieldInfo? WorkloadConsumableField { get; }

        public bool IsAwaitable => GetAwaiterMethod != null && GetResultMethod != null;
    }
}