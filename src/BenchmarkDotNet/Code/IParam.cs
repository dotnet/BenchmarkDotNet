namespace BenchmarkDotNet.Code
{
    public interface IParam
    {
        /// <summary>
        /// value of the parameter object for benchmark
        /// used internally (e.g. by the InProcessToolchain)
        /// </summary>
        object Value { get; }

        /// <summary>
        /// used to display the value (e.g. in summary in Params column)
        /// </summary>
        string DisplayText { get; }

        /// <summary>
        /// this source code is used to create parameter for benchmark
        /// in C# source code file
        /// example: $"new Point2D({Value.X}, {Value.Y})"
        /// </summary>
        string ToSourceCode();
    }
}