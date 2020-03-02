using System.Text;

namespace BenchmarkDotNet.Extensions
{
    internal static class EncodingExtensions
    {
        internal static string ToTemplateString(this Encoding encoding)
        {
            const string result = "System.Text.Encoding.";
            switch (encoding)
            {
                case UnicodeEncoding u:
                    return result + u.EncodingName;
                default:
                    return result + "ASCII";
            }
        }
    }
}