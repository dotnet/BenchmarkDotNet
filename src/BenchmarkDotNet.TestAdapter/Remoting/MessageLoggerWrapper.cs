using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;
using System;

namespace BenchmarkDotNet.TestAdapter.Remoting
{
    /// <summary>
    /// A wrapper around an IMessageLogger that works across AppDomain boundaries.
    /// </summary>
    internal class MessageLoggerWrapper : MarshalByRefObject, IMessageLogger
    {
        private readonly IMessageLogger logger;

        public MessageLoggerWrapper(IMessageLogger logger)
        {
            this.logger = logger;
        }

        public void SendMessage(TestMessageLevel testMessageLevel, string message)
        {
            logger.SendMessage(testMessageLevel, message);
        }
    }
}
