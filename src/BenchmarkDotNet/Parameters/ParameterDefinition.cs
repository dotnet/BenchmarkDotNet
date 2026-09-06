namespace BenchmarkDotNet.Parameters
{
    /// <summary>
    /// What a benchmark parameter is, independently of any value it takes: a member the runnable assigns, or a
    /// parameter the benchmark method is called with.
    /// </summary>
    public sealed class ParameterDefinition(string name, bool isStatic, bool isArgument, Type parameterType, int priorityInCategory)
    {
        public string Name { get; } = name;

        /// <summary>Whether the member holding the value is static. Always false for an argument, which is not a member.</summary>
        public bool IsStatic { get; } = isStatic;

        /// <summary>Whether the benchmark method is called with this parameter, rather than assigned it.</summary>
        public bool IsArgument { get; } = isArgument;

        public Type ParameterType { get; } = parameterType;

        public int PriorityInCategory { get; } = priorityInCategory;
    }
}
