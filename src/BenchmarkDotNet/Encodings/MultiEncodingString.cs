using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BenchmarkDotNet.Encodings
{
    /// <summary> MultiEncoding String. </summary>
    /// <remarks> Contains different variants of string for different encoding </remarks>
    public class MultiEncodingString
    {
        private readonly Dictionary<string, string> encodedStrings;

        
        public MultiEncodingString(string unicodePresentation, string asciiPresentation)
        {
            var pairs = new[]
            {
                new KeyValuePair<Encoding, string>(Encoding.Unicode, unicodePresentation),
                new KeyValuePair<Encoding, string>(Encoding.ASCII, asciiPresentation)
            };
            
            encodedStrings = pairs.ToDictionary(_ => _.Key.EncodingName,
                                                _ => _.Value);
        }

        public MultiEncodingString(IEnumerable<KeyValuePair<Encoding, string>> encodedStrings)
        {
            var sourceStrings = encodedStrings ?? new KeyValuePair<Encoding, string>[] { };
            
            this.encodedStrings = sourceStrings.Where(kvp => kvp.Value != null)
                                           .ToDictionary(_ => _.Key.EncodingName, _ => _.Value);
        }

        
        public override string ToString() => GetString();

        public string ToString(Encoding encoding) => GetStringByEncoding(encoding);

        public string GetString() => GetStringByEncoding(EncodingInfo.CurrentEncoding);

        private string GetStringByEncoding(Encoding encoding)
        {
            if (encodedStrings.TryGetValue(encoding.EncodingName, out string encodedString))
                return encodedString;
            
            return encodedStrings.TryGetValue(GetFallback().EncodingName, out encodedString)
                ? encodedString
                : null;
        }

        private Encoding GetFallback() => Encoding.ASCII;
        
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
                .Aggregate(0, (current, encodedString) =>
                               current ^ encodedString.Key.GetHashCode() + encodedString.Value.GetHashCode());
        }
    }
}