using Microsoft.VisualStudio.TestPlatform.CommunicationUtilities;

namespace BenchmarkDotNet.TestAdapter.Remoting
{
    /// <summary>
    /// A set of helper methods for serializing and deserializing the VSTest TestCases and TestReports.
    /// </summary>
    internal static class SerializationHelpers
    {
        public static string Serialize<T>(T data)
        {
            return JsonDataSerializer.Instance.Serialize(data, version: 7);
        }

        public static T Deserialize<T>(string data)
        {
            return JsonDataSerializer.Instance.Deserialize<T>(data, version: 7)!;
        }
    }
}
