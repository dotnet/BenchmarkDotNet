using System.Collections.Generic;
using System.Text;
using BenchmarkDotNet.Helpers;
using Xunit;

namespace BenchmarkDotNet.Tests
{
    public class MultiEncodingStringTests
    {
        [Fact]
        public void TestEquals()
        {
            var enc1 = Encoding.ASCII;
            var enc2 = Encoding.Unicode;
            
            Assert.Equal(new MultiEncodingString("string", "otherString"), new MultiEncodingString("string", "otherString"));
            Assert.Equal(new MultiEncodingString("string", "otherString").GetHashCode(), new MultiEncodingString("string", "otherString").GetHashCode());
            
            Assert.NotEqual(new MultiEncodingString(new[] {new KeyValuePair<Encoding, string>(enc1,"string")}),
                            new MultiEncodingString(new[] {new KeyValuePair<Encoding, string>(enc2,"string")}));
            
            Assert.NotEqual(new MultiEncodingString(new[] {new KeyValuePair<Encoding, string>(enc1,"string")}).GetHashCode(),
                            new MultiEncodingString(new[] {new KeyValuePair<Encoding, string>(enc2,"string")}).GetHashCode());
        }

        [Fact]
        public void TestToString()
        {
            var enc1 = Encoding.ASCII;
            var enc2 = Encoding.Unicode;

            var mes = new MultiEncodingString("string", "otherString");
            var otherMes = new MultiEncodingString(new[] { new KeyValuePair<Encoding, string>(enc1, "string"),
                                                           new KeyValuePair<Encoding, string>(enc2, "otherString") });
            
            Assert.Equal("string", mes.ToString(enc1));
            Assert.Equal("otherString", mes.ToString(enc2));
            
            Assert.Equal("string", otherMes.ToString(enc1));
            Assert.Equal("otherString", otherMes.ToString(enc2));
        }
    }
}