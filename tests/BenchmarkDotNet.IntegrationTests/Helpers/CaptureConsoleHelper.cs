using System.Text;

namespace BenchmarkDotNet.IntegrationTests;

internal class CaptureConsoleHelper : IDisposable
{
    private readonly TextWriter originalOut;
    private readonly StringWriter writer = new(new StringBuilder());

    public CaptureConsoleHelper()
    {
        originalOut = Console.Out;
        Console.SetOut(writer);
    }

    public void Dispose()
    {
        Console.SetOut(originalOut);
        writer.Dispose();
    }

    public string CapturedText
       => writer.ToString();

    public IReadOnlyList<string> CapturedLines
        => CapturedText.Split(["\r\n", "\n"], StringSplitOptions.None);
}
