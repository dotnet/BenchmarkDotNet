using BenchmarkDotNet.Extensions;
using BenchmarkDotNet.Helpers;
using BenchmarkDotNet.Portability;
using System.Reflection;

namespace BenchmarkDotNet.Running
{
    public class Descriptor : IEquatable<Descriptor>
    {
        public Type Type { get; }
        public MethodInfo WorkloadMethod { get; }
        public MethodInfo? GlobalSetupMethod { get; }
        public MethodInfo? GlobalCleanupMethod { get; }
        public MethodInfo? IterationSetupMethod { get; }
        public MethodInfo? IterationCleanupMethod { get; }
        public int OperationsPerInvoke { get; }
        public string WorkloadMethodDisplayInfo { get; }
        public int MethodIndex { get; }
        public bool Baseline { get; }
        public string[] Categories { get; }

        internal string TypeInfo => Type.GetDisplayName();
        private string MethodFolderInfo => WorkloadMethod.Name;

        public string FolderInfo => $"{FolderNameHelper.ToFolderName(Type)}_{MethodFolderInfo}";
        public string DisplayInfo => TypeInfo + "." + WorkloadMethodDisplayInfo;

        public Descriptor(
            Type type,
            MethodInfo workloadMethod,
            MethodInfo? globalSetupMethod = null,
            MethodInfo? globalCleanupMethod = null,
            MethodInfo? iterationSetupMethod = null,
            MethodInfo? iterationCleanupMethod = null,
            string? description = null,
            bool baseline = false,
            string[]? categories = null,
            int operationsPerInvoke = 1,
            int methodIndex = 0)
        {
            ArgumentNullException.ThrowIfNull(type);
            ArgumentNullException.ThrowIfNull(workloadMethod);

            EnsureDeclaredBy(type, workloadMethod, nameof(workloadMethod));
            EnsureDeclaredBy(type, globalSetupMethod, nameof(globalSetupMethod));
            EnsureDeclaredBy(type, globalCleanupMethod, nameof(globalCleanupMethod));
            EnsureDeclaredBy(type, iterationSetupMethod, nameof(iterationSetupMethod));
            EnsureDeclaredBy(type, iterationCleanupMethod, nameof(iterationCleanupMethod));

            Type = type;
            WorkloadMethod = workloadMethod;
            GlobalSetupMethod = globalSetupMethod;
            GlobalCleanupMethod = globalCleanupMethod;
            IterationSetupMethod = iterationSetupMethod;
            IterationCleanupMethod = iterationCleanupMethod;
            OperationsPerInvoke = operationsPerInvoke;
            WorkloadMethodDisplayInfo = FormatDescription(description) ?? workloadMethod?.Name ?? "Untitled";
            Baseline = baseline;
            Categories = categories ?? [];
            MethodIndex = methodIndex;
        }

        /// <summary>
        /// Every method named here is one BenchmarkDotNet calls on an instance of <paramref name="type"/>, so the
        /// type has to declare it or inherit it - an inherited one is declared by a base, which is why this asks
        /// what the type can be assigned to rather than what it declares itself. A method no type declares, emitted
        /// at run time, names nothing to check against and is left to whatever means to call it.
        /// </summary>
        private static void EnsureDeclaredBy(Type type, MethodInfo? method, string paramName)
        {
            if (method?.DeclaringType is not { } declaringType || declaringType.IsAssignableFrom(type))
                return;

            throw new ArgumentException(
                $"{declaringType.GetDisplayName()}.{method.Name} is not declared by {type.GetDisplayName()} or anything it inherits from, so it cannot be called on one.",
                paramName);
        }

        public override string ToString() => DisplayInfo;

        private static string? FormatDescription(string? description)
        {
            char[] specialSymbols = [' ', '\'', '[', ']'];
            return description != null && specialSymbols.Any(description.Contains)
                ? "'" + description + "'"
                : description;
        }

        public bool HasCategory(string category) => Categories.Any(c => c.EqualsWithIgnoreCase(category));

        public string GetFilterName() => $"{Type.GetCorrectCSharpTypeName(includeGenericArgumentsNamespace: false, prefixWithGlobal: false)}.{WorkloadMethod.Name}";

        public bool Equals(Descriptor? other) => GetFilterName().Equals(other?.GetFilterName());

        public override bool Equals(object? obj) => obj is Descriptor descriptor && Equals(descriptor);

        public override int GetHashCode() => GetFilterName().GetHashCode();
    }
}