using Microsoft.VisualStudio.TestPlatform.CommunicationUtilities;

namespace BenchmarkDotNet.TestAdapter.Remoting
{
    /// <summary>
    /// A set of helper methods for serializing and deserializing the VSTest TestCases and TestReports.
    /// </summary>
    internal static class SerializationHelpers
    {
        // Version number of the VSTest protocol that the adapter supports. Only needs to be updated when
        // the VSTest protocol has a change and this test adapter wishes to take a dependency on it.
        private const int VSTestProtocolVersion = 7;

        public static string Serialize<T>(T data)
        {
            return JsonDataSerializer.Instance.Serialize(data, version: VSTestProtocolVersion);
        }

        public static T Deserialize<T>(string data)
        {
            return JsonDataSerializer.Instance.Deserialize<T>(data, version: VSTestProtocolVersion)!;
        }
    }
}
