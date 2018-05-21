namespace BenchmarkDotNet.Diagnosers
{
    public enum RunMode : byte
    {
        /// <summary>
        /// given diagnoser should not be executed for given benchmark
        /// </summary>
        None,
        /// <summary>
        /// needs extra run of the benchmark
        /// </summary>
        ExtraRun,
        /// <summary>
        /// no overhead, can be executed without extra run
        /// </summary>
        NoOverhead,
        /// <summary>
        /// implements some separate logic, that can be executed at any time
        /// </summary>
        SeparateLogic
    }
}