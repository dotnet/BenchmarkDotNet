using System;
using System.Collections.Generic;
using System.Reflection;
using BenchmarkDotNet.Extensions;

namespace BenchmarkDotNet.Tasks
{
    public class BenchmarkTarget
    {
        public Type Type { get; }
        public MethodInfo Method { get; }
        public MethodInfo SetupMethod { get; }
        public string AdditionalLogic { get; }
        public long OperationsPerInvoke { get; }
        public string Description { get; }

        public string Caption => Type.Name.WithoutSuffix("Competition") + "_" + Method.Name;

        public BenchmarkTarget(Type type, MethodInfo method, MethodInfo setupMethod = null, string description = null, string additionalLogic = null)
        {
            Type = type;
            Method = method;
            SetupMethod = setupMethod;
            OperationsPerInvoke = method.ResolveAttribute<OperationsPerInvokeAttribute>()?.Count ?? 1;
            AdditionalLogic = additionalLogic ?? string.Empty;
            Description = description ?? Caption;
        }

        public IEnumerable<BenchmarkProperty> Properties
        {
            get
            {
                yield return new BenchmarkProperty(nameof(Type), Type.Name);
                yield return new BenchmarkProperty(nameof(Method), Description);
            }
        }
    }
}