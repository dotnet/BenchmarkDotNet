using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Jobs;

namespace BenchmarkDotNet.Tests;

public class SingleJobConfig : ManualConfig
{
    public SingleJobConfig(Job job)
    {
        AddJob(job);
    }
}

public class SingleRunFastConfig : ManualConfig
{
    public SingleRunFastConfig()
    {
        AddJob(Job.Dry);
    }
}