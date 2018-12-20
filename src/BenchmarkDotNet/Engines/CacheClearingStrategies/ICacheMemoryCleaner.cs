namespace BenchmarkDotNet.Engines.CacheClearingStrategies
{
    internal interface ICacheMemoryCleaner
    {
        void Clean();
    }
}