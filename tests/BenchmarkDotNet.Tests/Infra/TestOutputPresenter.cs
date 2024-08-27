using Perfolizer.Presenting;
using Xunit.Abstractions;

namespace BenchmarkDotNet.Tests.Infra;

public class TestOutputPresenter(ITestOutputHelper output) : BufferedPresenter
{
    protected override void Flush(string text) => output.WriteLine(text.TrimEnd('\n', '\r'));
}