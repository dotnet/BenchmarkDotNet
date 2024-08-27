using BenchmarkDotNet.Properties;
using Perfolizer.Phd.Dto;

namespace BenchmarkDotNet.Tests.Phd.Infra;

public static class PhdMock
{
    public static readonly PhdEngine Engine = new ()
    {
        Name = BenchmarkDotNetInfo.BenchmarkDotNetCaption,
        Version = "0.1729.0-mock"
    };
}