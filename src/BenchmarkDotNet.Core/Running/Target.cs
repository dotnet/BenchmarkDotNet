using System;
using System.Reflection;
using BenchmarkDotNet.Extensions;

namespace BenchmarkDotNet.Running
{
    public class Target
    {
        public Type Type { get; }
        public MethodInfo Method { get; }
        public MethodInfo SetupMethod { get; }
        public MethodInfo CleanupMethod { get; }
        public string AdditionalLogic { get; }
        public int OperationsPerInvoke { get; }
        public string MethodTitle { get; }
        public int MethodIndex { get; }
        public bool Baseline { get; }

        public string FullInfo => (Type?.Name.WithoutSuffix("Competition") ?? "Untitled") + "_" + (Method?.Name ?? "Untitled");

        public Target(
            Type type,
            MethodInfo method,
            MethodInfo setupMethod = null,
            MethodInfo cleanupMethod = null,
            string description = null,
            string additionalLogic = null,
            bool baseline = false,
            int operationsPerInvoke = 1,
            int methodIndex = 0)
        {
            Type = type;
            Method = method;
            SetupMethod = setupMethod;
            CleanupMethod = cleanupMethod;
            OperationsPerInvoke = operationsPerInvoke;
            AdditionalLogic = additionalLogic ?? string.Empty;
            MethodTitle = description ?? method?.Name ?? "Untitled";
            Baseline = baseline;
            MethodIndex = methodIndex;
        }

        public override string ToString() => FullInfo;
    }
}