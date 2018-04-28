using System.Text;

namespace BenchmarkDotNet.Encodings
{
    public class EncodingInfo
    {
        public static Encoding CurrentEncoding { get; set; }
        
        public static Encoding DefaultEncoding => Encoding.ASCII;

        static EncodingInfo() => CurrentEncoding = Encoding.ASCII;
    }
}