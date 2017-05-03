namespace BenchmarkDotNet.Configs
{
    public interface IConfigSource
    {
         IConfig Config { get; }
    }
}