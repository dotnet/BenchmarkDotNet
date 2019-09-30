using System.Collections.Generic;
using System.Linq;
using System.Text;
using JetBrains.Annotations;

namespace BenchmarkDotNet.Helpers
{
    /// <summary> MultiEncoding String. </summary>
    /// <remarks> Contains different variants of string for different encoding </remarks>
    public class MultiEncodingString
    {
        private readonly Dictionary<string, string> encodedStrings;
        
        /// <summary>Ctor for ascii-only presentation</summary>
        [PublicAPI] public MultiEncodingString(string asciiPresentation)
        {
            var pairs = new[]
            {
                new KeyValuePair<Encoding, string>(Encoding.ASCII, asciiPresentation)
            };
            
            encodedStrings = pairs.ToDictionary(_ => _.Key.EncodingName,
                _ => _.Value);
        }
        
        /// <summary>Ctor for specified unicode and ascii presentations</summary>
        [PublicAPI] public MultiEncodingString(string asciiPresentation, string unicodePresentation)
        {
            var pairs = new[]
            {
                new KeyValuePair<Encoding, string>(Encoding.Unicode, unicodePresentation),
                new KeyValuePair<Encoding, string>(Encoding.ASCII, asciiPresentation)
            };
            
            encodedStrings = pairs.ToDictionary(_ => _.Key.EncodingName,
                                                _ => _.Value);
        }

        /// <summary>Ctor for custom encoding presentations</summary>
        public MultiEncodingString(IEnumerable<KeyValuePair<Encoding, string>> encodedStrings)
        {
            var sourceStrings = encodedStrings ?? System.Array.Empty<KeyValuePair<Encoding, string>>();
            
            this.encodedStrings = sourceStrings.Where(kvp => kvp.Value != null)
                                               .ToDictionary(_ => _.Key.EncodingName,
                                                             _ => _.Value);
        }
        
        public override string ToString() => GetString(Encoding.ASCII);

        public string ToString(Encoding encoding) => GetStringByEncoding(encoding);

        [PublicAPI] public string GetString(Encoding encoding) => GetStringByEncoding(encoding);

        public static implicit operator MultiEncodingString(string s) => new MultiEncodingString(s);
        
        public static implicit operator MultiEncodingString((string unicodeString, string asciiString) tuple)
            => new MultiEncodingString(tuple.unicodeString, tuple.asciiString);
        
        public override bool Equals(object obj)
        {
            if (!(obj is MultiEncodingString otherMes))
                return false;

            return encodedStrings.Count == otherMes.encodedStrings.Count
                   && encodedStrings.All(p => otherMes.encodedStrings.ContainsKey(p.Key)
                                               && otherMes.encodedStrings[p.Key] == p.Value);
        }
        
        public override int GetHashCode()
        {
            return encodedStrings
                   .Aggregate(0, (current, encodedString)
                                 => current ^ encodedString.Key.GetHashCode() + encodedString.Value.GetHashCode());
        }
        
        private string GetStringByEncoding(Encoding encoding)
        {
            if (encoding == null)
                encoding = GetFallback();
                
            if (encodedStrings.TryGetValue(encoding.EncodingName, out string encodedString))
                return encodedString;
            
            return encodedStrings.TryGetValue(GetFallback().EncodingName, out encodedString)
                ? encodedString
                : null;
        }
        
        private static Encoding GetFallback() => Encoding.ASCII;
    }
}