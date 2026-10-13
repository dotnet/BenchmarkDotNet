using Microsoft.Diagnostics.Runtime;
using Microsoft.Diagnostics.Runtime.Interfaces;
using System.Collections.Immutable;

namespace BenchmarkDotNet.Tests.Disassemblers.Arm64;

internal class MockDataTarget : IDataTarget
{
    public IDataReader DataReader { get; }

    public MockDataTarget(IDataReader dataReader)
    {
        DataReader = dataReader;
    }

    public void Dispose()
    {
        DataReader.FlushCachedData();
    }

    #region Methods/Properties that is not used
    public CacheOptions CacheOptions
        => throw new NotImplementedException();

    public ImmutableArray<IClrInfo> ClrVersions
        => throw new NotImplementedException();


    public IFileLocator? FileLocator
        => throw new NotImplementedException();

    public IEnumerable<ModuleInfo> EnumerateModules()
        => throw new NotImplementedException();
    #endregion
}
