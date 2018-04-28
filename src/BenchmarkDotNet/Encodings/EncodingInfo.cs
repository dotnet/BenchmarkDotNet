using System.Text;

namespace BenchmarkDotNet.Encodings
{
    public class EncodingInfo
    {
        public static Encoding CurrentEncoding { get; set; } = Encoding.ASCII;
        
        public static Encoding DefaultEncoding => Encoding.ASCII;
    }
}