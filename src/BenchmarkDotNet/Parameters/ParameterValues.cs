namespace BenchmarkDotNet.Parameters
{
    /// <summary>
    /// A parameter and the values it takes; the benchmark cases are the cartesian product of these.
    /// </summary>
    /// <remarks>
    /// Only a [Params], [ParamsSource] or [ParamsAllValues] member ranges over values this way. An argument is
    /// filled from a row spanning the whole parameter list, so its values belong to the row and there is nothing
    /// to pair with the definition.
    /// </remarks>
    public sealed class ParameterValues(ParameterDefinition definition, IReadOnlyList<ParameterValue> items)
    {
        public ParameterDefinition Definition { get; } = definition;

        public IReadOnlyList<ParameterValue> Items { get; } = items;
    }
}
