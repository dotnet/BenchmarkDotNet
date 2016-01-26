using System;
using System.Reflection;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Extensions;

namespace BenchmarkDotNet.Running
{
    public class Target
    {
        public Type Type { get; }
        public MethodInfo Method { get; }
        public MethodInfo SetupMethod { get; }
        public string AdditionalLogic { get; }
        public long OperationsPerInvoke { get; }
        public string MethodTitle { get; }
        public bool Baseline { get; }

        public string FullInfo => (Type?.Name.WithoutSuffix("Competition") ?? "Untitled") + "_" + (Method?.Name ?? "Untitled");

        public Target(Type type, MethodInfo method, MethodInfo setupMethod = null,
                               string description = null, string additionalLogic = null, bool baseline = false)
        {
            Type = type;
            Method = method;
            SetupMethod = setupMethod;
            OperationsPerInvoke = method.ResolveAttribute<OperationsPerInvokeAttribute>()?.Count ?? 1;
            AdditionalLogic = additionalLogic ?? string.Empty;
            MethodTitle = description ?? method?.Name ?? "Untitled";
            Baseline = baseline;
        }

        public override string ToString() => FullInfo;
    }
}