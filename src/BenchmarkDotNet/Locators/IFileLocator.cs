using System.IO;

namespace BenchmarkDotNet.Locators;

/// <summary>
/// Locators can be used to extend the default behavior of finding files
/// </summary>
public interface IFileLocator
{
    /// <summary>
    /// The type of locator
    /// </summary>
    FileLocatorType LocatorType { get; }

    /// <summary>
    /// Tries to locate a file
    /// </summary>
    /// <param name="locatorArgs">The arguments such as benchmark and logger</param>
    /// <param name="fileInfo">The file is provided by the implementation</param>
    /// <returns>True when a file was successfully found, False otherwise.</returns>
    bool TryLocate(LocatorArgs locatorArgs, out FileInfo fileInfo);
}