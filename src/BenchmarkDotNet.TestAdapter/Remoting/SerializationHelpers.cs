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
        // A list of protocol versions and a summary of the changes that were made in them can be found here:
        //    https://github.com/microsoft/vstest/blob/main/docs/Overview.md#protocolversion-request
        private const int VsTestProtocolVersion = 7;

        public static string Serialize<T>(T data)
        {
            return JsonDataSerializer.Instance.Serialize(data, version: VsTestProtocolVersion);
        }

        public static T Deserialize<T>(string data)
        {
            return JsonDataSerializer.Instance.Deserialize<T>(data, version: VsTestProtocolVersion)!;
        }
    }
}
