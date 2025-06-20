using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;
using System.Diagnostics;
using System.IO;

namespace BenchmarkDotNet.TestAdapter;

internal class LoggerHelper
{
    public LoggerHelper(IMessageLogger logger, Stopwatch stopwatch)
    {
        InnerLogger = logger;
        Stopwatch = stopwatch;
    }

    public IMessageLogger InnerLogger { get; private set; }

    public Stopwatch Stopwatch { get; private set; }

    public void Log(string format, params object[] args)
    {
        SendMessage(TestMessageLevel.Informational, null, string.Format(format, args));
    }

    public void LogWithSource(string source, string format, params object[] args)
    {
        SendMessage(TestMessageLevel.Informational, source, string.Format(format, args));
    }

    public void LogError(string format, params object[] args)
    {
        SendMessage(TestMessageLevel.Error, null, string.Format(format, args));
    }

    public void LogErrorWithSource(string source, string format, params object[] args)
    {
        SendMessage(TestMessageLevel.Error, source, string.Format(format, args));
    }

    public void LogWarning(string format, params object[] args)
    {
        SendMessage(TestMessageLevel.Warning, null, string.Format(format, args));
    }

    public void LogWarningWithSource(string source, string format, params object[] args)
    {
        SendMessage(TestMessageLevel.Warning, source, string.Format(format, args));
    }

    private void SendMessage(TestMessageLevel level, string? assemblyName, string message)
    {
        var assemblyText = assemblyName == null
            ? "" :
            $"{Path.GetFileNameWithoutExtension(assemblyName)}: ";

        InnerLogger.SendMessage(level, $"[BenchmarkDotNet {Stopwatch.Elapsed:hh\\:mm\\:ss\\.ff}] {assemblyText}{message}");
    }
}
