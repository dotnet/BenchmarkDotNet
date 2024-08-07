namespace BenchmarkDotNet.TestAdapter.Annotations
{
    /// <summary>
    /// Comparison Operators
    /// </summary>
    public enum ComparisonOperator
    {
        /// <summary>
        /// Equal comparison operator
        /// </summary>
        Equal,
        /// <summary>
        /// Not Equal comparison operator
        /// </summary>
        NotEqual,
        /// <summary>
        ///  comparison operator
        /// </summary>
        Greater,
        /// <summary>
        ///  Greater than comparison operator
        /// </summary>
        GreaterOrEqual,
        /// <summary>
        ///  Greater or Equal than comparison operator
        /// </summary>
        Less,
        /// <summary>
        /// Less than comparison operator
        /// </summary>
        LessOrEqual,
        /// <summary>
        /// Less or equal than comparison operator
        /// </summary>
        Between
    }
}
