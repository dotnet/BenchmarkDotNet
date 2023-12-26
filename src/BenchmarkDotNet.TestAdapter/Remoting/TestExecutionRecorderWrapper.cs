using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Adapter;
using System;

namespace BenchmarkDotNet.TestAdapter.Remoting
{
    /// <summary>
    /// A wrapper around the ITestExecutionRecorder which works across AppDomain boundaries.
    /// </summary>
    internal class TestExecutionRecorderWrapper : MarshalByRefObject
    {
        private readonly ITestExecutionRecorder testExecutionRecorder;

        public TestExecutionRecorderWrapper(ITestExecutionRecorder testExecutionRecorder)
        {
            this.testExecutionRecorder = testExecutionRecorder;
        }

        public MessageLoggerWrapper GetLogger()
        {
            return new MessageLoggerWrapper(testExecutionRecorder);
        }

        internal void RecordStart(string serializedTestCase)
        {
            testExecutionRecorder.RecordStart(SerializationHelpers.Deserialize<TestCase>(serializedTestCase));
        }

        internal void RecordEnd(string serializedTestCase, TestOutcome testOutcome)
        {
            testExecutionRecorder.RecordEnd(SerializationHelpers.Deserialize<TestCase>(serializedTestCase), testOutcome);
        }

        internal void RecordResult(string serializedTestResult)
        {
            testExecutionRecorder.RecordResult(SerializationHelpers.Deserialize<TestResult>(serializedTestResult));
        }
    }
}