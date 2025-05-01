using BenchmarkDotNet.Properties;
using Perfolizer.Models;

namespace BenchmarkDotNet.Tests.Perfonar.Infra;

public static class PerfonarMock
{
    public static readonly EngineInfo Engine = new ()
    {
        Name = BenchmarkDotNetInfo.BenchmarkDotNetCaption,
        Version = "0.1729.0-mock"
    };
}