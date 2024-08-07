using BenchmarkDotNet.Reports;

namespace BenchmarkDotNet.TestAdapter.Annotations
{
    /// <summary>
    /// Base <see cref="System.Attribute"/> for TestAdapter <see cref="Running.BenchmarkCase"/> constraints
    /// </summary>
    [System.AttributeUsage(System.AttributeTargets.Method, Inherited = false, AllowMultiple = true)]
    public abstract class BenchmarkCaseConstraintAttribute : System.Attribute
    {
        /// <summary>
        /// Validate the <see cref="Running.BenchmarkCase"/> constraint
        /// </summary>
        /// <param name="report"></param>
        /// <param name="builder"></param>
        protected internal abstract void Validate(BenchmarkReport report, System.Text.StringBuilder builder);
    }
}
