using System.Diagnostics;
using Xunit;

namespace BenchmarkDotNet.IntegrationTests
{
    public abstract class IntegrationTestBase
    {
        protected string GetTestOutput()
        {
            // NOTE this only work with XUnit versions 1.9 and earlier, the mechanism changed in XUnit 2.0
            // see https://xunit.github.io/docs/capturing-output.html for more information

            // See https://github.com/xunit/xunit/blob/v1/src/xunit/Sdk/Commands/TestCommands/ExceptionAndOutputCaptureCommand.cs#L34-L77
            // for how XUnit captures the output during a unit test, we need to access the Text/StringWriter it uses and get the text that was captured
            if (Trace.Listeners.Count == 2 && Trace.Listeners[1] is TextWriterTraceListener)
            {
                var xunitListener = Trace.Listeners[1] as TextWriterTraceListener;
                var testOutput = xunitListener.Writer.ToString();
                return testOutput;
            }
            else
            {
                Assert.True(false, "Unable to parse Benchmark run output, test");
                // we won't get here this is just to keep the compiler happy, "Assert.True(false...)" above will throw
                return string.Empty;
            }
        }
    }
}
