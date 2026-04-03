namespace BenchmarkDotNet.Configs
{
    public enum ConfigUnionRule
    {
        Union,
        AlwaysUseLocal,
        AlwaysUseGlobal,

        /// <summary>
        /// Union global/local configs except for jobs.
        /// </summary>
        UnionAndUseLocalJob,
    }
}