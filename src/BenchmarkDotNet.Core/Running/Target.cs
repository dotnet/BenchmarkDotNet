using System;
using System.Linq;
using System.Reflection;
using BenchmarkDotNet.Portability;
using JetBrains.Annotations;

namespace BenchmarkDotNet.Running
{
    public class Target
    {
        public Type Type { get; }
        public MethodInfo Method { get; }
        public MethodInfo GlobalSetupMethod { get; }
        public MethodInfo GlobalCleanupMethod { get; }
        public MethodInfo IterationSetupMethod { get; }
        public MethodInfo IterationCleanupMethod { get; }
        public string AdditionalLogic { get; }
        public int OperationsPerInvoke { get; }
        public string MethodDisplayInfo { get; }
        public int MethodIndex { get; }
        public bool Baseline { get; }
        public string[] Categories { get; }

        private string TypeInfo => Type?.Name ?? "Untitled";
        private string MethodFolderInfo => Method?.Name ?? "Untitled";

        public string FolderInfo => TypeInfo + "_" + MethodFolderInfo;
        public string DisplayInfo => TypeInfo + "." + MethodDisplayInfo;

        public Target(
            Type type,
            MethodInfo method,              
            MethodInfo globalSetupMethod = null,
            MethodInfo globalCleanupMethod = null,
            MethodInfo iterationSetupMethod = null,
            MethodInfo iterationCleanupMethod = null,
            string description = null,
            string additionalLogic = null,
            bool baseline = false,
            string[] categories = null,
            int operationsPerInvoke = 1,
            int methodIndex = 0)
        {
            Type = type;
            Method = method;
            GlobalSetupMethod = globalSetupMethod;
            GlobalCleanupMethod = globalCleanupMethod;
            IterationSetupMethod = iterationSetupMethod;
            IterationCleanupMethod = iterationCleanupMethod;
            OperationsPerInvoke = operationsPerInvoke;
            AdditionalLogic = additionalLogic ?? string.Empty;
            MethodDisplayInfo = FormatDescription(description) ?? method?.Name ?? "Untitled";
            Baseline = baseline;
            Categories = categories ?? Array.Empty<string>();
            MethodIndex = methodIndex;
        }

        public override string ToString() => DisplayInfo;

        private static string FormatDescription([CanBeNull] string description)
        {
            var specialSymbols = new[] { ' ', '\'', '[', ']' };
            return description != null && specialSymbols.Any(description.Contains)
                ? "'" + description + "'"
                : description;
        }

        public bool HasCategory(string category) => Categories.Any(c => c.EqualsWithIgnoreCase(category));
    }
}