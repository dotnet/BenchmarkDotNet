using System.Text;

namespace BenchmarkDotNet.Extensions
{
    public static class EncodingExtensions
    {
        public static string ToTemplateString(this Encoding encoding)
        {
            var result = "System.Text.Encoding.";
            switch (encoding)
            {
                case ASCIIEncoding e:
                    result = result + "ASCII";
                    break;
                case UnicodeEncoding u:
                    result = result + u.EncodingName;
                    break;
            }
            return result;
        }
    }
}