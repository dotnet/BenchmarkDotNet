using System;
using System.Linq;
using System.Reflection;
using BenchmarkDotNet.Extensions;
using BenchmarkDotNet.Helpers;
using BenchmarkDotNet.Portability;
using JetBrains.Annotations;

namespace BenchmarkDotNet.Running
{
    public class Descriptor : IEquatable<Descriptor>
    {
        public Type Type { get; }
        public MethodInfo WorkloadMethod { get; }
        public MethodInfo GlobalSetupMethod { get; }
        public MethodInfo GlobalCleanupMethod { get; }
        public MethodInfo IterationSetupMethod { get; }
        public MethodInfo IterationCleanupMethod { get; }
        public string AdditionalLogic { get; }
        public int OperationsPerInvoke { get; }
        public string WorkloadMethodDisplayInfo { get; }
        public int MethodIndex { get; }
        public bool Baseline { get; }
        public string[] Categories { get; }

        internal string TypeInfo => Type?.GetDisplayName() ?? "Untitled";
        private string MethodFolderInfo => WorkloadMethod?.Name ?? "Untitled";

        public string FolderInfo => (Type != null ? FolderNameHelper.ToFolderName(Type) : "Untitled") + "_" + MethodFolderInfo;
        public string DisplayInfo => TypeInfo + "." + WorkloadMethodDisplayInfo;

        public Descriptor(
            Type type,
            MethodInfo workloadMethod,
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
            WorkloadMethod = workloadMethod;
            GlobalSetupMethod = globalSetupMethod;
            GlobalCleanupMethod = globalCleanupMethod;
            IterationSetupMethod = iterationSetupMethod;
            IterationCleanupMethod = iterationCleanupMethod;
            OperationsPerInvoke = operationsPerInvoke;
            AdditionalLogic = additionalLogic ?? string.Empty;
            WorkloadMethodDisplayInfo = FormatDescription(description) ?? workloadMethod?.Name ?? "Untitled";
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

        public string GetFilterName() => $"{Type.GetCorrectCSharpTypeName(includeGenericArgumentsNamespace: false)}.{WorkloadMethod.Name}";

        public bool Equals(Descriptor other) => GetFilterName().Equals(other.GetFilterName());

        public override bool Equals(object obj) => obj is Descriptor && Equals((Descriptor)obj);

        public override int GetHashCode() => GetFilterName().GetHashCode();
    }
}