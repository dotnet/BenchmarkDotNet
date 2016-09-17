using System;
using System.Linq;
using System.Reflection;
using JetBrains.Annotations;

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
        public string MethodDisplayInfo { get; }
        public int MethodIndex { get; }
        public bool Baseline { get; }

        private string TypeInfo => Type?.Name ?? "Untitled";
        private string MethodFolderInfo => Method?.Name ?? "Untitled";

        public string FolderInfo => TypeInfo + "_" + MethodFolderInfo;
        public string DisplayInfo => TypeInfo + "." + MethodDisplayInfo;

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
            MethodDisplayInfo = FormatDescription(description) ?? method?.Name ?? "Untitled";
            Baseline = baseline;
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
    }
}