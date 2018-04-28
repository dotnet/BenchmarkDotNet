using System.Text;

namespace BenchmarkDotNet.Extensions
{
    public static class EncodingExtensions
    {
        public static string ToTemplateString(this Encoding encoding)
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