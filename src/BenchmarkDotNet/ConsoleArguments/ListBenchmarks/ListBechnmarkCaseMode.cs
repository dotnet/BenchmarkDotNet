namespace BenchmarkDotNet.ConsoleArguments.ListBenchmarks
{
    public enum ListBenchmarkCaseMode
    {
        /// <summary>
        /// Do not print any of the available full benchmark names.
        /// </summary>
        Disable,

        /// <summary>
        /// Prints flat list of the available benchmark names.
        /// </summary>
        Flat,

        /// <summary>
        /// Prints tree of the available full benchmark names.
        /// </summary>
        Tree
    }
}