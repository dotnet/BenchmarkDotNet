using System;
using System.IO;

namespace BenchmarkDotNet.Locators;

public interface ILocator
{
    LocatorType LocatorType { get; }
    FileInfo Locate(DirectoryInfo rootDirectory, Type type);
}