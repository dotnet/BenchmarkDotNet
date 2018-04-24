using System.Text;

namespace BenchmarkDotNet.Encodings
{
    public class EncodingInfo
    {
        public static Encoding CurrentEncoding { get; set; }

        static EncodingInfo() => CurrentEncoding = Encoding.ASCII;
    }
}