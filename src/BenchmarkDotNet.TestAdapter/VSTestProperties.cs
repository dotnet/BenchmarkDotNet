using Microsoft.VisualStudio.TestPlatform.ObjectModel;

namespace BenchmarkDotNet.TestAdapter
{
    /// <summary>
    /// A class that contains all the custom properties that can be set on VSTest TestCase and TestResults.
    /// Some of these properties are well known as they are also used by VSTest adapters for other test libraries.
    /// </summary>
    internal static class VsTestProperties
    {
        /// <summary>
        /// A test property used for storing the test results so that they could be accessed
        /// programmatically from a custom VSTest runner.
        /// </summary>
        internal static readonly TestProperty Measurement = TestProperty.Register(
            "BenchmarkDotNet.TestAdapter.Measurements",
            "Measurements",
            typeof(string[]),
            TestPropertyAttributes.Hidden,
            typeof(TestResult));
    }
}
