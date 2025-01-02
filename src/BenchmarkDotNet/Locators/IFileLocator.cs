using System.IO;

namespace BenchmarkDotNet.Locators;

public interface IFileLocator
{
    FileLocatorType LocatorType { get; }
    bool TryLocate(LocatorArgs locatorArgs, out FileInfo fileInfo);
}