using BenchmarkDotNet.Loggers;
using System.Runtime.CompilerServices;
using TUnit.Core.Interfaces;

namespace BenchmarkDotNet.Tests;

public class OutputLogger : AccumulationLogger
{
    private readonly ITestOutput Output;
    private string currentLine = "";

    public OutputLogger(ITestOutput output)
    {
        Output = output ?? throw new ArgumentNullException(nameof(Output));
    }

    [MethodImpl(MethodImplOptions.Synchronized)]
    public override void Write(LogKind logKind, string text)
    {
        currentLine += text;
        base.Write(logKind, text);
    }

    [MethodImpl(MethodImplOptions.Synchronized)]
    public override void WriteLine()
    {
        Output.WriteLine(currentLine);
        currentLine = "";
        base.WriteLine();
    }

    [MethodImpl(MethodImplOptions.Synchronized)]
    public override void WriteLine(LogKind logKind, string text)
    {
        Output.WriteLine(currentLine + text);
        currentLine = "";
        base.WriteLine(logKind, text);
    }
}